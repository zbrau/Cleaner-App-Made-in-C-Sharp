using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class RegistryView : UserControl
    {
        private readonly ToolsEngine _engine = new();
        private List<RegistryIssue> _issues = new();

        public RegistryView()
        {
            InitializeComponent();
        }

        private async void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            FixBtn.IsEnabled = false;
            FixBtn.Opacity = 0.5;
            StatusLbl.Text = "";
            LoadingCard.Visibility = Visibility.Visible;
            RegList.ItemsSource = null;

            _issues = await Task.Run(() => _engine.ScanRegistry());
            LoadingCard.Visibility = Visibility.Collapsed;
            RegList.ItemsSource = _issues;

            if (_issues.Count == 0)
            {
                StatusLbl.Text = "¡Registro impecable! 0 errores encontrados.";
                StatusLbl.Foreground = System.Windows.Media.Brushes.MediumSpringGreen;
            }
            else
            {
                StatusLbl.Text = "";
                FixBtn.IsEnabled = true;
                FixBtn.Opacity = 1.0;
            }
        }

        private void FixBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_issues.Count == 0) return;

            if (MessageBox.Show($"¿Limpiar {_issues.Count} errores del registro de Windows?\n\nAcción segura: Solo borrará asociaciones vacías de archivos.", 
                "Reparar Registro", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                bool ok = _engine.FixRegistryIssues(_issues);
                MessageBox.Show(ok ? "Reparación completada con éxito." : "Completado con algunas omisiones (Permisos denegados).", "Registro", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Clear UI
                _issues.Clear();
                RegList.ItemsSource = null;
                FixBtn.IsEnabled = false;
                FixBtn.Opacity = 0.5;
                StatusLbl.Text = "Errores solucionados.";
                StatusLbl.Foreground = System.Windows.Media.Brushes.MediumSpringGreen;
            }
        }
    }
}
