using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace T2B
{
	public class Settings
	{
		public int BreakInterval { get; set; } = 30;
		public int BreakDuration { get; set; } = 2;
		public int LastPlayedVideoIndex { get; set; } = -1;

		// مسیر دلخواه پوشه ویدیوها
		public string VideoFolderPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Videos");

		public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;
		public static string SettingsFilePath => Path.Combine(AppDirectory, "settings.json");
		
		public void EnsureVideosFolderExists()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(VideoFolderPath) && !Directory.Exists(VideoFolderPath))
				{
					Directory.CreateDirectory(VideoFolderPath);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error creating Videos folder: {ex.Message}");
			}
		}

		public static Settings Load()
		{
			try
			{
				if (File.Exists(SettingsFilePath))
				{
					var json = File.ReadAllText(SettingsFilePath);
					var settings = JsonSerializer.Deserialize<Settings>(json);
					if (settings != null)
					{
						if (settings.BreakInterval < 1) settings.BreakInterval = 30;
						if (settings.BreakDuration < 1) settings.BreakDuration = 2;
						if (string.IsNullOrWhiteSpace(settings.VideoFolderPath))
						{
							settings.VideoFolderPath = Path.Combine(AppDirectory, "Videos");
						}
						settings.EnsureVideosFolderExists();
						return settings;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
			}

			var defaultSettings = new Settings();
			defaultSettings.EnsureVideosFolderExists();
			defaultSettings.Save();
			return defaultSettings;
		}

		public void Save()
		{
			try
			{
				var options = new JsonSerializerOptions { WriteIndented = true };
				var json = JsonSerializer.Serialize(this, options);
				File.WriteAllText(SettingsFilePath, json);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
			}
		}

		/// <summary>
		/// دریافت تمام ویدیوهای موجود در پوشه تنظیم‌شده
		/// </summary>
		public List<string> GetAvailableVideos()
		{
			EnsureVideosFolderExists();
			try
			{
				if (string.IsNullOrWhiteSpace(VideoFolderPath) || !Directory.Exists(VideoFolderPath))
					return new List<string>();

				var supportedExtensions = new[] { ".mp4", ".webm", ".mkv" };
				return Directory.GetFiles(VideoFolderPath)
					.Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
					.OrderBy(f => f)
					.ToList();
			}
			catch
			{
				return new List<string>();
			}
		}

		/// <summary>
		/// دریافت آدرس IP محلی فعال سیستم (IPv4)
		/// </summary>
		public static string GetLocalIPAddress()
		{
			try
			{
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
					{
						return ip.ToString();
					}
				}
			}
			catch
			{
				// نادیده گرفتن خطا و بازگشت لوکال‌هاست
			}
			return "127.0.0.1";
		}
	}
}
