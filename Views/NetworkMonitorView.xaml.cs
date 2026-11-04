using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using AppCleanerPro.Core;

namespace AppCleanerPro.Views
{
    public partial class NetworkMonitorView : UserControl
    {
        private readonly SecurityEngine _engine = new();

        public NetworkMonitorView()
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
            NetList.ItemsSource = null;
            var items = await Task.Run(() => _engine.GetNetworkConnections());
            NetList.ItemsSource = items;
        }
    }
}
