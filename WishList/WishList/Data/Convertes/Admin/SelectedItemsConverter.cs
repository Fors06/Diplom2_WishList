using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace WishList.Data.Convertes.Admin
{
    public class SelectedItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable items)
            {
                var selectedItems = new List<string>();

                foreach (var item in items)
                {
                    // Используем рефлексию для получения свойств
                    var isSelectedProp = item.GetType().GetProperty("IsSelected");
                    var itemProp = item.GetType().GetProperty("Item");

                    if (isSelectedProp?.GetValue(item) is bool isSelected && isSelected)
                    {
                        var itemObject = itemProp?.GetValue(item);
                        if (itemObject != null)
                        {
                            // Пытаемся получить Name, FullName или CompanyName
                            var nameProp = itemObject.GetType().GetProperty("Name")
                                        ?? itemObject.GetType().GetProperty("CompanyName");

                            if (nameProp?.GetValue(itemObject) is string name && !string.IsNullOrEmpty(name))
                            {
                                selectedItems.Add(name);
                            }
                        }
                    }
                }

                return selectedItems.Any() ? string.Join(", ", selectedItems) : "Выберите...";
            }
            return "Выберите...";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}