using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;

namespace AppCleanerPro.Views
{
    public partial class SystemInfoView : UserControl
    {
        private readonly SystemEngine _engine = new();
        private bool _loaded = false;

        public SystemInfoView()
        {
            InitializeComponent();
        }

        public async void LoadInfo()
        {
            if (_loaded) return;
            
            var info = await _engine.GetSystemInfoAsync();
            
            LoadingPanel.Visibility = Visibility.Collapsed;
            InfoGrid.Visibility = Visibility.Visible;

            OsText.Text = info.OsName;
            ArchText.Text = $"Arquitectura: {info.OsArchitecture}";

            CpuText.Text = info.CpuName;
            CoresText.Text = $"Núcleos Lógicos: {info.CpuCores}";

            RamText.Text = info.RamTotal;
            GpuText.Text = info.GpuName;
            MoboText.Text = info.MoboModel;

            _loaded = true;
        }
    }
}
