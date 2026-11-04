using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AppCleanerPro.Core;

namespace AppCleanerPro.Views
{
    public partial class ShredderView : UserControl
    {
        private readonly SecurityEngine _engine = new();
        private readonly ObservableCollection<string> _logs = new();
        private CancellationTokenSource? _cts;

        public ShredderView()
        {
            InitializeComponent();
            LogList.ItemsSource = _logs;
        }

        private void BrowseFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = "Eliminación Segura - Seleccionar Archivo" };
            if (dlg.ShowDialog() == true)
            {
                PathEntry.Text = dlg.FileName;
            }
        }

        private void BrowseFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Eliminación Segura - Seleccionar Carpeta" };
            if (dlg.ShowDialog() == true)
            {
                PathEntry.Text = dlg.FolderName;
            }
        }

        private async void ShredFileBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = PathEntry.Text.Trim();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show("Seleccione un archivo válido.", "Shredder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int passes = PassesCombo.SelectedIndex == 0 ? 1 : PassesCombo.SelectedIndex == 1 ? 3 : 7;
            
            if (MessageBox.Show($"¿Destruir permanentemente el archivo con {passes} pases?\n\n{path}\n\nESTO NO SE PUEDE DESHACER.", 
                "Confirmación Peligrosa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            _logs.Clear();
            ResultLbl.Text = "Destruyendo...";
            ResultLbl.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
            ShredProgressBar.Visibility = Visibility.Visible;
            PathEntry.IsEnabled = false;

            var progress = new Progress<string>(msg => _logs.Add(msg));
            var res = await Task.Run(() => _engine.ShredFileAsync(path, passes, progress));

            ShredProgressBar.Visibility = Visibility.Collapsed;
            if (res.success)
            {
                ResultLbl.Text = $"Destruido con éxito: {CleanerEngine.FormatSize(res.bytesFreed)}";
                PathEntry.Text = "";
            }
            else
            {
                ResultLbl.Text = "Falla o Acceso Denegado";
                ResultLbl.Foreground = System.Windows.Media.Brushes.Red;
            }
            PathEntry.IsEnabled = true;
        }

        private async void ShredFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = PathEntry.Text.Trim();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Seleccione una carpeta válida.", "Shredder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int passes = PassesCombo.SelectedIndex == 0 ? 1 : PassesCombo.SelectedIndex == 1 ? 3 : 7;
            
            if (MessageBox.Show($"¿Destruir permanentemente TODOS LOS ARCHIVOS en la carpeta con {passes} pases?\n\n{path}\n\nESTO NO SE PUEDE DESHACER.", 
                "Confirmación Peligrosa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            _logs.Clear();
            ResultLbl.Text = "Destruyendo carpeta...";
            ResultLbl.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
            ShredProgressBar.Visibility = Visibility.Visible;
            PathEntry.IsEnabled = false;
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var progress = new Progress<string>(msg => 
            {
                if (_logs.Count > 100) _logs.RemoveAt(0); // limits UI lag
                _logs.Add(msg);
            });

            var res = await Task.Run(() => _engine.ShredFolderAsync(path, passes, progress, _cts.Token));

            ShredProgressBar.Visibility = Visibility.Collapsed;
            ResultLbl.Text = $"{res.shredded} archivos destruidos ({CleanerEngine.FormatSize(res.totalBytes)}). {res.failed} fallidos.";
            ResultLbl.Foreground = res.failed > 0 ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.MediumSpringGreen;
            PathEntry.Text = "";
            PathEntry.IsEnabled = true;
        }
    }
}
