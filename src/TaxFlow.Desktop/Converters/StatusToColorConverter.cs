using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TaxFlow.Core.Enums;

namespace TaxFlow.Desktop.Converters;

/// <summary>
/// Converts DocumentStatus to color brush
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Draft => new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Orange
                DocumentStatus.Valid => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                DocumentStatus.Validating => new SolidColorBrush(Color.FromRgb(156, 39, 176)), // Purple
                DocumentStatus.Signing => new SolidColorBrush(Color.FromRgb(156, 39, 176)), // Purple
                DocumentStatus.Submitting => new SolidColorBrush(Color.FromRgb(156, 39, 176)), // Purple
                DocumentStatus.Submitted => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                DocumentStatus.Accepted => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                DocumentStatus.Rejected => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                DocumentStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                DocumentStatus.Cancelled => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
                DocumentStatus.Invalid => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack not supported for color to status conversion
        throw new NotSupportedException("ConvertBack is not supported for StatusToColorConverter");
    }
}

/// <summary>
/// Converts amount to formatted currency string
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            return $"EGP {amount:N2}";
        }

        return "EGP 0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            str = str.Replace("EGP", "").Trim();
            if (decimal.TryParse(str, out var amount))
            {
                return amount;
            }
        }

        return 0m;
    }
}

/// <summary>
/// Converts null to boolean
/// </summary>
public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack not supported for null to boolean conversion
        throw new NotSupportedException("ConvertBack is not supported for NullToBooleanConverter");
    }
}

/// <summary>
/// Converts date to relative time string (e.g., "2 hours ago")
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack not supported for relative time conversion
        throw new NotSupportedException("ConvertBack is not supported for RelativeTimeConverter");
    }
}
