using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WishList.Views.AdminView.UseControl
{
    /// <summary>
    /// Логика взаимодействия для TasksView.xaml
    /// </summary>
    public partial class TasksView : UserControl
    {
        public TasksView()
        {
            InitializeComponent();
        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Блокируем прокрутку колесиком мыши в выпадающем списке
            e.Handled = true;
        }

        // Проверка на ввод только цифр
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры и одну точку
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // Блокировка букв и символов при нажатии клавиш
        private void BlockLettersAndSymbols(object sender, KeyEventArgs e)
        {
            // Блокируем буквы и символы, разрешаем цифры, Backspace, Delete, Enter, Tab
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
