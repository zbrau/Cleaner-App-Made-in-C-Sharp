using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using AppCleanerPro.Models;

namespace AppCleanerPro.Core
{
    public class ToolsEngine
    {
        // ── 1. STARTUP MANAGER ──

        public List<StartupItem> GetStartupItems()
        {
            var results = new List<StartupItem>();
            string[] keys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
            };

            foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (var keyPath in keys)
                {
                    try
                    {
                        using var key = hive.OpenSubKey(keyPath);
                        if (key == null) continue;

                        foreach (var name in key.GetValueNames())
                        {
                            var val = key.GetValue(name)?.ToString() ?? "";
                            results.Add(new StartupItem
                            {
                                Name = name,
                                Command = val,
                                RegistryPath = (hive == Registry.CurrentUser ? "HKCU\\" : "HKLM\\") + keyPath,
                                Location = hive == Registry.CurrentUser ? "Usuario Actual" : "Todos los Usuarios"
                            });
                        }
                    }
                    catch { }
                }
            }
            return results;
        }

        public bool DeleteStartupItem(StartupItem item)
        {
            try
            {
                var hive = item.RegistryPath.StartsWith("HKCU") ? Registry.CurrentUser : Registry.LocalMachine;
                var subKeyPath = item.RegistryPath.Substring(5);
                using var key = hive.OpenSubKey(subKeyPath, true);
                if (key != null)
                {
                    key.DeleteValue(item.Name, false);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // ── 2. UNINSTALLER & LEFTOVERS ──

        public List<AppItem> GetInstalledApps()
        {
            var results = new List<AppItem>();
            string[] keys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (var keyPath in keys)
                {
                    try
                    {
                        using var key = hive.OpenSubKey(keyPath);
                        if (key == null) continue;

                        foreach (var subName in key.GetSubKeyNames())
                        {
                            using var subKey = key.OpenSubKey(subName);
                            if (subKey == null) continue;

                            var displayName = subKey.GetValue("DisplayName") as string;
                            var uninstallStr = subKey.GetValue("UninstallString") as string ?? subKey.GetValue("QuietUninstallString") as string;
                            var publisher = subKey.GetValue("Publisher") as string;

                            if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(uninstallStr))
                            {
                                results.Add(new AppItem
                                {
                                    Name = displayName,
                                    Publisher = publisher ?? "Desconocido",
                                    UninstallString = uninstallStr,
                                    RegistryKeyPath = (hive == Registry.CurrentUser ? "HKCU\\" : "HKLM\\") + keyPath + "\\" + subName
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            return results.GroupBy(x => x.Name).Select(g => g.First()).OrderBy(x => x.Name).ToList();
        }

        public List<string> FindLeftoverFolders(string appName, string publisher)
        {
            var leftovers = new List<string>();
            var targets = new List<string>();

            // Extraer palabras clave (nombre principal de la app)
            string keyword = appName.Split(' ')[0];
            string pubKeyword = !string.IsNullOrEmpty(publisher) && publisher != "Desconocido" ? publisher.Split(' ')[0] : keyword;

            var baseDirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            };

            foreach (var b in baseDirs)
            {
                if (string.IsNullOrEmpty(b) || !Directory.Exists(b)) continue;
                try
                {
                    var subdirs = Directory.GetDirectories(b);
                    foreach (var sub in subdirs)
                    {
                        var folderName = Path.GetFileName(sub);
                        if (folderName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || folderName.Contains(pubKeyword, StringComparison.OrdinalIgnoreCase))
                        {
                            leftovers.Add(sub);
                        }
                    }
                }
                catch { }
            }
            return leftovers;
        }

        public void RunUninstaller(string uninstallString)
        {
            try
            {
                // Limpiar argumentos
                string exe = uninstallString;
                string args = "";
                
                if (exe.StartsWith("\"")) {
                    int endQuote = exe.IndexOf("\"", 1);
                    if (endQuote > 0) {
                        args = exe.Substring(endQuote + 1).Trim();
                        exe = exe.Substring(1, endQuote - 1);
                    }
                } else if (exe.Contains(" ")) {
                    int space = exe.IndexOf(" ");
                    args = exe.Substring(space + 1).Trim();
                    exe = exe.Substring(0, space);
                }

                if (exe.ToLower().Contains("msiexec.exe") && !args.ToLower().Contains("/x")) {
                    args = "/x " + args;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch { }
        }

        // ── 3. REGISTRY CLEANER (SAFE) ──

        public List<RegistryIssue> ScanRegistry()
        {
            var issues = new List<RegistryIssue>();
            
            // Only scan Safe spots: HKCR pass
            try
            {
                using var extKey = Registry.ClassesRoot;
                foreach (var sub in extKey.GetSubKeyNames().Take(1000)) // limit para rapidez
                {
                    try
                    {
                        if (sub.StartsWith("."))
                        {
                            using var k = extKey.OpenSubKey(sub);
                            var val = k?.GetValue("") as string;
                            if (val != null && val.EndsWith("_auto_file"))
                            {
                                // Extensiones huérfanas sin tipo
                                issues.Add(new RegistryIssue { Type = "Extensión Vacía", RootKey = "HKCR", SubKey = sub, Name = sub, Detail = "Asociación huérfana de tipo de archivo" });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return issues;
        }

        public bool FixRegistryIssues(List<RegistryIssue> issues)
        {
            int solved = 0;
            foreach (var issue in issues)
            {
                try
                {
                    if (issue.RootKey == "HKCR")
                    {
                        Registry.ClassesRoot.DeleteSubKeyTree(issue.SubKey, false);
                        solved++;
                    }
                }
                catch { }
            }
            return solved == issues.Count;
        }

        // ── 4. FIREWALL ──

        public List<FirewallRuleItem> GetFirewallRules()
        {
            var rules = new List<FirewallRuleItem>();
            try
            {
                Type? type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (type is null) return rules;

                object? fw = Activator.CreateInstance(type);
                if (fw is null) return rules;
                dynamic fwPolicy2 = fw;
                foreach (dynamic rule in fwPolicy2.Rules)
                {
                    rules.Add(new FirewallRuleItem
                    {
                        Name = rule.Name ?? "Desconocido",
                        Action = rule.Action == 1 ? "Allow" : "Block",
                        Direction = rule.Direction == 1 ? "Inbound" : "Outbound",
                        Protocol = rule.Protocol == 6 ? "TCP" : (rule.Protocol == 17 ? "UDP" : rule.Protocol.ToString()),
                        Profile = rule.Profiles.ToString()
                    });
                }
            }
            catch { }
            return rules;
        }
    }
}
