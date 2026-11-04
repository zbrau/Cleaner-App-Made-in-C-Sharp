using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace AppCleanerPro.Services
{
    public class FullTheme
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string BgDeep { get; set; } = "";
        public string BgSurface { get; set; } = "";
        public string BgSidebar { get; set; } = "";
        public string BgCard { get; set; } = "";
        public string BgCardHov { get; set; } = "";
        public string BorderCard { get; set; } = "";
        public string TextPrimary { get; set; } = "";
        public string TextSecond { get; set; } = "";
        public string TextMuted { get; set; } = "";
        public string AccentPrimary { get; set; } = "";
        public string AccentSecondary { get; set; } = "";
        public bool IsLight { get; set; } = false;
    }

    public static class ThemeService
    {
        public static readonly List<FullTheme> AvailableThemes = new()
        {
            new()
            {
                Key = "NovaDark",
                Name = "Nova Dark",
                Subtitle = "Oscuro Espacial",
                BgDeep = "#080A12",
                BgSurface = "#0E1020",
                BgSidebar = "#090B16",
                BgCard = "#111627",
                BgCardHov = "#161D35",
                BorderCard = "#1E2540",
                TextPrimary = "#E8EEFF",
                TextSecond = "#7B82A8",
                TextMuted = "#47506F",
                AccentPrimary = "#7B9FFF",
                AccentSecondary = "#4060CC"
            },
            new()
            {
                Key = "NovaLight",
                Name = "Nova Light",
                Subtitle = "Blanco Minimal",
                BgDeep = "#F1F4F9",
                BgSurface = "#FFFFFF",
                BgSidebar = "#E6ECF5",
                BgCard = "#FFFFFF",
                BgCardHov = "#EBF0F8",
                BorderCard = "#D1DAE8",
                TextPrimary = "#182033",
                TextSecond = "#556382",
                TextMuted = "#8B97B0",
                AccentPrimary = "#2563EB",
                AccentSecondary = "#1D4ED8",
                IsLight = true
            },
            new()
            {
                Key = "CyberpunkNeon",
                Name = "Cyberpunk",
                Subtitle = "Púrpura & Neón",
                BgDeep = "#0D0819",
                BgSurface = "#140C27",
                BgSidebar = "#0F091E",
                BgCard = "#191032",
                BgCardHov = "#231745",
                BorderCard = "#34225E",
                TextPrimary = "#F5EDFF",
                TextSecond = "#A999C7",
                TextMuted = "#6A5A8A",
                AccentPrimary = "#C084FC",
                AccentSecondary = "#7C3AED"
            },
            new()
            {
                Key = "EmeraldMatrix",
                Name = "Emerald",
                Subtitle = "Verde Stealth",
                BgDeep = "#050E0B",
                BgSurface = "#091813",
                BgSidebar = "#06120E",
                BgCard = "#0E221B",
                BgCardHov = "#143026",
                BorderCard = "#1A4234",
                TextPrimary = "#EDFDF5",
                TextSecond = "#7CD4B1",
                TextMuted = "#427863",
                AccentPrimary = "#34D399",
                AccentSecondary = "#059669"
            }
        };

        public static void ApplyTheme(string themeKey)
        {
            var theme = AvailableThemes.Find(t => t.Key.Equals(themeKey, StringComparison.OrdinalIgnoreCase))
                        ?? (themeKey == "ElectricBlue" ? AvailableThemes[0] :
                            themeKey == "CyberpunkViolet" ? AvailableThemes[2] :
                            themeKey == "EmeraldMint" ? AvailableThemes[3] : AvailableThemes[0]);

            try
            {
                var primaryColor = (Color)ColorConverter.ConvertFromString(theme.AccentPrimary);
                var secondaryColor = (Color)ColorConverter.ConvertFromString(theme.AccentSecondary);

                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["BgDeep"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BgDeep));
                    Application.Current.Resources["BgSurface"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BgSurface));
                    Application.Current.Resources["BgSidebar"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BgSidebar));
                    Application.Current.Resources["BgCard"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BgCard));
                    Application.Current.Resources["BgCardHov"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BgCardHov));
                    Application.Current.Resources["BorderCard"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BorderCard));
                    Application.Current.Resources["TextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.TextPrimary));
                    Application.Current.Resources["TextSecond"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.TextSecond));
                    Application.Current.Resources["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.TextMuted));

                    Application.Current.Resources["AccentBlue"] = new SolidColorBrush(primaryColor);
                    Application.Current.Resources["AccentViolet"] = new SolidColorBrush(secondaryColor);
                    Application.Current.Resources["AccentPrimary"] = new SolidColorBrush(primaryColor);
                    Application.Current.Resources["AccentSecondary"] = new SolidColorBrush(secondaryColor);
                }

                // Notify MainWindow directly to update background borders, indicators, and logo
                if (Application.Current?.MainWindow is MainWindow mw)
                {
                    mw.UpdateTheme(theme);
                }
            }
            catch { }
        }
    }
}
