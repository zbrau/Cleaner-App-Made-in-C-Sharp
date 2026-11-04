using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class FirewallView : UserControl
    {
        private readonly ToolsEngine _engine = new();
        private List<FirewallRuleItem> _allRules = new();

        public FirewallView()
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
            RulesList.ItemsSource = null;
            StatusLbl.Text = "Cargando reglas a través de COM...";
            var items = await Task.Run(() => _engine.GetFirewallRules());
            _allRules = items;
            
            if (_allRules.Count == 0)
            {
                StatusLbl.Text = "No se pudieron obtener las reglas (requiere permisos de Administrador o COM no disponible).";
                StatusLbl.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                StatusLbl.Text = "";
                FilterList();
            }
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
                RulesList.ItemsSource = _allRules;
            }
            else
            {
                RulesList.ItemsSource = _allRules.Where(p => p.Name.ToLower().Contains(query)).ToList();
            }
        }
    }
}
