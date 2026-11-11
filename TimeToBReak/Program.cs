// مسیر فایل: Program.cs
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace T2B
{
	internal static class Program
	{
		private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string AppRunName = "BehzadRest";

		[STAThread]
		private static void Main()
		{
			// Mutex منحصر به کاربر جاری تا در صورت Switch User اختلال پیش نیاید
			string userMutexName = $@"Local\T2B_BreakTime_{Environment.UserName}";
			using var mutex = new Mutex(true, userMutexName, out bool createdNew);

			if (!createdNew)
			{
				return; // برنامه در این یوزر در حال اجرا است
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			EnsureStartupRegistered();

			Application.Run(new BreakApplicationContext());
		}

		public static void EnsureStartupRegistered()
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
				if (key == null) return;

				string exePath = $"\"{Application.ExecutablePath}\"";
				var currentVal = key.GetValue(AppRunName) as string;

				if (string.IsNullOrEmpty(currentVal) || !string.Equals(currentVal, exePath, StringComparison.OrdinalIgnoreCase))
				{
					key.SetValue(AppRunName, exePath);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Registry self-healing error: {ex.Message}");
			}
		}
	}
}
