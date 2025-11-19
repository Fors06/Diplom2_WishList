using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace WishList.Data.SwitchTheme
{
    public static class SettingsManager
    {
        private const string settingsFilePath = "appsettings.json";

        public static AppSettings LoadAsync()
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                    return new AppSettings { IsDarkThemeSelected = false };

                var json = File.ReadAllText(settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings { IsDarkThemeSelected = false };
            }
            catch (Exception e)
            {
                Debug.WriteLine("Ошибка загрузки настроек: " + e.Message);
                return new AppSettings { IsDarkThemeSelected = false };
            }
        }

        public static void SaveAsync(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}