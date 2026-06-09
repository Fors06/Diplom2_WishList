using System;
using System.Diagnostics;
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

                // Получаем тип DataContext, чтобы определить, какое окно нужно создать
                Type viewModelType = viewModel.GetType();

                Window newWindow;

                // Определяем тип окна по типу ViewModel
                if (viewModelType.Name.Contains("Admin") || viewModelType.Name == "MainAdminViewModel")
                {
                    newWindow = new WishList.Views.AdminView.AdminWindow();
                }
                else if (viewModelType.Name.Contains("Manager") || viewModelType.Name == "ManagerWindowViewModel")
                {
                    newWindow = new WishList.Views.ManagerView.ManagerWindow();
                }
                else if (viewModelType.Name.Contains("Programmer") || viewModelType.Name == "ProgrammerWindowViewModel")
                {
                    newWindow = new WishList.Views.ProgrammerView.ProgrammerWindow();
                }
                else
                {
                    // Если тип неизвестен, пробуем определить по имени окна
                    string windowTypeName = mainWindow.GetType().Name;
                    if (windowTypeName.Contains("Admin"))
                    {
                        newWindow = new WishList.Views.AdminView.AdminWindow();
                    }
                    else if (windowTypeName.Contains("Manager"))
                    {
                        newWindow = new WishList.Views.ManagerView.ManagerWindow();
                    }
                    else if (windowTypeName.Contains("Programmer"))
                    {
                        newWindow = new WishList.Views.ProgrammerView.ProgrammerWindow();
                    }
                    else
                    {
                        newWindow = new WishList.Views.AdminView.AdminWindow();
                    }
                }

                newWindow.DataContext = viewModel;

                // Сохраняем состояние окна
                var windowState = mainWindow.WindowState;

                // Закрываем старое окно
                mainWindow.Close();

                // Показываем новое окно
                newWindow.WindowState = windowState;
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