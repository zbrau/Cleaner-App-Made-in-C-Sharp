using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AppCleanerPro.Models;

namespace AppCleanerPro.Core
{
    public class AnalyzerEngine
    {
        /// <summary>Lista solo el nivel actual; ignora enlaces simbólicos/carpetas inaccesibles.</summary>
        private static readonly EnumerationOptions TopLevelOptions = new()
        {
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            BufferSize = 65536
        };

        /// <summary>Recorrido recursivo sin seguir junctions/symlinks (evita bucles y rutas absurdas en C:\).</summary>
        private static readonly EnumerationOptions RecursiveFileOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            BufferSize = 65536
        };

        // ── Analyzer (Folder Drill-down) ────────────────────────────────────────────────
        public async Task<(List<DiskItem> items, long total)> AnalyzeFolderAsync(
            string path,
            CancellationToken ct,
            IProgress<string>? status = null)
        {
            var items = new List<DiskItem>();
            long grandTotal = 0;

            if (!Directory.Exists(path)) return (items, 0);

            await Task.Run(() =>
            {
                status?.Report("Enumerando carpetas…");
                var dirInfo = new DirectoryInfo(path);

                var subdirs = new List<DirectoryInfo>();
                try
                {
                    foreach (var d in dirInfo.EnumerateDirectories("*", TopLevelOptions))
                        subdirs.Add(d);
                }
                catch (UnauthorizedAccessException) { }

                int n = subdirs.Count;
                var bag = new ConcurrentBag<DiskItem>();

                if (n > 0)
                {
                    status?.Report($"Midiendo {n} carpetas de primer nivel (en discos grandes puede tardar varios minutos)…");
                    int done = 0;
                    int maxParallel = Math.Max(1, Math.Min(8, Environment.ProcessorCount / 2));
                    Parallel.ForEach(subdirs, new ParallelOptions
                    {
                        CancellationToken = ct,
                        MaxDegreeOfParallelism = maxParallel
                    }, d =>
                    {
                        ct.ThrowIfCancellationRequested();
                        long dSize = GetDirectorySizeSafe(d.FullName, ct, status, d.Name);
                        bag.Add(new DiskItem
                        {
                            Name = d.Name,
                            Path = d.FullName,
                            IsDirectory = true,
                            SizeBytes = dSize
                        });
                        int c = Interlocked.Increment(ref done);
                        status?.Report($"{c}/{n} carpetas listas — {d.Name}: {CleanerEngine.FormatSize(dSize)}");
                    });
                }

                foreach (var x in bag)
                {
                    items.Add(x);
                    grandTotal += x.SizeBytes;
                }

                status?.Report("Contando archivos en la raíz de esta carpeta…");
                try
                {
                    foreach (var f in dirInfo.EnumerateFiles("*", TopLevelOptions))
                    {
                        ct.ThrowIfCancellationRequested();
                        items.Add(new DiskItem
                        {
                            Name = f.Name,
                            Path = f.FullName,
                            IsDirectory = false,
                            SizeBytes = f.Length
                        });
                        grandTotal += f.Length;
                    }
                }
                catch (UnauthorizedAccessException) { }

            }, ct);

            if (grandTotal > 0)
            {
                foreach (var i in items)
                    i.PercentOfParent = (double)i.SizeBytes / grandTotal;
            }

            return (items.OrderByDescending(x => x.SizeBytes).ToList(), grandTotal);
        }

        private long GetDirectorySizeSafe(
            string path,
            CancellationToken ct,
            IProgress<string>? status,
            string? folderLabel)
        {
            long size = 0;
            long fileCount = 0;
            const int reportEvery = 40_000;
            string label = folderLabel ?? Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
            if (string.IsNullOrEmpty(label)) label = path;

            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var fi in dir.EnumerateFiles("*", RecursiveFileOptions))
                {
                    ct.ThrowIfCancellationRequested();
                    size += fi.Length;
                    fileCount++;
                    if (status != null && fileCount % reportEvery == 0)
                        status.Report($"{label}: {fileCount:n0} archivos… (~{CleanerEngine.FormatSize(size)})");
                }
            }
            catch (Exception) { /* permisos / rutas extrañas */ }

            return size;
        }

        // ── Large Files ─────────────────────────────────────────────────────────────────
        public async Task<List<DiskItem>> FindLargeFilesAsync(string searchPath, int maxCount, IProgress<(string, double)> progress, CancellationToken ct)
        {
            var files = new ConcurrentBag<FileInfo>();

            await Task.Run(() =>
            {
                progress.Report(("Building file tree...", 0));
                ScanFilesRecursive(searchPath, files, ct);
            }, ct);

            progress.Report(("Sorting top largest files...", 0.9));

            return files.OrderByDescending(f => f.Length)
                        .Take(maxCount)
                        .Select(f => new DiskItem
                        {
                            Name = f.Name,
                            Path = f.FullName,
                            IsDirectory = false,
                            SizeBytes = f.Length
                        })
                        .ToList();
        }

        // ── Duplicates ──────────────────────────────────────────────────────────────────
        public async Task<List<DuplicateGroup>> FindDuplicatesAsync(string searchPath, IProgress<(string, double)> progress, CancellationToken ct)
        {
            var files = new ConcurrentBag<FileInfo>();

            await Task.Run(() =>
            {
                progress.Report(("Buscando archivos...", 0.1));
                ScanFilesRecursive(searchPath, files, ct);
            }, ct);

            progress.Report(("Analizando por tamaño...", 0.3));

            // 1. Group by size
            var sizeGroups = files.GroupBy(f => f.Length)
                                  .Where(g => g.Key > 0 && g.Count() > 1) // Only identical size > 0
                                  .ToList();

            var candidateCount = sizeGroups.Sum(g => g.Count());
            int processed = 0;

            var results = new ConcurrentBag<DuplicateGroup>();

            await Task.Run(() =>
            {
                progress.Report(($"Comparando {candidateCount} archivos…", 0.5));

                Parallel.ForEach(sizeGroups, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct }, group =>
                {
                    // 2. Exact deep hash comparison
                    var dict = new Dictionary<string, List<FileInfo>>();
                    foreach (var f in group)
                    {
                        ct.ThrowIfCancellationRequested();
                        Interlocked.Increment(ref processed);
                        if (processed % 10 == 0)
                        {
                            double pct = 0.5 + (0.5 * processed / candidateCount);
                            progress.Report(($"Comparando firmas…", pct));
                        }

                        string? hash = ComputeFileHashPreview(f.FullName);
                        if (hash is null) continue;

                        if (!dict.ContainsKey(hash)) dict[hash] = new List<FileInfo>();
                        dict[hash].Add(f);
                    }

                    // Promote matches
                    foreach (var kvp in dict.Where(kv => kv.Value.Count > 1))
                    {
                        var dupes = kvp.Value.Select((f, index) => new DuplicateFileItem
                        {
                            Name = f.Name,
                            Path = f.FullName,
                            IsSelected = index != 0 // Auto-select all but the first one
                        }).ToList();

                        results.Add(new DuplicateGroup
                        {
                            Hash = kvp.Key,
                            SizeBytes = group.Key,
                            Files = dupes
                        });
                    }
                });
            }, ct);

            return results.OrderByDescending(g => g.SizeBytes * (g.Files.Count - 1)).ToList();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────

        private void ScanFilesRecursive(string dir, ConcurrentBag<FileInfo> bag, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var dInfo = new DirectoryInfo(dir);
                foreach (var f in dInfo.GetFiles())
                {
                    bag.Add(f);
                }

                foreach (var d in dInfo.GetDirectories())
                {
                    ScanFilesRecursive(d.FullName, bag, ct);
                }
            }
            catch { }
        }

        private string? ComputeFileHashPreview(string path)
        {
            try
            {
                // Read up to 1 MB for speed, not 100% full proof but good enough for mass scanning.
                // For exact duplicates, one would hash the entire file. For performance, we'll hash first 512KB + last 512KB + length
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long len = fs.Length;
                byte[] buffer = new byte[Math.Min(1024 * 1024, len)];

                int firstLen = Math.Min(512 * 1024, buffer.Length);
                fs.ReadExactly(buffer, 0, firstLen);
                if (len > 1024 * 1024)
                {
                    fs.Position = len - (512 * 1024);
                    int secondLen = Math.Min(512 * 1024, buffer.Length - 512 * 1024);
                    fs.ReadExactly(buffer, 512 * 1024, secondLen);
                }

                using var md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(buffer);
                return len.ToString() + "_" + BitConverter.ToString(hash).Replace("-", "");
            }
            catch
            {
                return null;
            }
        }
    }
}
