using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class AnalyzerView : UserControl
    {
        private readonly AnalyzerEngine _engine = new();
        private CancellationTokenSource? _cts;
        private readonly Stack<string> _history = new();

        public AnalyzerView()
        {
            InitializeComponent();
            PathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private async void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = PathEntry.Text.Trim();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Ruta no válida o no existe.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _history.Clear();
            BackBtn.IsEnabled = false;
            BackBtn.Opacity = 0.45;
            
            await LoadPathAsync(path);
        }

        private async void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_history.Count == 0) return;
            string prev = _history.Pop();
            PathEntry.Text = prev;
            BackBtn.IsEnabled = _history.Count > 0;
            BackBtn.Opacity = _history.Count > 0 ? 1.0 : 0.45;
            await LoadPathAsync(prev);
        }

        private async void OpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string newPath)
            {
                _history.Push(PathEntry.Text);
                PathEntry.Text = newPath;
                BackBtn.IsEnabled = true;
                BackBtn.Opacity = 1.0;
                await LoadPathAsync(newPath);
            }
        }

        private async System.Threading.Tasks.Task LoadPathAsync(string path)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            AnalyzeBtn.IsEnabled = false;
            ResultList.ItemsSource = null;
            LoadingCard.Visibility = Visibility.Visible;
            LoadingTitle.Text = "Analizando espacio en disco...";
            LoadingDetail.Text = "Iniciando escaneo...";
            TotalSizeLbl.Text = "Total: Calculando...";

            var progress = new Progress<string>(msg => LoadingDetail.Text = msg);

            try
            {
                var (items, total) = await _engine.AnalyzeFolderAsync(path, _cts.Token, progress);

                // Map to UI-friendly shapes
                var uiList = new List<dynamic>();
                foreach (var i in items)
                {
                    uiList.Add(new
                    {
                        i.Name,
                        i.Path,
                        i.DisplaySize,
                        IconPath = i.IsDirectory
                            ? "pack://application:,,,/Assets/Icons/fi-rr-folder.png"
                            : "pack://application:,,,/Assets/Icons/fi-rr-document.png",
                        OpenVis = i.IsDirectory ? Visibility.Visible : Visibility.Collapsed,
                        BarWidth = i.PercentOfParent * 100, // 100 is max width in XAML
                        OriginalItem = i
                    });
                }

                ResultList.ItemsSource = uiList;
                TotalSizeLbl.Text = $"Total: {CleanerEngine.FormatSize(total)}";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analizando carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingCard.Visibility = Visibility.Collapsed;
                AnalyzeBtn.IsEnabled = true;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            
            // Due to anonymous type, we use reflection to get OriginalItem
            var ctx = btn.Tag;
            var propInfo = ctx.GetType().GetProperty("OriginalItem");
            if (propInfo == null) return;
            
            var item = propInfo.GetValue(ctx) as DiskItem;
            if (item == null) return;

            string kind = item.IsDirectory ? "carpeta" : "archivo";
            var msg = MessageBox.Show(
                $"¿Seguro que deseas eliminar permanentemente est{(item.IsDirectory ? "a" : "e")} {kind}?\n\n{item.Name}\n\nESTA ACCIÓN NO SE PUEDE DESHACER.",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (msg != MessageBoxResult.Yes) return;

            try
            {
                if (item.IsDirectory) Directory.Delete(item.Path, true);
                else File.Delete(item.Path);

                MessageBox.Show("¡Eliminado correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                // Reload current path
                _ = LoadPathAsync(PathEntry.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo eliminar:\n{ex.Message}\n\nEs posible que necesites permisos de Administrador.", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
