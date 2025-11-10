using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WishList.Date.SwitchTheme
{
    public static class SettingsManager
    {
        private const string settingsFilePath = "appsettings.json"; // Путь к файлу настроек

        //public static async Task<AppSettings> LoadAsync()
        //{
        //    try
        //    {
        //        using var fs = File.OpenRead(settingsFilePath);
        //        return await JsonSerializer.DeserializeAsync<AppSettings>(fs);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error loading settings: {ex.Message}");
        //        return new AppSettings(); // Возвращаем дефолтные настройки
        //    }
        //}

        //public static async Task SaveAsync(AppSettings settings)
        //{
        //    try
        //    {
        //        using var fs = File.Create(settingsFilePath);
        //        await JsonSerializer.SerializeAsync(fs, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error saving settings: {ex.Message}");
        //    }
        //}

        public static AppSettings LoadAsync()
        {
            try
            {
                if (!File.Exists(settingsFilePath)) return new AppSettings(); // Дефолтные настройки

                using var stream = File.OpenRead(settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(stream)!;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Ошибка загрузки настроек: " + e.Message);
                return new AppSettings(); // Возвращаем дефолтные настройки
            }
        }

        public static void SaveAsync(AppSettings settings)
        {
            try
            {
                using var fs = File.Create(settingsFilePath);
                JsonSerializer.SerializeAsync(fs, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
