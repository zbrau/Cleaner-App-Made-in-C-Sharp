using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class DuplicatesView : UserControl
    {
        private readonly AnalyzerEngine _engine = new();
        private CancellationTokenSource? _cts;
        private List<DuplicateGroup> _lastResults = new();

        public DuplicatesView()
        {
            InitializeComponent();
            PathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private async void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = PathEntry.Text.Trim();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Ruta no válida o no existe.","Error",MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            ScanBtn.IsEnabled = false;
            CleanBtn.IsEnabled = false;
            CleanBtn.Opacity = 0.45;
            ResultList.ItemsSource = null;
            ProgressRow.Visibility = Visibility.Visible;
            TotalSizeLbl.Text = "Calculando...";

            var progress = new Progress<(string status, double pct)>(p =>
            {
                ScanStatusText.Text = p.status;
            });

            try
            {
                _lastResults = await _engine.FindDuplicatesAsync(path, progress, _cts.Token);
                ResultList.ItemsSource = _lastResults;
                
                long totalRecoverable = _lastResults.Sum(g => g.SizeBytes * (g.Files.Count - 1));
                TotalSizeLbl.Text = $"Total recuperable: {CleanerEngine.FormatSize(totalRecoverable)}";
                
                if (_lastResults.Count > 0)
                {
                    CleanBtn.IsEnabled = true;
                    CleanBtn.Opacity = 1.0;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Error escaneando: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressRow.Visibility = Visibility.Collapsed;
                ScanBtn.IsEnabled = true;
            }
        }

        private void CleanBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastResults.Count == 0) return;

            // Gather all marked files
            var toDelete = _lastResults.SelectMany(g => g.Files).Where(f => f.IsSelected).ToList();
            if (toDelete.Count == 0) return;

            var msg = MessageBox.Show(
                $"¿Seguro que deseas eliminar {toDelete.Count} archivos duplicados?\n\nESTA ACCIÓN NO SE PUEDE DESHACER.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (msg != MessageBoxResult.Yes) return;

            int deleted = 0;
            long freed = 0;

            foreach (var f in toDelete)
            {
                try
                {
                    if (File.Exists(f.Path))
                    {
                        long size = new FileInfo(f.Path).Length;
                        File.Delete(f.Path);
                        deleted++;
                        freed += size;
                    }
                }
                catch { }
            }

            MessageBox.Show($"Se eliminaron {deleted} archivos.\nEspacio liberado: {CleanerEngine.FormatSize(freed)}", "Limpieza Completada", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Clear results to force rescan
            _lastResults.Clear();
            ResultList.ItemsSource = null;
            CleanBtn.IsEnabled = false;
            CleanBtn.Opacity = 0.45;
            TotalSizeLbl.Text = "Total recuperable: 0 MB";
        }
    }
}
