using System;
using System.Windows;
using System.Windows.Input;

namespace WishList.Views.ProgrammerView
{
    public partial class ProgrammerWindow : Window
    {
        public ProgrammerWindow()
        {
            InitializeComponent();
            DataContext = new ProgrammerWindowViewModel();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeButton.Content = "🗖";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeButton.Content = "🗗";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (WindowState == WindowState.Maximized)
                    {
                        WindowState = WindowState.Normal;
                        MaximizeButton.Content = "🗖";
                    }
                    else
                    {
                        WindowState = WindowState.Maximized;
                        MaximizeButton.Content = "🗗";
                    }
                }
                else
                {
                    DragMove();
                }
            }
        }
    }
}