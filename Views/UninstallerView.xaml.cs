using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class UninstallerView : UserControl
    {
        private readonly ToolsEngine _engine = new();
        private List<AppItem> _allApps = new();

        public UninstallerView()
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
            LoadingCard.Visibility = Visibility.Visible;
            AppList.ItemsSource = null;
            var items = await Task.Run(() => _engine.GetInstalledApps());
            _allApps = items;
            LoadingCard.Visibility = Visibility.Collapsed;
            FilterList();
        }

        private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterList();
        }

        private void FilterList()
        {
            string query = SearchEntry.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(query))
            {
                AppList.ItemsSource = _allApps;
            }
            else
            {
                AppList.ItemsSource = _allApps.Where(p => p.Name.ToLower().Contains(query) || p.Publisher.ToLower().Contains(query)).ToList();
            }
        }

        private async void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AppItem app)
            {
                if (MessageBox.Show($"¿Desinstalar {app.Name}?\n\nPrimero se abrirá su desinstalador oficial, complétalo y luego el programa escaneará archivos residuales.", 
                    "Desinstalar", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    _engine.RunUninstaller(app.UninstallString);
                    
                    MessageBox.Show("Presiona OK cuando el desinstalador oficial haya terminado por completo.", "Esperando Confirmación", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Scan leftovers
                    var leftovers = await Task.Run(() => _engine.FindLeftoverFolders(app.Name, app.Publisher));
                    if (leftovers.Count > 0)
                    {
                        var msg = "¿Eliminar carpetas residuales sobrantes?\n\n" + string.Join("\n", leftovers.Take(5));
                        if (leftovers.Count > 5) msg += $"\n...y {leftovers.Count - 5} más.";

                        if (MessageBox.Show(msg, "Limpiar Sobras", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            foreach (var f in leftovers)
                            {
                                try { Directory.Delete(f, true); } catch { }
                            }
                            MessageBox.Show("Sobras eliminadas con éxito.", "Mantenimiento", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No se encontraron rastros residuales en el sistema.", "Limpio", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    RefreshData();
                }
            }
        }
    }
}
