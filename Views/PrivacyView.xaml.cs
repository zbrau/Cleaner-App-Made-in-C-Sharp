using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AppCleanerPro.Core;

namespace AppCleanerPro.Views
{
    public partial class PrivacyView : UserControl
    {
        private readonly SecurityEngine _engine = new();
        private CancellationTokenSource? _cts;

        public PrivacyView()
        {
            InitializeComponent();
            PathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Seleccionar Carpeta para Escanear" };
            if (dlg.ShowDialog() == true)
            {
                PathEntry.Text = dlg.FolderName;
            }
        }

        private async void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = PathEntry.Text.Trim();
            if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path)) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            StatusLbl.Text = "";
            PrivacyProgressText.Text = "Iniciando análisis de privacidad...";
            PrivacyProgressBar.Value = 0;
            LoadingCard.Visibility = Visibility.Visible;
            ResultsList.ItemsSource = null;
            PathEntry.IsEnabled = false;

            var progress = new Progress<int>(pct =>
            {
                PrivacyProgressText.Text = $"Escaneando ({pct}%)...";
                PrivacyProgressBar.Value = pct;
            });
            
            try
            {
                var matches = await _engine.ScanPrivacyFilesAsync(path, progress, _cts.Token);
                ResultsList.ItemsSource = matches;
                
                if (matches.Count == 0)
                {
                    StatusLbl.Text = "¡Ningún archivo expuesto encontrado!";
                    StatusLbl.Foreground = System.Windows.Media.Brushes.MediumSpringGreen;
                }
                else
                {
                    StatusLbl.Text = string.Empty; // Hide status text, show items
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                LoadingCard.Visibility = Visibility.Collapsed;
                PathEntry.IsEnabled = true;
            }
        }
    }
}
