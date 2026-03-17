using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GoldSystem.WPF.Converters;

/// <summary>Converts a bool to Visibility (true → Visible, false → Collapsed).</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Converts a bool to sidebar width (true → 200, false → 56).</summary>
[ValueConversion(typeof(bool), typeof(double))]
public sealed class BoolToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 200.0 : 56.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true when the bound string equals the ConverterParameter.
/// Used to set IsChecked on sidebar RadioButtons based on SelectedMenuItem.
/// </summary>
[ValueConversion(typeof(string), typeof(bool))]
public sealed class MenuItemMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && parameter is string p && s == p;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? parameter : Binding.DoNothing;
}

/// <summary>
/// Converts a nullable object to Visibility.
/// null → Collapsed, non-null → Visible.
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s) return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts an integer count to Visibility.
/// 0 → Visible (shows empty-state placeholder), &gt;0 → Collapsed.
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a non-empty string to Visibility.Visible, empty/null to Visibility.Collapsed.
/// Used to show validation messages only when there is text to display.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Equality converter: returns true when the bound value equals the ConverterParameter string.
/// Used to bind payment-mode RadioButton.IsChecked to the SelectedPaymentMode string property.
/// ConvertBack returns the parameter value when checked (sets the property to the mode string).
/// </summary>
[ValueConversion(typeof(string), typeof(bool))]
public sealed class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && parameter is string p && s == p;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? parameter?.ToString() ?? string.Empty : Binding.DoNothing;
}

/// <summary>
/// Returns Red brush when the bound bool is true (error state), otherwise the default text brush.
/// </summary>
[ValueConversion(typeof(bool), typeof(System.Windows.Media.Brush))]
public sealed class BoolToErrorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? System.Windows.Media.Brushes.Red
            : (object)System.Windows.SystemColors.ControlTextBrush;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

