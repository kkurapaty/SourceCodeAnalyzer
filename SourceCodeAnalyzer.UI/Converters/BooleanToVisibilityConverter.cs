using System.Globalization;
using System.Windows;

namespace SourceCodeAnalyzer.UI.Converters
{
    public class BooleanToVisibilityConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            if (parameter != null && parameter.ToString().ToLower() == "invert")
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
