using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WishList.ViewModel.AdminViewModel;

namespace WishList.Views.AdminView
{
    /// <summary>
    /// Логика взаимодействия для AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            DataContext = new MainAdminViewModel();
            //this.StateChanged += AdminWindow_StateChanged;
            UpdateMaximizeButton();
        }

        //private void AdminWindow_StateChanged(object sender, EventArgs e)
        //{
        //    UpdateMaximizeButton();
        //}

        private void UpdateMaximizeButton()
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.Content = "🗗"; // Restore icon
                MaximizeButton.ToolTip = "Восстановить";
            }
            else
            {
                MaximizeButton.Content = "🗖"; // Maximize icon
                MaximizeButton.ToolTip = "Развернуть";
            }
        }

        // Window control buttons
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
            UpdateMaximizeButton();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    // Двойной клик - максимизация/восстановление
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
                else
                {
                    DragMove();
                }
            }
        }
    }
}
