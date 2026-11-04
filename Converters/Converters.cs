using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AppCleanerPro.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBoolToVisibilityConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public static readonly EmptyStringToVisibilityConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => string.IsNullOrEmpty(v as string) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class ProgressWidthConverter : IValueConverter
    {
        public static readonly ProgressWidthConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is double d ? d * 3.0 : 0.0;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class StringToBrushConverter : IValueConverter
    {
        public static readonly StringToBrushConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(v?.ToString() ?? "#7B9FFF")); }
            catch { return new SolidColorBrush(Color.FromRgb(123, 159, 255)); }
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
