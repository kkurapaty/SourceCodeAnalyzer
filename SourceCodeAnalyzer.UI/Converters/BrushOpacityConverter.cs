using System.Globalization;
using System.Windows.Media;

namespace SourceCodeAnalyzer.UI.Converters
{
    public class BrushOpacityConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var opacity = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
                return new SolidColorBrush(brush.Color)
                {
                    Opacity = opacity
                };
            }
            return value;
        }
    }
}
