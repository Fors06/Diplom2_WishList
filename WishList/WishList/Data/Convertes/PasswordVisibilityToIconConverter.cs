using System.Globalization;
using System.Windows.Data;

namespace WishList.Data.Convertes
{
    public class PasswordVisibilityToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Простая логика для тестирования
            if (value is bool isVisible && isVisible)
            {
                return "👁️"; // Видимый пароль
            }
            return "👁️‍🗨️"; // Скрытый пароль
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}