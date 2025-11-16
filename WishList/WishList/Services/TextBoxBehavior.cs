using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WishList.Services
{
    public static class TextBoxBehavior
    {
        #region IsWatermarkVisible Property
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
        #endregion

        #region MonitorText Property
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
        #endregion

        #region WatermarkText Property
        public static readonly DependencyProperty WatermarkTextProperty =
            DependencyProperty.RegisterAttached("WatermarkText", typeof(string), typeof(TextBoxBehavior),
                new PropertyMetadata("Введите пароль"));

        public static string GetWatermarkText(DependencyObject obj)
        {
            return (string)obj.GetValue(WatermarkTextProperty);
        }

        public static void SetWatermarkText(DependencyObject obj, string value)
        {
            obj.SetValue(WatermarkTextProperty, value);
        }
        #endregion

        #region IsPasswordVisible Property (для управления видимостью пароля)
        public static readonly DependencyProperty IsPasswordVisibleProperty =
            DependencyProperty.RegisterAttached("IsPasswordVisible", typeof(bool), typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnIsPasswordVisibleChanged));

        public static bool GetIsPasswordVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPasswordVisibleProperty);
        }

        public static void SetIsPasswordVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPasswordVisibleProperty, value);
        }

        private static void OnIsPasswordVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                // Здесь можно добавить логику изменения отображения текста
                // Например, менять FontFamily для отображения звездочек
                UpdatePasswordDisplay(textBox, (bool)e.NewValue);
            }
        }

        private static void UpdatePasswordDisplay(TextBox textBox, bool isPasswordVisible)
        {
            if (!isPasswordVisible)
            {
                // Режим скрытого пароля - заменяем текст на звездочки
                textBox.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            }
            else
            {
                // Режим видимого пароля - обычный шрифт
                textBox.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            }
        }
        #endregion

        #region TogglePasswordCommand Property
        public static readonly DependencyProperty TogglePasswordCommandProperty =
            DependencyProperty.RegisterAttached("TogglePasswordCommand", typeof(ICommand), typeof(TextBoxBehavior),
                new PropertyMetadata(null));

        public static ICommand GetTogglePasswordCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(TogglePasswordCommandProperty);
        }

        public static void SetTogglePasswordCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(TogglePasswordCommandProperty, value);
        }
        #endregion
    }
}
