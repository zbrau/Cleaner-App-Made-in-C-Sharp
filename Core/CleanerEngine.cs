using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCleanerPro.Models;

namespace AppCleanerPro.Core
{
    public class CleanerEngine
    {
        // ── Windows API for Recycle Bin ─────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        private struct SHQUERYRBINFO
        {
            public uint cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll")]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        // ── Paths ──────────────────────────────────────────────────────────────

        private readonly string _userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private readonly string _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private readonly string _winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private readonly string _temp = Path.GetTempPath();
        private readonly string _historyFile;

        public CleanerEngine()
        {
            _historyFile = Path.Combine(AppContext.BaseDirectory, "clean_history.json");
        }

        // ── Recycle Bin ────────────────────────────────────────────────────────

        public (long size, long count) GetRecycleBinStats()
        {
            try
            {
                var info = new SHQUERYRBINFO { cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>() };
                if (SHQueryRecycleBin(null, ref info) == 0)
                    return (info.i64Size, info.i64NumItems);
            }
            catch { }
            return (0, 0);
        }

        public bool EmptyRecycleBin()
        {
            try { return SHEmptyRecycleBin(IntPtr.Zero, null, 0x0001 | 0x0002 | 0x0004) == 0; }
            catch { return false; }
        }

        // ── Directory Scanning ─────────────────────────────────────────────────

        public (long size, int count, List<string> files) ScanDirectory(string path, int minAgeDays = 0)
        {
            long total = 0; int cnt = 0;
            var files = new List<string>();
            if (!Directory.Exists(path)) return (0, 0, files);

            var cutoff = DateTime.Now.AddDays(-minAgeDays);
            try
            {
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        if (!fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            if (minAgeDays == 0 || fi.LastWriteTime < cutoff)
                            {
                                total += fi.Length;
                                cnt++;
                                files.Add(f);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return (total, cnt, files);
        }

        public CategoryResult ScanPaths(string[] paths, int minAgeDays = 0)
        {
            var result = new CategoryResult();
            foreach (var path in paths)
            {
                var (sz, cnt, files) = ScanDirectory(path, minAgeDays);
                result.Size += sz;
                result.Count += cnt;
                result.Files.AddRange(files);
            }
            return result;
        }

        // ── Full Scan ──────────────────────────────────────────────────────────

        public async Task<ScanResult> ScanAllAsync(
            IProgress<(string label, double pct)>? progress = null,
            CancellationToken ct = default)
        {
            var result = new ScanResult();

            async Task<CategoryResult> Run(string label, double pct, Func<CategoryResult> fn)
            {
                progress?.Report((label, pct));
                return await Task.Run(fn, ct);
            }

            result.Categories["system_temp"] = await Run("Archivos Temporales…", 0.10, () =>
                ScanPaths(new[]
                {
                    _temp,
                    Path.Combine(_winDir, "Temp"),
                    Path.Combine(_winDir, "Prefetch")
                }));

            result.Categories["browser_cache"] = await Run("Caché de Navegadores…", 0.25, () =>
                ScanPaths(new[]
                {
                    Path.Combine(_localAppData, @"Google\Chrome\User Data\Default\Cache"),
                    Path.Combine(_localAppData, @"Google\Chrome\User Data\Default\Code Cache"),
                    Path.Combine(_localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
                    Path.Combine(_localAppData, @"Microsoft\Edge\User Data\Default\Code Cache"),
                    Path.Combine(_appData, @"Mozilla\Firefox\Profiles")
                }));

            result.Categories["system_logs"] = await Run("Logs del Sistema…", 0.40, () =>
                ScanPaths(new[]
                {
                    Path.Combine(_winDir, "Logs"),
                    Path.Combine(_winDir, @"SoftwareDistribution\DataStore\Logs")
                }));

            result.Categories["windows_updates"] = await Run("Actualizaciones de Windows…", 0.55, () =>
                ScanPaths(new[] { Path.Combine(_winDir, @"SoftwareDistribution\Download") }));

            result.Categories["thumbnails"] = await Run("Caché de Miniaturas…", 0.65, () =>
                ScanPaths(new[] { Path.Combine(_localAppData, @"Microsoft\Windows\Explorer") }));

            result.Categories["downloads"] = await Run("Descargas Antiguas…", 0.75, () =>
                ScanPaths(new[] { Path.Combine(_userProfile, "Downloads") }, minAgeDays: 30));

            result.Categories["windows_old"] = await Run("Windows.old…", 0.82, () =>
                ScanPaths(new[] { @"C:\Windows.old" }));

            // Privacy (recent files shortcut)
            result.Categories["privacy"] = await Run("Privacidad…", 0.88, () =>
            {
                var r = ScanPaths(new[] { Path.Combine(_appData, @"Microsoft\Windows\Recent") });
                r.IsPrivacy = true;
                return r;
            });

            // Recycle Bin
            progress?.Report(("Papelera de Reciclaje…", 0.94));
            var (rbSize, rbCount) = GetRecycleBinStats();
            result.Categories["recycle_bin"] = new CategoryResult
            {
                Size = rbSize, Count = (int)rbCount, IsRecycleBin = true
            };

            progress?.Report(("¡Listo!", 1.0));
            return result;
        }

        // ── Clean ──────────────────────────────────────────────────────────────

        public (long freed, int deleted, int failed) CleanFiles(List<string> files)
        {
            long freed = 0; int del = 0, fail = 0;
            foreach (var f in files)
            {
                try
                {
                    if (File.Exists(f))
                    {
                        freed += new FileInfo(f).Length;
                        File.Delete(f);
                        del++;
                    }
                }
                catch { fail++; }
            }
            return (freed, del, fail);
        }

        public (long freed, int deleted, int failed) CleanCategory(string key, CategoryResult cat)
        {
            if (cat.IsRecycleBin)
            {
                var (before, _) = GetRecycleBinStats();
                EmptyRecycleBin();
                var (after, _) = GetRecycleBinStats();
                return (before - after, 1, 0);
            }
            if (cat.IsPrivacy)
            {
                var (freed, del, fail) = CleanFiles(cat.Files);
                // Flush DNS (safe on background thread)
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        ("ipconfig", "/flushdns") { CreateNoWindow = true })?.WaitForExit(3000);
                }
                catch { }
                // NOTE: Clipboard.Clear() must be called on UI thread by the caller
                return (freed, del, fail);
            }
            return CleanFiles(cat.Files);
        }

        // ── History ────────────────────────────────────────────────────────────

        public List<CleanHistoryEntry> GetHistory()
        {
            try
            {
                if (File.Exists(_historyFile))
                    return JsonSerializer.Deserialize<List<CleanHistoryEntry>>(File.ReadAllText(_historyFile))
                           ?? new();
            }
            catch { }
            return new();
        }

        public void SaveHistory(long bytes)
        {
            var history = GetHistory();
            var cutoff = DateTimeOffset.UtcNow.AddDays(-90).ToUnixTimeSeconds();
            history.RemoveAll(h => h.Timestamp < cutoff);
            history.Add(new CleanHistoryEntry
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Bytes = bytes
            });
            try { File.WriteAllText(_historyFile, JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        public static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 MB";
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F2} GB";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F2} MB";
            if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }

        public static bool IsAdmin()
        {
            try
            {
                using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(id);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }
}
