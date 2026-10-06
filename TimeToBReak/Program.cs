using Microsoft.Win32;

namespace T2B
{
    internal static class Program
    {
        // استفاده از پیشوند Global برای اعمال محدودیت یک نسخه در کل سیستم (تمامی کاربران)
        private static Mutex? _globalMutex;

        [STAThread]
        static void Main()
        {
            const string mutexName = @"Global\T2B_BreakTime_SingleInstance_Mutex_Unique_Id";

            // بررسی اجرای تنها یک نسخه در کل سیستم
            _globalMutex = new Mutex(true, mutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                // یک نسخه از برنامه در حال اجراست؛ این نسخه بسته می‌شود.
                _globalMutex.Dispose();
                return;
            }

            try
            {
                ApplicationConfiguration.Initialize();

                // ثبت برنامه در رجیستری برای شروع خودکار در زمان بالا آمدن ویندوز (Startup)
                RegisterInStartup();

                Application.Run(new BreakApplicationContext());
            }
            finally
            {
                if (_globalMutex != null)
                {
                    _globalMutex.ReleaseMutex();
                    _globalMutex.Dispose();
                }
            }
        }

        /// <summary>
        /// ثبت برنامه در استارت‌آپ کاربر فعلی، بدون نیاز به دسترسی مدیر سیستم.
        /// </summary>
        private static void RegisterInStartup()
        {
            const string appName = "BehzadRest";
            string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            string? executablePath = Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(executablePath))
                executablePath = Application.ExecutablePath;

            try
            {
                // ثبت در HKCU برای جلوگیری از نیاز به elevation و اجرای برنامه در نشست کاربر.
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(runKeyPath, writable: true))
                {
                    string startupCommand = $"\"{executablePath}\"";
                    string? currentValue = key.GetValue(appName) as string;

                    if (!string.Equals(currentValue, startupCommand, StringComparison.OrdinalIgnoreCase))
                        key.SetValue(appName, startupCommand, RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register startup entry: {ex.Message}");
            }
        }
    }
}
