using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WishList.Services
{
    public static class TextBoxBehavior
    {
        public static readonly DependencyProperty IsWatermarkVisibleProperty =
            DependencyProperty.RegisterAttached("IsWatermarkVisible", typeof(bool), typeof(TextBoxBehavior),
                new PropertyMetadata(true));

        public static bool GetIsWatermarkVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsWatermarkVisibleProperty);
        }

        public static void SetIsWatermarkVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsWatermarkVisibleProperty, value);
        }

        public static readonly DependencyProperty MonitorTextProperty =
            DependencyProperty.RegisterAttached("MonitorText", typeof(bool), typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnMonitorTextChanged));

        public static bool GetMonitorText(DependencyObject obj)
        {
            return (bool)obj.GetValue(MonitorTextProperty);
        }

        public static void SetMonitorText(DependencyObject obj, bool value)
        {
            obj.SetValue(MonitorTextProperty, value);
        }

        private static void OnMonitorTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= TextBox_TextChanged;
                if ((bool)e.NewValue)
                {
                    textBox.TextChanged += TextBox_TextChanged;
                    // Инициализируем видимость при загрузке
                    SetIsWatermarkVisible(textBox, string.IsNullOrEmpty(textBox.Text));
                }
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                SetIsWatermarkVisible(textBox, string.IsNullOrEmpty(textBox.Text));
            }
        }
    }
}
