using System;
using System.IO; // اضافه شده برای حل مشکل File و Directory
using System.Text.Json;

namespace T2B
{
    public class Settings
    {
        public int BreakInterval { get; set; }
        public int BreakDuration { get; set; }

        private const string FilePath = "settings.json";

        public static Settings Load()
        {
            if (!File.Exists(FilePath))
            {
                var def = new Settings { BreakInterval = 30, BreakDuration = 2 };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(def));
                return def;
            }

            return JsonSerializer.Deserialize<Settings>(
                File.ReadAllText(FilePath)
            )!;
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
    }
}
