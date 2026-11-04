using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AppCleanerPro.Core;
using AppCleanerPro.Models;
using AppCleanerPro.Services;

namespace AppCleanerPro.Views
{
    // Converter needed inside XAML (BoolToOpacity)
    public class BoolToOpacityConverter : System.Windows.Data.IValueConverter
    {
        public static readonly BoolToOpacityConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? 1.0 : 0.45;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public partial class DashboardView : UserControl
    {
        [System.Runtime.InteropServices.DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        private readonly CleanerEngine _engine = new();
        private ScanResult? _lastScan;
        private CancellationTokenSource? _cts;

        // Category metadata
        private readonly List<(string key, string name, string desc, string icon, string color, bool selectedByDefault)> _cats = new()
        {
            ("system_temp",      "Temp del Sistema",   "Archivos temporales de Windows",   "Assets/Icons/fi-rr-cache-clean.png",       "#7B9FFF", true),
            ("browser_cache",    "Caché Navegadores",  "Chrome, Edge, Firefox",             "Assets/Icons/fi-rr-browsers.png",          "#A78BFA", true),
            ("system_logs",      "Logs del Sistema",   "Prefetch y registros",              "Assets/Icons/fi-rr-log-file.png",          "#FB923C", true),
            ("recycle_bin",      "Papelera",           "Archivos eliminados",               "Assets/Icons/fi-rr-recycle-bin.png",       "#F87171", true),
            ("windows_updates",  "Actualizaciones",    "Instaladores antiguos",             "Assets/Icons/fi-rr-auto-update.png",       "#FBBF24", true),
            ("thumbnails",       "Miniaturas",         "Caché de iconos",                   "Assets/Icons/fi-rr-gallery-thumbnails.png","#34D399", true),
            ("privacy",          "Privacidad",         "Recientes, Portapapeles, DNS",      "Assets/Icons/fi-rr-eye-crossed.png",       "#F472B6", true),
            ("downloads",        "Descargas +30d",     "Descargas con más de 30 días",      "Assets/Icons/fi-rr-download.png",          "#60A5FA", false),
            ("windows_old",      "Windows.old",        "SO anterior",                       "Assets/Icons/fi-rr-folder-xmark.png",      "#94A3B8", false),
        };

        // Per-category checkbox state  
        private readonly Dictionary<string, bool> _catSelected = new();

        public DashboardView()
        {
            InitializeComponent();
            foreach (var (key, _, _, _, _, sel) in _cats)
                _catSelected[key] = sel;

            BuildCategoryCards();
            LoadHealthAndHistory();
        }

        // ── Build category cards ─────────────────────────────────────────────────

        private Dictionary<string, TextBlock> _catSizeLabels = new();
        private Dictionary<string, CheckBox> _catCheckboxes = new();

        private void BuildCategoryCards()
        {
            CategoryGrid.Children.Clear();
            _catSizeLabels.Clear();
            _catCheckboxes.Clear();

            foreach (var (key, name, desc, icon, color, sel) in _cats)
            {
                // Outer border (glass card)
                var card = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(16, 14, 16, 14),
                };
                card.SetResourceReference(Border.BorderBrushProperty, "BorderCard");
                card.Background = new SolidColorBrush(Color.FromRgb(0x11, 0x16, 0x27));

                var inner = new StackPanel();

                // Header row: icon + name + checkbox
                var hdr = new Grid();
                hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var iconImg = new Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri($"pack://application:,,,/{icon}", UriKind.Absolute)),
                    Width = 18, Height = 18,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(iconImg, System.Windows.Media.BitmapScalingMode.HighQuality);
                Grid.SetColumn(iconImg, 0);

                var nameLbl = new TextBlock
                {
                    Text = name, FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xCF, 0xEE)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(nameLbl, 1);

                var cb = new CheckBox
                {
                    IsChecked = sel,
                    VerticalAlignment = VerticalAlignment.Center
                };
                cb.Checked   += (_, _) => { _catSelected[key] = true; UpdateSelectedTotal(); };
                cb.Unchecked += (_, _) => { _catSelected[key] = false; UpdateSelectedTotal(); };
                _catCheckboxes[key] = cb;
                Grid.SetColumn(cb, 2);

                hdr.Children.Add(iconImg);
                hdr.Children.Add(nameLbl);
                hdr.Children.Add(cb);

                // Description
                var descLbl = new TextBlock
                {
                    Text = desc, FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x3A, 0x40, 0x60)),
                };

                // Size label
                var sizeLbl = new TextBlock
                {
                    Text = "—",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 20, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                _catSizeLabels[key] = sizeLbl;

                // Progress bar
                var barBg = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x0F, 0x1A)),
                    CornerRadius = new CornerRadius(3),
                    Height = 4
                };
                var barFill = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0
                };
                // Store barFill ref with key
                barBg.Tag = barFill;
                var grid = new Grid();
                grid.Children.Add(barBg);
                grid.Children.Add(barFill);

                // Track bar fill
                _barFills[key] = barFill;

                inner.Children.Add(hdr);
                inner.Children.Add(descLbl);
                inner.Children.Add(sizeLbl);
                inner.Children.Add(grid);

                card.Child = inner;
                CategoryGrid.Children.Add(card);
            }
        }

        private readonly Dictionary<string, Border> _barFills = new();

        // ── Health & History (on load) ───────────────────────────────────────────

        private void LoadHealthAndHistory()
        {
            // History
            var hist = _engine.GetHistory();
            if (hist.Count > 0)
            {
                var last = hist[^1];
                LastCleanSize.Text = CleanerEngine.FormatSize(last.Bytes);
                LastCleanDate.Text = last.Date;
            }

            // Enhanced health score with disk + RAM
            Task.Run(() =>
            {
                int score = 100;
                string label = "Excelente";

                try
                {
                    // Disk check
                    var di = new System.IO.DriveInfo("C");
                    double diskPct = (double)(di.TotalSize - di.AvailableFreeSpace) / di.TotalSize * 100;
                    if (diskPct > 90) score -= 35;
                    else if (diskPct > 75) score -= 18;
                    else if (diskPct > 60) score -= 8;

                    // RAM check via WMI
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        if (ulong.TryParse(obj["FreePhysicalMemory"]?.ToString(), out ulong freeKb) &&
                            ulong.TryParse(obj["TotalVisibleMemorySize"]?.ToString(), out ulong totalKb))
                        {
                            double ramUsedPct = (1.0 - (double)freeKb / totalKb) * 100;
                            if (ramUsedPct > 90) score -= 25;
                            else if (ramUsedPct > 75) score -= 12;
                            else if (ramUsedPct > 60) score -= 5;
                        }
                    }

                    score = Math.Max(0, Math.Min(100, score));
                    label = score >= 85 ? "Excelente" : score >= 65 ? "Bueno" : score >= 40 ? "Precaución" : "Crítico";
                }
                catch { }

                Dispatcher.Invoke(() =>
                {
                    HealthScore.Text = score.ToString();
                    HealthLabel.Text = label;
                    HealthScore.Foreground = score >= 80
                        ? new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99))
                        : score >= 60
                            ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24))
                            : new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));

                    // Fire notification if score is critical
                    if (score < 40)
                        NotificationService.Instance.Warn("⚠️ Sistema en estado crítico", $"Score de salud: {score}/100. Considera limpiar y optimizar tu PC.");
                });
            });
        }

        // ── Scan ────────────────────────────────────────────────────────────────

        private async void DashScanBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            DashScanBtn.IsEnabled = false;
            DashCleanBtn.IsEnabled = false;
            ScanProgressRow.Visibility = Visibility.Visible;
            JunkTotal.Text = "—";
            JunkLabel.Text = "Escaneando…";

            // Reset category labels
            foreach (var k in _catSizeLabels.Keys)
            {
                _catSizeLabels[k].Text = "…";
                _barFills[k].Width = 0;
            }

            var progress = new Progress<(string label, double pct)>(p =>
            {
                ScanStatusText.Text = p.label;
                ScanPctText.Text = $"{(int)(p.pct * 100)}%";
                // Animate bar width (bind to ActualWidth of parent later; clamping to >= 0 to avoid crash)
                ScanProgressFill.Width = p.pct * Math.Max(ScanProgressRow.ActualWidth - 44, 0);
            });

            try
            {
                _lastScan = await _engine.ScanAllAsync(progress, _cts.Token);
                DisplayScanResults();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al escanear:\n\n{ex.Message}",
                    "Error de escaneo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DashScanBtn.IsEnabled = true;
                ScanProgressRow.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayScanResults()
        {
            if (_lastScan == null) return;

            long total = 0;
            foreach (var (key, _, _, _, _, _) in _cats)
            {
                if (!_lastScan.Categories.TryGetValue(key, out var cat)) continue;
                var sz = cat.Size;
                total += sz;

                if (_catSizeLabels.TryGetValue(key, out var lbl))
                    lbl.Text = CleanerEngine.FormatSize(sz);

                // Animate bar proportional (rough visual width)
                if (_barFills.TryGetValue(key, out var fill))
                {
                    double maxW = 200;
                    double pct = total > 0 ? Math.Min((double)sz / total, 1.0) : 0;
                    fill.Width = pct * maxW;
                }
            }

            JunkTotal.Text  = CleanerEngine.FormatSize(_lastScan.TotalSize);
            JunkLabel.Text  = $"En {_lastScan.Categories.Count} categorías";
            DashCleanBtn.IsEnabled = _lastScan.TotalSize > 0;
        }

        private void UpdateSelectedTotal()
        {
            if (_lastScan == null) return;
            long sel = 0;
            foreach (var (key, _, _, _, _, _) in _cats)
                if (_catSelected.TryGetValue(key, out var isOn) && isOn &&
                    _lastScan.Categories.TryGetValue(key, out var cat))
                    sel += cat.Size;
            JunkTotal.Text = CleanerEngine.FormatSize(sel);
        }

        // ── Clean ────────────────────────────────────────────────────────────────

        private async void DashCleanBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScan == null) return;

            var result = MessageBox.Show(
                $"¿Limpiar {CleanerEngine.FormatSize(_lastScan.TotalSize)} de archivos seleccionados?",
                "Confirmar limpieza", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            DashScanBtn.IsEnabled = false;
            DashCleanBtn.IsEnabled = false;
            ScanProgressRow.Visibility = Visibility.Visible;
            ScanStatusText.Text = "Limpiando…";

            long totalFreed = 0;

            await Task.Run(() =>
            {
                foreach (var (key, _, _, _, _, _) in _cats)
                {
                    if (!_catSelected.TryGetValue(key, out var sel) || !sel) continue;
                    if (!_lastScan.Categories.TryGetValue(key, out var cat)) continue;
                    var (freed, _, _) = _engine.CleanCategory(key, cat);
                    totalFreed += freed;

                    Dispatcher.Invoke(() =>
                    {
                        if (_catSizeLabels.TryGetValue(key, out var lbl)) lbl.Text = "0 MB";
                        if (_barFills.TryGetValue(key, out var fill)) fill.Width = 0;
                    });
                }
            });

            _engine.SaveHistory(totalFreed);

            JunkTotal.Text  = CleanerEngine.FormatSize(totalFreed);
            JunkLabel.Text  = "¡Limpieza completa!";
            LastCleanSize.Text = CleanerEngine.FormatSize(totalFreed);
            LastCleanDate.Text = DateTime.Now.ToString("yyyy-MM-dd");

            ScanProgressRow.Visibility = Visibility.Collapsed;
            DashScanBtn.IsEnabled = true;
            _lastScan = null;

            MessageBox.Show(
                $"Se liberaron {CleanerEngine.FormatSize(totalFreed)} de espacio.",
                "Limpieza completada", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private async void TurboBtn_Click(object sender, RoutedEventArgs e)
        {
            TurboBtn.IsEnabled = false;

            var msg = MessageBox.Show(
                "Modo Turbo cerrará procesos no esenciales, liberará RAM y limpiará archivos temporales.\n\n¿Continuar?",
                "Activar Modo Turbo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (msg != MessageBoxResult.Yes)
            {
                TurboBtn.IsEnabled = true;
                return;
            }

            ScanProgressRow.Visibility = Visibility.Visible;
            ScanStatusText.Text = "Modo Turbo activado...";

            await Task.Run(() =>
            {
                // 1. Kill non-essential background processes
                var killTargets = new[] { "OneDrive", "Spotify", "Discord", "Teams", "slack", "Skype", "SearchApp", "YourPhone" };
                foreach (var target in killTargets)
                    foreach (var p in Process.GetProcessesByName(target))
                    {
                        try { p.Kill(); } catch { }
                    }

                // 2. Free RAM working sets
                foreach (var p in Process.GetProcesses())
                {
                    try { EmptyWorkingSet(p.Handle); } catch { }
                }

                // 3. GC
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();

                // 4. Delete temp files
                try
                {
                    var tmp = System.IO.Path.GetTempPath();
                    foreach (var f in System.IO.Directory.GetFiles(tmp))
                        try { System.IO.File.Delete(f); } catch { }
                }
                catch { }

                Dispatcher.Invoke(() =>
                {
                    ScanStatusText.Text = "Modo Turbo completado";
                    ScanProgressFill.Width = ScanProgressRow.ActualWidth - 44;
                });
            });

            await Task.Delay(1200);
            ScanProgressRow.Visibility = Visibility.Collapsed;

            NotificationService.Instance.Notify("Modo Turbo Activado",
                "Tu PC ha sido optimizada. RAM liberada y procesos cerrados.");

            MessageBox.Show("Modo Turbo completado.\n\nSe cerraron procesos innecesarios y se liberó memoria RAM.",
                "Turbo Mode", MessageBoxButton.OK, MessageBoxImage.Information);

            TurboBtn.IsEnabled = true;
            LoadHealthAndHistory(); // Refresh health score
        }
    }
}
