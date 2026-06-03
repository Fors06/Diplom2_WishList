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

        // Добавьте эти методы в ManagerWindow.xaml.cs
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void BlockLettersAndSymbols(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) &&
                !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) &&
                e.Key != Key.Back && e.Key != Key.Delete &&
                e.Key != Key.Enter && e.Key != Key.Tab && e.Key != Key.Decimal)
            {
                e.Handled = true;
            }
        }
    }
}