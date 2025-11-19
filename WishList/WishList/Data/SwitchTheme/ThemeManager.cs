using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace WishList.Data.SwitchTheme
{
    public static class ThemeManager
    {
        private const string DarkThemePath = "/WishList;component/Data/SwitchTheme/DarkTheme.xaml";
        private const string LightThemePath = "/WishList;component/Data/SwitchTheme/LightTheme.xaml";

        public static void SwitchTheme(bool isDarkTheme)
        {
            var app = Application.Current;
            if (app == null) return;

            try
            {
                // Очищаем существующие темы
                app.Resources.MergedDictionaries.Clear();

                // Добавляем новую тему
                var newThemePath = isDarkTheme ? DarkThemePath : LightThemePath;
                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(newThemePath, UriKind.Relative)
                };

                app.Resources.MergedDictionaries.Add(newTheme);

                // Пересоздаем главное окно для мгновенного применения темы
                RecreateMainWindow();

                // Сохраняем настройки
                var settings = SettingsManager.LoadAsync();
                settings.IsDarkThemeSelected = isDarkTheme;
                SettingsManager.SaveAsync(settings);

                Debug.WriteLine($"Тема переключена на: {(isDarkTheme ? "Тёмную" : "Светлую")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка переключения темы: {ex.Message}");
            }
        }

        private static void RecreateMainWindow()
        {
            var app = Application.Current;
            var mainWindow = app.MainWindow;

            if (mainWindow != null && mainWindow.DataContext != null)
            {
                // Сохраняем ViewModel
                var viewModel = mainWindow.DataContext;

                // Создаем новое окно
                var newWindow = new WishList.Views.AdminView.AdminWindow();
                newWindow.DataContext = viewModel;

                // Закрываем старое окно
                mainWindow.Close();

                // Показываем новое окно
                newWindow.Show();
                app.MainWindow = newWindow;
            }
        }

        public static void LoadSavedTheme()
        {
            var settings = SettingsManager.LoadAsync();

            // Применяем тему без пересоздания окна при старте
            var app = Application.Current;
            if (app != null)
            {
                app.Resources.MergedDictionaries.Clear();

                var themePath = settings.IsDarkThemeSelected ? DarkThemePath : LightThemePath;
                var theme = new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.Relative)
                };

                app.Resources.MergedDictionaries.Add(theme);
            }
        }

        public static bool GetCurrentTheme()
        {
            var settings = SettingsManager.LoadAsync();
            return settings.IsDarkThemeSelected;
        }

        public static void ToggleTheme()
        {
            var currentTheme = GetCurrentTheme();
            SwitchTheme(!currentTheme);
        }
    }
}