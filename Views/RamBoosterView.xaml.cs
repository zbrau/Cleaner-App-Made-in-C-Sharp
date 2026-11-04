using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AppCleanerPro.Services;

namespace AppCleanerPro.Views
{
    public partial class RamBoosterView : UserControl
    {
        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        private readonly DispatcherTimer _timer = new();
        private readonly List<double> _history = new();
        private const int MaxPoints = 60;

        // WMI total RAM fallback — read once
        private double _totalGb = 0;

        public RamBoosterView()
        {
            InitializeComponent();
            ReadTotalRam();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        public void StartMonitor()
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
                RefreshStats();
            }
        }

        public void StopMonitor() => _timer.Stop();

        private void ReadTotalRam()
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                    if (ulong.TryParse(obj["TotalVisibleMemorySize"]?.ToString(), out ulong kb))
                        _totalGb = kb / 1024.0 / 1024.0;
            }
            catch { _totalGb = 8; }
        }

        private void Timer_Tick(object? sender, EventArgs e) => RefreshStats();

        private void RefreshStats()
        {
            try
            {
                ulong availKb = 0;
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                    ulong.TryParse(obj["FreePhysicalMemory"]?.ToString(), out availKb);

                double freeGb = availKb / 1024.0 / 1024.0;
                double usedGb = _totalGb - freeGb;
                double usedPct = _totalGb > 0 ? usedGb / _totalGb * 100 : 0;

                TotalRamText.Text = $"{_totalGb:0.0} GB";
                UsedRamText.Text  = $"{usedGb:0.0} GB";
                UsedPctText.Text  = $"{usedPct:0}% utilizado";
                FreeRamText.Text  = $"{freeGb:0.0} GB";
                FreePctText.Text  = $"{100 - usedPct:0}% libre";
                LivePctBadge.Text = $"{usedPct:0}%";

                // Color live badge by usage
                LivePctBadge.Foreground = usedPct > 85
                    ? new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71))
                    : usedPct > 65
                        ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24))
                        : new SolidColorBrush(Color.FromRgb(0x7B, 0x9F, 0xFF));

                // Update history
                _history.Add(usedPct);
                if (_history.Count > MaxPoints) _history.RemoveAt(0);

                DrawGraph();
            }
            catch { }
        }

        private void DrawGraph()
        {
            RamGraph.Children.Clear();

            double w = RamGraph.ActualWidth;
            double h = RamGraph.ActualHeight;
            if (w <= 0 || h <= 0) return;

            // Draw grid lines
            for (int i = 1; i <= 3; i++)
            {
                double y = h * i / 4;
                var line = new Line { X1 = 0, X2 = w, Y1 = y, Y2 = y, Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), StrokeThickness = 1 };
                RamGraph.Children.Add(line);
                var label = new TextBlock { Text = $"{100 - i * 25}%", Foreground = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), FontSize = 9, FontFamily = new FontFamily("Segoe UI") };
                Canvas.SetLeft(label, 4);
                Canvas.SetTop(label, y - 12);
                RamGraph.Children.Add(label);
            }

            if (_history.Count < 2) return;

            double step = w / (MaxPoints - 1);

            // Draw filled area under line
            var polygon = new Polygon { StrokeThickness = 0 };
            polygon.Fill = new LinearGradientBrush(
                Color.FromArgb(80, 0x7B, 0x9F, 0xFF),
                Color.FromArgb(5, 0x7B, 0x9F, 0xFF),
                90);

            int startIdx = Math.Max(0, MaxPoints - _history.Count);

            // Bottom-left start
            polygon.Points.Add(new Point((startIdx) * step, h));

            for (int i = 0; i < _history.Count; i++)
            {
                double x = (startIdx + i) * step;
                double y = h - (_history[i] / 100.0 * h);
                polygon.Points.Add(new Point(x, y));
            }

            // Bottom-right end
            polygon.Points.Add(new Point((startIdx + _history.Count - 1) * step, h));
            RamGraph.Children.Add(polygon);

            // Draw the line on top
            var polyline = new Polyline
            {
                Stroke = new LinearGradientBrush(Color.FromRgb(0x7B, 0x9F, 0xFF), Color.FromRgb(0xA7, 0x8B, 0xFA), 0),
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round
            };
            for (int i = 0; i < _history.Count; i++)
            {
                double x = (startIdx + i) * step;
                double y = h - (_history[i] / 100.0 * h);
                polyline.Points.Add(new Point(x, y));
            }
            RamGraph.Children.Add(polyline);

            // Current value dot
            if (_history.Count > 0)
            {
                double lastX = (startIdx + _history.Count - 1) * step;
                double lastY = h - (_history[^1] / 100.0 * h);
                var dot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)) };
                dot.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0xA7, 0x8B, 0xFA), BlurRadius = 8, ShadowDepth = 0 };
                Canvas.SetLeft(dot, lastX - 4);
                Canvas.SetTop(dot, lastY - 4);
                RamGraph.Children.Add(dot);
            }
        }

        private async void BoostBtn_Click(object sender, RoutedEventArgs e)
        {
            BoostBtn.IsEnabled = false;
            BoostResultText.Visibility = Visibility.Collapsed;

            double beforeGb = 0;
            if (double.TryParse(UsedRamText.Text.Replace(" GB", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                beforeGb = v;

            await System.Threading.Tasks.Task.Run(() =>
            {
                // Trim working sets of all accessible processes
                foreach (var p in Process.GetProcesses())
                {
                    try { EmptyWorkingSet(p.Handle); }
                    catch { }
                }

                // Force GC collection
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            });

            await System.Threading.Tasks.Task.Delay(1500);
            RefreshStats();

            if (double.TryParse(UsedRamText.Text.Replace(" GB", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double afterGb))
            {
                double freed = beforeGb - afterGb;
                if (freed > 0.01)
                {
                    BoostResultText.Text = $"✓ Liberados {freed:0.00} GB";
                    NotificationService.Instance.Notify("RAM Boosteada", $"Se liberaron {freed:0.00} GB de memoria RAM.");
                }
                else
                {
                    BoostResultText.Text = "✓ RAM optimizada";
                }
                BoostResultText.Visibility = Visibility.Visible;
                AppCleanerPro.Services.AudioService.PlayRamBoosted();
            }

            BoostBtn.IsEnabled = true;
        }
    }
}
