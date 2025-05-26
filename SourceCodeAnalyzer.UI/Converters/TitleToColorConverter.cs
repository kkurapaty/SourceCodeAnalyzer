using System.Globalization;
using System.Windows.Media;

namespace SourceCodeAnalyzer.UI.Converters
{
    public class TitleToColorConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string title = value as string;
            return title switch
            {
                "Total Lines" => new SolidColorBrush(Color.FromRgb(70, 130, 180)),   // SteelBlue
                "Source Lines" => new SolidColorBrush(Color.FromRgb(60, 179, 113)),    // MediumSeaGreen
                "Comment Lines" => new SolidColorBrush(Color.FromRgb(238, 130, 238)),  // Violet
                "Blank Lines" => new SolidColorBrush(Color.FromRgb(255, 165, 0)),      // Orange
                _ => new SolidColorBrush(Color.FromRgb(100, 149, 237))                // CornflowerBlue
            };
        }
    }
}