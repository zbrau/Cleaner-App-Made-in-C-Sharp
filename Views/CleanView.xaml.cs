using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;
using AppCleanerPro.Services;

namespace AppCleanerPro.Views
{
    public partial class CleanView : UserControl
    {
        private readonly CleanerEngine _engine = new();
        private readonly BrowserSqliteOptimizer _sqliteOptimizer = new();
        private readonly DismEngine _dismEngine = new();
        private ScanResult? _lastScan;
        private CancellationTokenSource? _cts;

        private readonly List<CleanCategory> _categories;

        public CleanView()
        {
            InitializeComponent();

            _categories = new List<CleanCategory>
            {
                new() { Key="system_temp",     Name="Temp del Sistema",  Description="Archivos temporales de Windows",   Icon="Assets/Icons/fi-rr-cache-clean.png",      AccentColor="#7B9FFF", IsSelected=true  },
                new() { Key="browser_cache",   Name="Caché Navegadores", Description="Chrome, Edge, Firefox",            Icon="Assets/Icons/fi-rr-browsers.png",         AccentColor="#A78BFA", IsSelected=true  },
                new() { Key="browser_sqlite",  Name="Bases de Datos Web", Description="Optimizar y compactar SQLite (Chrome/Edge/Brave/Firefox)", Icon="Assets/Icons/fi-rr-data-processing.png", AccentColor="#A78BFA", IsSelected=true },
                new() { Key="system_logs",     Name="Logs del Sistema",  Description="Prefetch y registros",             Icon="Assets/Icons/fi-rr-log-file.png",         AccentColor="#FB923C", IsSelected=true  },
                new() { Key="recycle_bin",     Name="Papelera",          Description="Archivos eliminados",              Icon="Assets/Icons/fi-rr-recycle-bin.png",      AccentColor="#F87171", IsSelected=true  },
                new() { Key="windows_updates", Name="Actualizaciones",   Description="Instaladores antiguos de Windows", Icon="Assets/Icons/fi-rr-auto-update.png",      AccentColor="#FBBF24", IsSelected=true  },
                new() { Key="thumbnails",      Name="Miniaturas",        Description="Caché de iconos del explorador",   Icon="Assets/Icons/fi-rr-gallery-thumbnails.png",AccentColor="#34D399", IsSelected=true  },
                new() { Key="privacy",         Name="Privacidad",        Description="Recientes, portapapeles, DNS",     Icon="Assets/Icons/fi-rr-eye-crossed.png",      AccentColor="#F472B6", IsSelected=true  },
                new() { Key="downloads",       Name="Descargas +30d",    Description="Archivos con más de 30 días",      Icon="Assets/Icons/fi-rr-download.png",         AccentColor="#60A5FA", IsSelected=false },
                new() { Key="windows_old",     Name="Windows.old",       Description="Sistema operativo anterior",       Icon="Assets/Icons/fi-rr-folder-xmark.png",     AccentColor="#94A3B8", IsSelected=false },
            };

            CategoryList.ItemsSource = _categories;
            LoadHistory();
        }

        // ── History ─────────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            var hist = _engine.GetHistory().TakeLast(5).Reverse().ToList();
            if (hist.Count == 0)
            {
                NoHistoryText.Visibility = Visibility.Visible;
                HistoryList.Visibility   = Visibility.Collapsed;
                return;
            }
            NoHistoryText.Visibility = Visibility.Collapsed;
            HistoryList.Visibility   = Visibility.Visible;
            HistoryList.ItemsSource = hist.Select(h => new
            {
                h.Date,
                SizeStr = CleanerEngine.FormatSize(h.Bytes)
            }).ToList();
        }

        // ── Scan ────────────────────────────────────────────────────────────────

        private async void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            ScanBtn.IsEnabled  = false;
            CleanBtn.IsEnabled = false;
            SmartBtn.IsEnabled = false;
            ProgressSection.Visibility = Visibility.Visible;
            CleanStatusLbl.Text = "Iniciando escaneo…";

            foreach (var c in _categories) c.SizeDisplay = "…";
            CategoryList.Items.Refresh();
            UpdateSummaryEmpty();

            var progress = new Progress<(string label, double pct)>(p =>
            {
                CleanStatusLbl.Text = p.label;
                CleanPctLbl.Text = $"{(int)(p.pct * 100)}%";
                double w = p.pct * Math.Max(ProgressSection.ActualWidth - 32, 100);
                CleanProgressBar.Width = w;
            });

            try
            {
                _lastScan = await _engine.ScanAllAsync(progress, _cts.Token);

                // Escanear bases de datos SQLite de navegadores
                var (sqliteBytes, sqliteCount) = _sqliteOptimizer.Scan();
                _lastScan.Categories["browser_sqlite"] = new CategoryResult { Size = sqliteBytes, Count = sqliteCount };

                ApplyScanResults();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al escanear:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ScanBtn.IsEnabled  = true;
                SmartBtn.IsEnabled = true;
                ProgressSection.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyScanResults()
        {
            if (_lastScan == null) return;

            foreach (var cat in _categories)
            {
                if (_lastScan.Categories.TryGetValue(cat.Key, out var res))
                {
                    cat.SizeBytes   = res.Size;
                    cat.SizeDisplay = CleanerEngine.FormatSize(res.Size);
                }
                else
                {
                    cat.SizeDisplay = "—";
                    cat.SizeBytes = 0;
                }
            }

            CategoryList.Items.Refresh();
            UpdateSummary();
            CleanBtn.IsEnabled = _lastScan.TotalSize > 0;
            CleanBtn.Opacity = _lastScan.TotalSize > 0 ? 1.0 : 0.45;
        }

        private void UpdateSummaryEmpty()
        {
            SelectedTotal.Text    = "—";
            SelectedCatCount.Text = "—";
            TotalFileCount.Text   = "—";
        }

        private void UpdateSummary()
        {
            if (_lastScan == null) return;
            var selected = _categories.Where(c => c.IsSelected).ToList();
            long total = selected.Sum(c => c.SizeBytes);
            int files = selected.Sum(c =>
                _lastScan.Categories.TryGetValue(c.Key, out var r) ? r.Count : 0);

            SelectedTotal.Text    = CleanerEngine.FormatSize(total);
            SelectedCatCount.Text = selected.Count.ToString();
            TotalFileCount.Text   = $"{files:N0}";
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is CleanCategory model)
            {
                model.IsSelected = cb.IsChecked == true;
                UpdateSummary();
            }
        }

        // ── Clean ────────────────────────────────────────────────────────────────

        private async void CleanBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScan == null) return;
            await RunClean(_categories.Where(c => c.IsSelected).ToList());
        }

        private async void CleanOne_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScan == null || sender is not Button btn) return;
            var key = btn.Tag as string;
            var cat = _categories.FirstOrDefault(c => c.Key == key);
            if (cat == null) return;
            await RunClean(new List<CleanCategory> { cat });
        }

        private async void SmartBtn_Click(object sender, RoutedEventArgs e)
        {
            ScanBtn_Click(SmartBtn, e);
            // Wait for scan to complete (max 90s)
            for (int i = 0; i < 450 && _lastScan == null; i++)
                await Task.Delay(200);
            if (_lastScan != null)
                await RunClean(_categories.Where(c => c.IsSelected).ToList());
        }

        private async Task RunClean(List<CleanCategory> targetCats)
        {
            if (_lastScan == null) return;

            long sel = targetCats.Sum(c => c.SizeBytes);
            var msg = MessageBox.Show(
                $"¿Limpiar {CleanerEngine.FormatSize(sel)} de {targetCats.Count} categoría(s)?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (msg != MessageBoxResult.Yes) return;

            ScanBtn.IsEnabled  = false;
            CleanBtn.IsEnabled = false;
            CleanBtn.Opacity   = 0.45;
            ProgressSection.Visibility = Visibility.Visible;
            CleanStatusLbl.Text = "Limpiando…";

            long totalFreed = 0;
            int step = 0;

            await Task.Run(() =>
            {
                foreach (var cat in targetCats)
                {
                    if (!_lastScan.Categories.TryGetValue(cat.Key, out var res)) continue;
                    step++;
                    double pct = (double)step / targetCats.Count;
                    Dispatcher.Invoke(() =>
                    {
                        CleanStatusLbl.Text = $"Limpiando {cat.Name}…";
                        CleanPctLbl.Text = $"{(int)(pct * 100)}%";
                        CleanProgressBar.Width = pct * Math.Max(ProgressSection.ActualWidth - 32, 100);
                    });

                    if (cat.Key == "browser_sqlite")
                    {
                        var (optFreed, optCount, _) = _sqliteOptimizer.OptimizeAsync().GetAwaiter().GetResult();
                        totalFreed += optFreed;
                        cat.SizeBytes = 0;
                        cat.SizeDisplay = optCount > 0 ? $"{optCount} optimizadas" : "0 MB";
                        continue;
                    }

                    var (freed, _, _) = _engine.CleanCategory(cat.Key, res);
                    totalFreed += freed;
                    cat.SizeBytes   = 0;
                    cat.SizeDisplay = "0 MB";
                }
            });

            _engine.SaveHistory(totalFreed);
            AudioService.PlayCleanComplete();
            CategoryList.Items.Refresh();
            UpdateSummary();
            LoadHistory();

            ProgressSection.Visibility = Visibility.Collapsed;
            ScanBtn.IsEnabled  = true;
            CleanBtn.IsEnabled = false;
            _lastScan = null;

            MessageBox.Show(
                $"¡Listo! Se liberaron {CleanerEngine.FormatSize(totalFreed)}.",
                "Limpieza completa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DismBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!DismEngine.IsAdmin())
            {
                MessageBox.Show(
                    "La limpieza profunda de DISM requiere ejecutar AppCleaner Pro como Administrador.",
                    "Permisos requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "¿Deseas iniciar la limpieza profunda del Almacén de Componentes de Windows (WinSxS / DISM)?\n\nEsta operación purga copias de seguridad de actualizaciones viejas de Windows y puede tardar varios minutos.",
                "Confirmar limpieza DISM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            DismBtn.IsEnabled = false;
            ScanBtn.IsEnabled = false;
            CleanBtn.IsEnabled = false;
            ProgressSection.Visibility = Visibility.Visible;
            CleanStatusLbl.Text = "Iniciando DISM...";
            CleanPctLbl.Text = "...";

            var progress = new Progress<string>(msg =>
            {
                CleanStatusLbl.Text = msg;
            });

            var (success, resultMsg) = await _dismEngine.CleanComponentStoreAsync(progress);

            ProgressSection.Visibility = Visibility.Collapsed;
            DismBtn.IsEnabled = true;
            ScanBtn.IsEnabled = true;

            if (success)
            {
                AudioService.PlayCleanComplete();
                NotificationService.Instance.Notify("Limpieza DISM Completada", "Se purgó el almacén de componentes de Windows.");
                MessageBox.Show(resultMsg, "DISM Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(resultMsg, "Aviso DISM", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
