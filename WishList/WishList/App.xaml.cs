using System.Windows;
using WishList.Data.SwitchTheme;

namespace WishList
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Загружаем сохраненную тему при запуске
            ThemeManager.LoadSavedTheme();
        }

        // Метод для переключения темы из ViewModel
        public void SetCurrentTheme()
        {
            ThemeManager.ToggleTheme();
        }
    }
}