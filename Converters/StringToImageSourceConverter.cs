using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AppCleanerPro.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    // Supports both Pack URIs and relative paths
                    Uri uri = path.StartsWith("pack://")
                        ? new Uri(path, UriKind.Absolute)
                        : new Uri($"pack://application:,,,/{path}", UriKind.Absolute);

                    return new BitmapImage(uri);
                }
                catch { }
            }
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
