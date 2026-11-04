using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace AppCleanerPro.Core
{
    public class BrowserSqliteOptimizer
    {
        private static readonly string[] ChromiumDbNames =
        {
            "History",
            "Favicons",
            "Web Data",
            "Top Sites",
            "Shortcuts",
            "Network Action Predictor"
        };

        private static readonly string[] FirefoxDbPatterns =
        {
            "places.sqlite",
            "favicons.sqlite",
            "cookies.sqlite",
            "formhistory.sqlite",
            "storage.sqlite"
        };

        public List<string> FindAllDatabases()
        {
            var found = new List<string>();

            // 1. Chromium paths
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromiumRoots = new[]
            {
                Path.Combine(localAppData, "Google", "Chrome", "User Data"),
                Path.Combine(localAppData, "Microsoft", "Edge", "User Data"),
                Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "User Data")
            };

            foreach (var root in chromiumRoots)
            {
                if (!Directory.Exists(root)) continue;

                try
                {
                    // Check Default and Profile *
                    var dirs = new List<string>();
                    var defaultDir = Path.Combine(root, "Default");
                    if (Directory.Exists(defaultDir)) dirs.Add(defaultDir);

                    dirs.AddRange(Directory.GetDirectories(root, "Profile *"));

                    foreach (var dir in dirs)
                    {
                        foreach (var name in ChromiumDbNames)
                        {
                            var filePath = Path.Combine(dir, name);
                            if (File.Exists(filePath))
                                found.Add(filePath);
                        }
                    }
                }
                catch { }
            }

            // 2. Firefox paths
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var firefoxProfiles = Path.Combine(appData, "Mozilla", "Firefox", "Profiles");

            if (Directory.Exists(firefoxProfiles))
            {
                try
                {
                    foreach (var profileDir in Directory.GetDirectories(firefoxProfiles))
                    {
                        foreach (var pattern in FirefoxDbPatterns)
                        {
                            var filePath = Path.Combine(profileDir, pattern);
                            if (File.Exists(filePath))
                                found.Add(filePath);
                        }
                    }
                }
                catch { }
            }

            return found;
        }

        public (long totalBytes, int count) Scan()
        {
            long total = 0;
            var dbs = FindAllDatabases();
            foreach (var db in dbs)
            {
                try
                {
                    var fi = new FileInfo(db);
                    total += fi.Length;
                }
                catch { }
            }

            return (total, dbs.Count);
        }

        public async Task<(long freedBytes, int optimizedCount, int skippedCount)> OptimizeAsync(
            IProgress<(string message, double progress)>? progress = null,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                var dbs = FindAllDatabases();
                long totalFreed = 0;
                int optimized = 0;
                int skipped = 0;

                for (int i = 0; i < dbs.Count; i++)
                {
                    if (ct.IsCancellationRequested) break;

                    var db = dbs[i];
                    var fileName = Path.GetFileName(db);
                    progress?.Report(($"Optimizando {fileName}...", (double)i / Math.Max(1, dbs.Count)));

                    long beforeSize = 0;
                    try
                    {
                        beforeSize = new FileInfo(db).Length;
                    }
                    catch
                    {
                        skipped++;
                        continue;
                    }

                    // Test if file is locked
                    if (IsFileLocked(db))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        var connStr = new SqliteConnectionStringBuilder
                        {
                            DataSource = db,
                            Mode = SqliteOpenMode.ReadWrite
                        }.ToString();

                        using (var conn = new SqliteConnection(connStr))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "VACUUM; REINDEX;";
                                cmd.CommandTimeout = 30;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        var afterSize = new FileInfo(db).Length;
                        if (beforeSize > afterSize)
                        {
                            totalFreed += (beforeSize - afterSize);
                        }
                        optimized++;
                    }
                    catch
                    {
                        skipped++;
                    }
                }

                progress?.Report(("Bases de datos optimizadas con éxito", 1.0));
                return (totalFreed, optimized, skipped);
            }, ct);
        }

        private static bool IsFileLocked(string file)
        {
            try
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
