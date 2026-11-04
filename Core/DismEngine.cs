using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AppCleanerPro.Core
{
    public class DismEngine
    {
        public static bool IsAdmin()
        {
            return CleanerEngine.IsAdmin();
        }

        public async Task<(bool success, string output)> CleanComponentStoreAsync(
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            if (!IsAdmin())
            {
                return (false, "Se requieren permisos de Administrador para ejecutar DISM.");
            }

            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("Iniciando limpieza del almacén de componentes de Windows (DISM)...");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/Online /Cleanup-Image /StartComponentCleanup /NoRestart",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var proc = new Process { StartInfo = psi };
                    proc.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            progress?.Report(e.Data.Trim());
                        }
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();

                    while (!proc.HasExited)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            try { proc.Kill(true); } catch { }
                            return (false, "Operación cancelada por el usuario.");
                        }
                        Thread.Sleep(500);
                    }

                    return (proc.ExitCode == 0, proc.ExitCode == 0 
                        ? "Limpieza del almacén de componentes completada exitosamente." 
                        : $"DISM finalizó con código {proc.ExitCode}.");
                }
                catch (Exception ex)
                {
                    return (false, $"Error al ejecutar DISM: {ex.Message}");
                }
            }, ct);
        }
    }
}
