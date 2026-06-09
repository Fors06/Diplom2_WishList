using System;
using System.Globalization;
using System.Windows.Data;

namespace WishList.Data.Convertes
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверка на null
            if (value == null)
                return false;

            // Проверка на правильный тип
            if (value is bool boolValue)
                return !boolValue;

            // Если значение не bool, пробуем преобразовать
            try
            {
                bool parsed = System.Convert.ToBoolean(value);
                return !parsed;
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true;

            if (value is bool boolValue)
                return !boolValue;

            try
            {
                bool parsed = System.Convert.ToBoolean(value);
                return !parsed;
            }
            catch
            {
                return true;
            }
        }
    }
}