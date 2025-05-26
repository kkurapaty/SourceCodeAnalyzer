using System.Globalization;
using System.Windows;

namespace SourceCodeAnalyzer.UI.Converters;

public class CountToVisibilityConverter : ValueConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
}