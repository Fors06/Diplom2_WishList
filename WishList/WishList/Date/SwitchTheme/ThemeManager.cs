using System.Windows;

namespace WishList.Date.SwitchTheme
{
    public static class ThemeManager
    {
        private const string DarkThemePath = "/Date/SwitchTheme/DarkTheme.xaml";
        private const string LightThemePath = "/Date/SwitchTheme/LightTheme.xaml";

        public static void SwitchTheme(bool isDarkTheme)
        {
            var app = Application.Current;
            if (app == null) return;

            // Создаем список известных путей к темам
            var knownThemePaths = new List<string>
    {
        "/Date/SwitchTheme/DarkTheme.xaml",
        "/Date/SwitchTheme/LightTheme.xaml"
    };

            // Удаляем существующие темы по известным путям
            var themesToRemove = app.Resources.MergedDictionaries
                .Where(d => d.Source != null && knownThemePaths.Contains(d.Source.OriginalString))
                .ToList();

            foreach (var theme in themesToRemove)
            {
                app.Resources.MergedDictionaries.Remove(theme);
            }

            // Добавляем новую тему
            var newThemePath = isDarkTheme ? DarkThemePath : LightThemePath;
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(newThemePath, UriKind.Relative)
            };

            app.Resources.MergedDictionaries.Add(newTheme);

            // Сохраняем настройки
            var settings = SettingsManager.LoadAsync();
            settings.IsDarkThemeSelected = isDarkTheme;
            SettingsManager.SaveAsync(settings);
        }

        public static void LoadSavedTheme()
        {
            var settings = SettingsManager.LoadAsync();
            SwitchTheme(settings.IsDarkThemeSelected);
        }

        public static bool GetCurrentTheme()
        {
            var settings = SettingsManager.LoadAsync();
            return settings.IsDarkThemeSelected;
        }
    }
}