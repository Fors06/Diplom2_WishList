using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WishList.Services
{
    public class TextBoxPasswordHelper
    {
        public static readonly DependencyProperty IsPasswordProperty =
            DependencyProperty.RegisterAttached("IsPassword", typeof(bool), typeof(TextBoxPasswordHelper),
                new PropertyMetadata(false, OnIsPasswordChanged));

        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.RegisterAttached("PasswordChar", typeof(char), typeof(TextBoxPasswordHelper),
                new PropertyMetadata('•'));

        public static bool GetIsPassword(TextBox obj) => (bool)obj.GetValue(IsPasswordProperty);
        public static void SetIsPassword(TextBox obj, bool value) => obj.SetValue(IsPasswordProperty, value);

        public static char GetPasswordChar(TextBox obj) => (char)obj.GetValue(PasswordCharProperty);
        public static void SetPasswordChar(TextBox obj, char value) => obj.SetValue(PasswordCharProperty, value);

        private static void OnIsPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    // Режим пароля
                    textBox.TextChanged += OnPasswordTextChanged;
                    UpdatePasswordDisplay(textBox);
                }
                else
                {
                    textBox.TextChanged -= OnPasswordTextChanged;
                }
            }
        }

        private static void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                UpdatePasswordDisplay(textBox);
            }
        }

        private static void UpdatePasswordDisplay(TextBox textBox)
        {
            // Здесь можно добавить логику для отображения символов пароля
        }
    }
}
