using System.Globalization;

namespace SourceCodeAnalyzer.UI.Converters;

public class BooleanInverter : ValueConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = (bool)value;
        return !boolValue;
    }
}