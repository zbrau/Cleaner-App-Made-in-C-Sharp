using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class LargeFilesView : UserControl
    {
        private readonly AnalyzerEngine _engine = new();
        private CancellationTokenSource? _cts;

        public LargeFilesView()
        {
            InitializeComponent();
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
            ResultList.ItemsSource = null;
            ProgressRow.Visibility = Visibility.Visible;
            TotalSizeLbl.Text = "Calculando...";

            var progress = new Progress<(string status, double pct)>(p =>
            {
                ScanStatusText.Text = p.status;
            });

            try
            {
                var files = await _engine.FindLargeFilesAsync(path, 100, progress, _cts.Token);
                ResultList.ItemsSource = files;
                long total = files.Sum(f => f.SizeBytes);
                TotalSizeLbl.Text = $"Total recuperable: {CleanerEngine.FormatSize(total)}";
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not DiskItem item) return;

            var msg = MessageBox.Show(
                $"¿Seguro que deseas eliminar permanentemente este archivo gigante?\n\n{item.Name}\n{item.DisplaySize}\n\nESTA ACCIÓN NO SE PUEDE DESHACER.",
                "Precaución", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (msg != MessageBoxResult.Yes) return;

            try
            {
                File.Delete(item.Path);
                MessageBox.Show("¡Archivo eliminado!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                // Optionally remove from list visually without full rescan
                if (ResultList.ItemsSource is System.Collections.IEnumerable list)
                {
                    var l = list.Cast<DiskItem>().ToList();
                    l.Remove(item);
                    ResultList.ItemsSource = null;
                    ResultList.ItemsSource = l;
                    TotalSizeLbl.Text = $"Total recuperable: {CleanerEngine.FormatSize(l.Sum(f=>f.SizeBytes))}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo eliminar:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
