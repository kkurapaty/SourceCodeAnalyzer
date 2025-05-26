using System.Globalization;
using System.Windows.Data;

namespace SourceCodeAnalyzer.UI.Converters;

public abstract class ValueConverter : System.Windows.Data.IValueConverter
{
    public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}