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
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty TogglePasswordCommandProperty =
            DependencyProperty.RegisterAttached(
                "TogglePasswordCommand",
                typeof(ICommand),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(null));

        public static void SetTogglePasswordCommand(PasswordBox element, ICommand value)
        {
            element.SetValue(TogglePasswordCommandProperty, value);
        }

        public static ICommand GetTogglePasswordCommand(PasswordBox element)
        {
            return (ICommand)element.GetValue(TogglePasswordCommandProperty);
        }

        public static readonly DependencyProperty IsPasswordVisibleProperty =
            DependencyProperty.RegisterAttached(
                "IsPasswordVisible",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnIsPasswordVisibleChanged));

        public static void SetIsPasswordVisible(PasswordBox element, bool value)
        {
            element.SetValue(IsPasswordVisibleProperty, value);
        }

        public static bool GetIsPasswordVisible(PasswordBox element)
        {
            return (bool)element.GetValue(IsPasswordVisibleProperty);
        }

        private static void OnIsPasswordVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChar = (bool)e.NewValue ? '\0' : '●';
            }
        }
    }
}