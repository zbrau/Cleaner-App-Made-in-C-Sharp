using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class TreemapView : UserControl
    {
        private readonly AnalyzerEngine _engine = new();
        private CancellationTokenSource? _cts;
        private readonly Stack<string> _history = new();
        
        private List<DiskItem> _currentData = new();
        private readonly List<Brush> _palette = new();

        public TreemapView()
        {
            InitializeComponent();
            PathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var hexes = new[] { "#3B82F6", "#8B5CF6", "#10B981", "#F59E0B", "#EF4444", "#EC4899", "#06B6D4", "#84CC16", "#F97316", "#6366F1" };
            foreach (var h in hexes) _palette.Add(new SolidColorBrush((Color)ColorConverter.ConvertFromString(h)));
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = PathEntry.Text.Trim();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

            _history.Clear();
            BackBtn.IsEnabled = false;
            BackBtn.Opacity = 0.45;
            await LoadTreemapAsync(path);
        }

        private async void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_history.Count == 0) return;
            string prev = _history.Pop();
            PathEntry.Text = prev;
            BackBtn.IsEnabled = _history.Count > 0;
            BackBtn.Opacity = _history.Count > 0 ? 1.0 : 0.45;
            await LoadTreemapAsync(prev);
        }

        private async System.Threading.Tasks.Task LoadTreemapAsync(string path)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            GenerateBtn.IsEnabled = false;
            StatusLbl.Text = "";
            TreemapProgressText.Text = "Escaneando archivos...";
            LoadingCard.Visibility = Visibility.Visible;
            _currentData.Clear();
            TreemapCanvas.Children.Clear();

            var progress = new Progress<string>(msg => TreemapProgressText.Text = msg);

            try
            {
                var (items, total) = await _engine.AnalyzeFolderAsync(path, _cts.Token, progress);
                _currentData = items.Where(x => x.SizeBytes > 0).ToList();
                StatusLbl.Text = $"Carpeta actual: {CleanerEngine.FormatSize(total)}";
                
                RedrawTreemap();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                StatusLbl.Text = "Error.";
                MessageBox.Show($"Error generando treemap: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingCard.Visibility = Visibility.Collapsed;
                GenerateBtn.IsEnabled = true;
            }
        }

        private void TreemapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentData.Count > 0) RedrawTreemap();
        }

        private void RedrawTreemap()
        {
            TreemapCanvas.Children.Clear();
            if (_currentData.Count == 0) return;

            double w = TreemapCanvas.ActualWidth;
            double h = TreemapCanvas.ActualHeight;
            if (w < 10 || h < 10) return;

            DrawTreemapSliceDice(_currentData, 0, 0, w, h);
        }

        private void DrawTreemapSliceDice(List<DiskItem> items, double x, double y, double w, double h)
        {
            long total = items.Sum(i => i.SizeBytes);
            if (total == 0 || w < 3 || h < 3) return;

            bool isWide = w >= h;
            double size = isWide ? w : h;
            double pos = isWide ? x : y;

            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                double frac = (double)item.SizeBytes / total;
                double dim = (idx < items.Count - 1) ? Math.Max(size * frac, 2) : (isWide ? x + w - pos : y + h - pos);

                double rx = Math.Max(0, isWide ? pos : x);
                double ry = Math.Max(0, isWide ? y : pos);
                double rw = Math.Max(0, isWide ? dim : w);
                double rh = Math.Max(0, isWide ? h : dim);

                // Create rectangle
                var border = new Border
                {
                    Background = _palette[idx % _palette.Count],
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x16, 0x27)), // #111627
                    BorderThickness = new Thickness(1),
                    Width = rw,
                    Height = rh,
                    Cursor = item.IsDirectory ? Cursors.Hand : Cursors.Arrow,
                    ToolTip = $"{item.Name}\n{item.DisplaySize}"
                };

                Canvas.SetLeft(border, rx);
                Canvas.SetTop(border, ry);

                // Add text if big enough
                if (rw > 40 && rh > 20)
                {
                    var tb = new TextBlock
                    {
                        Text = (item.Name.Length > 15) ? item.Name.Substring(0, 14) + "…" : item.Name,
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(4)
                    };

                    if (rh > 36)
                    {
                        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                        stack.Children.Add(tb);
                        stack.Children.Add(new TextBlock
                        {
                            Text = item.DisplaySize,
                            Foreground = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                            FontSize = 9,
                            HorizontalAlignment = HorizontalAlignment.Center
                        });
                        border.Child = stack;
                    }
                    else
                    {
                        border.Child = tb;
                    }
                }

                // Drill-down event
                if (item.IsDirectory)
                {
                    string targetPath = item.Path;
                    border.MouseLeftButtonDown += async (s, e) =>
                    {
                        e.Handled = true;
                        _history.Push(PathEntry.Text);
                        PathEntry.Text = targetPath;
                        BackBtn.IsEnabled = true;
                        BackBtn.Opacity = 1.0;
                        await LoadTreemapAsync(targetPath);
                    };
                }

                TreemapCanvas.Children.Add(border);
                pos += dim;
            }
        }
    }
}
