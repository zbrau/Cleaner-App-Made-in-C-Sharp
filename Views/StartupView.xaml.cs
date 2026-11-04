using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class StartupView : UserControl
    {
        private readonly ToolsEngine _engine = new();

        public StartupView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private async void RefreshData()
        {
            StartupList.ItemsSource = null;
            var items = await Task.Run(() => _engine.GetStartupItems());
            StartupList.ItemsSource = items;
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is StartupItem item)
            {
                if (MessageBox.Show($"¿Eliminar {item.Name} del inicio de Windows?", "Desactivar Arranque", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool ok = _engine.DeleteStartupItem(item);
                    if (ok) RefreshData();
                    else MessageBox.Show("Error eliminando clave (Requiere permisos de Administrador).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
