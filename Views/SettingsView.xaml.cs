using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AppCleanerPro.Core;
using AppCleanerPro.Models;
using AppCleanerPro.Services;

namespace AppCleanerPro.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SystemEngine _engine = new();
        private bool _loaded = false;
        private string _selectedTheme = "NovaDark";

        public SettingsView()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            if (_loaded) return;

            var settings = _engine.LoadSettings();

            ChkAutoScan.IsChecked = settings.AutoScanOnStartup;
            ChkCloseAfter.IsChecked = settings.CloseAfterClean;
            ChkUpdates.IsChecked = settings.CheckForUpdates;
            ChkConfirm.IsChecked = settings.ConfirmDeletions;
            ChkSound.IsChecked = settings.SoundEffects;

            var themeKey = string.IsNullOrWhiteSpace(settings.AccentColor) ? "NovaDark" : settings.AccentColor;
            if (themeKey.Equals("ElectricBlue", StringComparison.OrdinalIgnoreCase)) themeKey = "NovaDark";
            else if (themeKey.Equals("CyberpunkViolet", StringComparison.OrdinalIgnoreCase)) themeKey = "CyberpunkNeon";
            else if (themeKey.Equals("EmeraldMint", StringComparison.OrdinalIgnoreCase)) themeKey = "EmeraldMatrix";
            else if (themeKey.Equals("SunsetOrange", StringComparison.OrdinalIgnoreCase)) themeKey = "NovaDark";

            _selectedTheme = themeKey;
            HighlightThemeTile(_selectedTheme);

            _loaded = true;
        }

        private void ThemeTile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string key)
            {
                _selectedTheme = key;
                HighlightThemeTile(key);
                ThemeService.ApplyTheme(key);
            }
        }

        private void HighlightThemeTile(string key)
        {
            var defaultBorder = (Brush)new BrushConverter().ConvertFromString("#252F50")!;

            if (ThemeTileNovaDark != null)
            {
                ThemeTileNovaDark.BorderBrush = key.Equals("NovaDark", StringComparison.OrdinalIgnoreCase)
                    ? (Brush)new BrushConverter().ConvertFromString("#7B9FFF")!
                    : defaultBorder;
                ThemeTileNovaDark.BorderThickness = new Thickness(key.Equals("NovaDark", StringComparison.OrdinalIgnoreCase) ? 2 : 1);
            }

            if (ThemeTileNovaLight != null)
            {
                ThemeTileNovaLight.BorderBrush = key.Equals("NovaLight", StringComparison.OrdinalIgnoreCase)
                    ? (Brush)new BrushConverter().ConvertFromString("#2563EB")!
                    : defaultBorder;
                ThemeTileNovaLight.BorderThickness = new Thickness(key.Equals("NovaLight", StringComparison.OrdinalIgnoreCase) ? 2 : 1);
            }

            if (ThemeTileCyberpunk != null)
            {
                ThemeTileCyberpunk.BorderBrush = key.Equals("CyberpunkNeon", StringComparison.OrdinalIgnoreCase)
                    ? (Brush)new BrushConverter().ConvertFromString("#C084FC")!
                    : defaultBorder;
                ThemeTileCyberpunk.BorderThickness = new Thickness(key.Equals("CyberpunkNeon", StringComparison.OrdinalIgnoreCase) ? 2 : 1);
            }

            if (ThemeTileEmerald != null)
            {
                ThemeTileEmerald.BorderBrush = key.Equals("EmeraldMatrix", StringComparison.OrdinalIgnoreCase)
                    ? (Brush)new BrushConverter().ConvertFromString("#34D399")!
                    : defaultBorder;
                ThemeTileEmerald.BorderThickness = new Thickness(key.Equals("EmeraldMatrix", StringComparison.OrdinalIgnoreCase) ? 2 : 1);
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ApplyTheme(_selectedTheme);

            var settings = new AppSettings
            {
                AutoScanOnStartup = ChkAutoScan.IsChecked == true,
                CloseAfterClean = ChkCloseAfter.IsChecked == true,
                CheckForUpdates = ChkUpdates.IsChecked == true,
                ConfirmDeletions = ChkConfirm.IsChecked == true,
                Theme = "Dark",
                SoundEffects = ChkSound.IsChecked == true,
                AccentColor = _selectedTheme
            };

            _engine.SaveSettings(settings);

            SavedBadge.Visibility = Visibility.Visible;
            await Task.Delay(2500);
            SavedBadge.Visibility = Visibility.Collapsed;
        }
    }
}
