using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VNCManager.Converters;

public class BoolVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class InverseBoolVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "在线" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(41, 160, 92)),
            "离线" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 76, 76)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(137, 148, 165))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
