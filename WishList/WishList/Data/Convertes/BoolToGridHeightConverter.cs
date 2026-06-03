// Добавьте в папку WishList.Data.Convertes:

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WishList.Data.Convertes
{
    public class BoolToGridHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFormVisible && isFormVisible)
            {
                // Когда форма видна - таблица занимает фиксированную высоту (половина)
                return new GridLength(250, GridUnitType.Star);
            }
            // Когда форма скрыта - таблица растянута на всё доступное пространство
            return new GridLength(1, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}