using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AppCleanerPro.Core;

namespace AppCleanerPro.Views
{
    public partial class HistoryView : UserControl
    {
        private readonly CleanerEngine _engine = new();
        private bool _loaded = false;

        public HistoryView()
        {
            InitializeComponent();
        }

        public void LoadHistory()
        {
            if (_loaded) return;
            
            var hist = _engine.GetHistory().OrderByDescending(h => h.Timestamp).ToList();
            if (hist.Count == 0)
            {
                NoHistoryText.Visibility = Visibility.Visible;
                HistoryList.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoHistoryText.Visibility = Visibility.Collapsed;
                HistoryList.Visibility = Visibility.Visible;
                HistoryList.ItemsSource = hist.Select(h => new
                {
                    h.Date,
                    SizeStr = "+" + CleanerEngine.FormatSize(h.Bytes)
                }).ToList();
            }

            _loaded = true;
        }
    }
}
