using System.Windows;
using System.Windows.Input;
using WishList.Services.Manager;
using WishList.ViewModel.ManagerViewModel;

namespace WishList.Views.ManagerView
{
    public partial class ManagerWindow : Window
    {
        public ManagerWindow()
        {
            InitializeComponent();
            DataContext = new ManagerWindowViewModel();
            UpdateMaximizeButton();
        }

        private void UpdateMaximizeButton()
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.Content = "🗗";
                MaximizeButton.ToolTip = "Восстановить";
            }
            else
            {
                MaximizeButton.Content = "🗖";
                MaximizeButton.ToolTip = "Развернуть";
            }
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

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            TabItemDragDropBehavior.HandleWindowDragDrop(sender, e);
        }
    }
}