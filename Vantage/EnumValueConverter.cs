using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

using Avalonia.Data.Converters;

namespace Vantage;

public class EnumValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || !value.GetType().IsEnum)
        {
            return null;
        }

        var enumType = value.GetType();
        if (parameter is string mode && mode == "Label")
        {
            return GetEnumLabel(enumType, value);
        }

        return Enum.GetValues(enumType).Cast<object>().ToArray();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        if (value.GetType().IsEnum)
        {
            return value;
        }

        var property = value.GetType().GetProperty("Value");
        return property != null ? property.GetValue(value) : null;
    }

    private static string GetEnumLabel(Type enumType, object enumValue)
    {
        var fieldInfo = enumType.GetField(enumValue.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<EditorLabelAttribute>();
        return attribute?.Label ?? enumValue.ToString();
    }
}
