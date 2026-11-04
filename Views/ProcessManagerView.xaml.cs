using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;
using AppCleanerPro.Models;

namespace AppCleanerPro.Views
{
    public partial class ProcessManagerView : UserControl
    {
        private readonly SecurityEngine _engine = new();
        private List<ProcessItem> _allProcesses = new();

        public ProcessManagerView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private async void RefreshProcesses()
        {
            ProcessList.ItemsSource = null;
            var items = await Task.Run(() => _engine.GetActiveProcesses());
            _allProcesses = items;
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
                ProcessList.ItemsSource = _allProcesses;
            }
            else
            {
                ProcessList.ItemsSource = _allProcesses.Where(p => p.Name.ToLower().Contains(query) || p.MainWindowTitle.ToLower().Contains(query)).ToList();
            }
        }

        private void KillBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int pid)
            {
                var proc = _allProcesses.FirstOrDefault(p => p.ProcessId == pid);
                if (proc == null) return;

                if (MessageBox.Show($"¿Forzar el cierre de {proc.Name} (PID: {pid})?\n\nPuede causar inestabilidad si es un proceso del sistema.", 
                    "Terminar Proceso", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool success = _engine.KillProcess(pid);
                    if (success)
                    {
                        RefreshProcesses();
                    }
                    else
                    {
                        MessageBox.Show("Acceso denegado o el proceso ya se cerró.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
