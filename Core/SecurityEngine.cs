using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AppCleanerPro.Models;

namespace AppCleanerPro.Core
{
    public class SecurityEngine
    {
        // ── 1. SECURE SHREDDER ──

        public async Task<(bool success, long bytesFreed)> ShredFileAsync(string filePath, int passes, IProgress<string>? progress = null)
        {
            if (!File.Exists(filePath)) return (false, 0);

            long fileLength = 0;
            try
            {
                var fi = new FileInfo(filePath);
                fileLength = fi.Length;

                // Remove readonly attributes
                fi.Attributes &= ~FileAttributes.ReadOnly;

                // Secure Overwrite (DoD 5220.22-M inspired)
                byte[][] patterns = new byte[][]
                {
                    new byte[] { 0x00 },
                    new byte[] { 0xFF },
                    // Pseudo-random pass omitted for speed unless 3 or 7 passes are chosen
                };

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    for (int pass = 1; pass <= passes; pass++)
                    {
                        progress?.Report($"Pase {pass}/{passes} sobre {fi.Name}...");
                        fs.Position = 0;
                        
                        // Decide pattern (0x00, 0xFF, Random)
                        int bInt = (pass == 1) ? 0x00 : (pass == 2) ? 0xFF : new Random().Next(0, 256);
                        byte b = (byte)bInt;

                        byte[] buffer = new byte[81920]; // 80KB buffer
                        Array.Fill(buffer, b);

                        long remaining = fileLength;
                        while (remaining > 0)
                        {
                            int toWrite = (int)Math.Min(buffer.Length, remaining);
                            fs.Write(buffer, 0, toWrite);
                            remaining -= toWrite;
                        }
                        fs.Flush(true);
                    }
                    
                    // Final pass: truncate length to 0 to destroy master file table records
                    fs.SetLength(0);
                    fs.Flush(true);
                }

                // Rename file randomly to destroy original name footprint
                string dir = Path.GetDirectoryName(filePath) ?? "";
                string newPath = Path.Combine(dir, Guid.NewGuid().ToString() + ".tmp");
                File.Move(filePath, newPath);
                
                // Finally delete
                File.Delete(newPath);
                progress?.Report($"[ÉXITO] Archivo destruido.");
                return (true, fileLength);
            }
            catch (Exception ex)
            {
                progress?.Report($"[ERROR] Falló destrucción: {ex.Message}");
                return (false, 0);
            }
        }

        public async Task<(int shredded, int failed, long totalBytes)> ShredFolderAsync(string folderPath, int passes, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            if (!Directory.Exists(folderPath)) return (0, 0, 0);

            int shredded = 0, failed = 0;
            long totalBytes = 0;

            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            progress?.Report($"Preparando destrucción de {files.Length} archivos...");

            foreach (var f in files)
            {
                if (ct.IsCancellationRequested) break;
                var res = await ShredFileAsync(f, passes);
                if (res.success)
                {
                    shredded++;
                    totalBytes += res.bytesFreed;
                }
                else
                {
                    failed++;
                }
            }

            try
            {
                if (!ct.IsCancellationRequested)
                {
                    Directory.Delete(folderPath, true);
                }
            }
            catch { }

            return (shredded, failed, totalBytes);
        }

        // ── 2. PRIVACY SCANNER ──

        public async Task<List<PrivacyMatch>> ScanPrivacyFilesAsync(string basePath, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            var results = new ConcurrentBag<PrivacyMatch>();
            string[] searchPatterns = { "*.txt", "*.json", "*.csv", "*.log", "*.xml" };
            
            // Spanish & English privacy keywords
            var regex = new Regex(@"password|contraseña|pwd|secret|api_key|token|auth", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            await Task.Run(() =>
            {
                var allFiles = new List<string>();
                // Gather files safely
                foreach (var pat in searchPatterns)
                {
                    try
                    {
                        var files = Directory.EnumerateFiles(basePath, pat, SearchOption.AllDirectories);
                        allFiles.AddRange(files.Take(10000)); // Cap at 10,000 files to avoid locking up
                    }
                    catch (UnauthorizedAccessException) { }
                }

                int totalCount = allFiles.Count;
                int processed = 0;

                Parallel.ForEach(allFiles, new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        var matches = regex.Matches(content);
                        if (matches.Count > 0)
                        {
                            var fi = new FileInfo(file);
                            results.Add(new PrivacyMatch
                            {
                                FilePath = file,
                                FileName = fi.Name,
                                MatchCount = matches.Count,
                                SizeBytes = fi.Length,
                                SizeDisplay = CleanerEngine.FormatSize(fi.Length)
                            });
                        }
                    }
                    catch { }
                    
                    int p = Interlocked.Increment(ref processed);
                    if (p % 100 == 0) progress?.Report((p * 100) / totalCount);
                });
            }, ct);

            return results.OrderByDescending(x => x.MatchCount).ToList();
        }

        // ── 3. PROCESS MANAGER ──

        public List<ProcessItem> GetActiveProcesses()
        {
            var result = new List<ProcessItem>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.Id == 0 || p.Id == 4) continue; // Skip Idle and System
                    
                    result.Add(new ProcessItem
                    {
                        ProcessId = p.Id,
                        Name = p.ProcessName,
                        MainWindowTitle = p.MainWindowTitle,
                        MemoryBytes = p.WorkingSet64,
                        MemoryDisplay = CleanerEngine.FormatSize(p.WorkingSet64)
                    });
                }
                catch { } // Excepciones por acceso denegado a procesos críticos
            }
            return result.OrderByDescending(x => x.MemoryBytes).ToList();
        }

        public bool KillProcess(int processId)
        {
            try
            {
                var p = Process.GetProcessById(processId);
                p.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── 4. NETWORK MONITOR ──

        public List<NetworkConnectionItem> GetNetworkConnections()
        {
            var results = new List<NetworkConnectionItem>();
            try
            {
                var props = IPGlobalProperties.GetIPGlobalProperties();
                
                // TCP Connections
                foreach (var tcp in props.GetActiveTcpConnections())
                {
                    results.Add(new NetworkConnectionItem
                    {
                        Protocol = "TCP",
                        LocalAddress = $"{tcp.LocalEndPoint.Address}:{tcp.LocalEndPoint.Port}",
                        RemoteAddress = $"{tcp.RemoteEndPoint.Address}:{tcp.RemoteEndPoint.Port}",
                        State = tcp.State.ToString()
                    });
                }

                // UDP Endpoints
                foreach (var udp in props.GetActiveUdpListeners())
                {
                    results.Add(new NetworkConnectionItem
                    {
                        Protocol = "UDP",
                        LocalAddress = $"{udp.Address}:{udp.Port}",
                        RemoteAddress = "*:*",
                        State = "Listening"
                    });
                }
            }
            catch { }
            return results;
        }
    }
}
