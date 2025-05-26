using System.Globalization;

namespace SourceCodeAnalyzer.UI.Converters;

public class NumberFormatConverter : ValueConverter
{
    public CultureInfo Culture { get; set; } = new CultureInfo("en-IN");

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        // Try to parse the input value as a number
        if (double.TryParse(value.ToString(), out double number))
        {
            // If parameter is provided, use it as format string
            if (parameter is string format)
            {
                return number.ToString(format, Culture);
            }

            // Default formatting: #,##,##0.00
            return number.ToString("#,##,##0.##", Culture);
        }

        return value.ToString();
    }

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return 0;

        // Try to parse the string back to number using Indian culture
        if (double.TryParse(value.ToString(), NumberStyles.Any, Culture, out double result))
        {
            return result;
        }

        return 0;
    }
}