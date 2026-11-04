using System;
using System.IO;
using System.Management;
using System.Text.Json;
using System.Threading.Tasks;
using AppCleanerPro.Models;

namespace AppCleanerPro.Core
{
    public class SystemEngine
    {
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        // ── Hardware Info ────────────────────────────────────────────────────────
        public async Task<SystemHardwareInfo> GetSystemInfoAsync()
        {
            var info = new SystemHardwareInfo();

            await Task.Run(() =>
            {
                // OS
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Caption, OSArchitecture FROM Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        info.OsName = obj["Caption"]?.ToString() ?? "Windows";
                        info.OsArchitecture = obj["OSArchitecture"]?.ToString() ?? "x64";
                    }
                }
                catch { }

                // CPU
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor");
                    foreach (var obj in searcher.Get())
                    {
                        info.CpuName = obj["Name"]?.ToString() ?? "CPU Desconocida";
                        info.CpuCores = Convert.ToInt32(obj["NumberOfCores"]);
                    }
                }
                catch { }

                // GPU
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        info.GpuName = obj["Name"]?.ToString() ?? "GPU Desconocida";
                        break; // Get primay
                    }
                }
                catch { }

                // RAM
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        if (ulong.TryParse(obj["TotalVisibleMemorySize"]?.ToString(), out ulong kb))
                        {
                            double gb = kb / 1024.0 / 1024.0;
                            info.RamTotal = $"{Math.Round(gb, 1)} GB";
                        }
                    }
                }
                catch { }

                // Motherboard
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard");
                    foreach (var obj in searcher.Get())
                    {
                        info.MoboModel = obj["Product"]?.ToString() ?? "Motherboard Desconocida";
                    }
                }
                catch { }
            });

            return info;
        }

        // ── Settings ─────────────────────────────────────────────────────────────
        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    var sets = JsonSerializer.Deserialize<AppSettings>(json);
                    if (sets != null) return sets;
                }
            }
            catch { }
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }
    }
}
