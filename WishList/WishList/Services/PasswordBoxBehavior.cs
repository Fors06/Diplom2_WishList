using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WishList.Services
{
    public static class PasswordBoxBehavior
    {
        public static readonly DependencyProperty IsWatermarkVisibleProperty =
            DependencyProperty.RegisterAttached("IsWatermarkVisible", typeof(bool), typeof(PasswordBoxBehavior),
                new PropertyMetadata(true));

        public static bool GetIsWatermarkVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsWatermarkVisibleProperty);
        }

        public static void SetIsWatermarkVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsWatermarkVisibleProperty, value);
        }

        public static readonly DependencyProperty MonitorPasswordProperty =
            DependencyProperty.RegisterAttached("MonitorPassword", typeof(bool), typeof(PasswordBoxBehavior),
                new PropertyMetadata(false, OnMonitorPasswordChanged));

        public static bool GetMonitorPassword(DependencyObject obj)
        {
            return (bool)obj.GetValue(MonitorPasswordProperty);
        }

        public static void SetMonitorPassword(DependencyObject obj, bool value)
        {
            obj.SetValue(MonitorPasswordProperty, value);
        }

        private static void OnMonitorPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                SetIsWatermarkVisible(passwordBox, string.IsNullOrEmpty(passwordBox.Password));
            }
        }

        public static readonly DependencyProperty WatermarkTextProperty =
    DependencyProperty.RegisterAttached("WatermarkText", typeof(string), typeof(PasswordBoxBehavior),
        new PropertyMetadata("Введите пароль"));

        public static string GetWatermarkText(DependencyObject obj)
        {
            return (string)obj.GetValue(WatermarkTextProperty);
        }

        public static void SetWatermarkText(DependencyObject obj, string value)
        {
            obj.SetValue(WatermarkTextProperty, value);
        }
    }
}