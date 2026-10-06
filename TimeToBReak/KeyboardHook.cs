using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace T2B
{
    public sealed class KeyboardHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc? _proc;

        // رویدادی برای اطلاع‌رسانی به برنامه جهت خروج از حالت استراحت
        public event Action? F2Pressed;
        public event Action? ScreenSaverShortcutPressed;

        public bool IsBlockingEnabled { get; set; }

        public void Install()
        {
            if (_hookId != IntPtr.Zero)
                return;

            _proc = HookCallback;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;

            _hookId = SetWindowsHookEx(
                13, // WH_KEYBOARD_LL
                _proc,
                GetModuleHandle(curModule!.ModuleName),
                0);
        }

        public void Uninstall()
        {
            if (_hookId == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // بررسی کلید F2 (در زمان فشردن کلید یعنی WM_KEYDOWN یا WM_SYSKEYDOWN)
                const int WM_KEYDOWN = 0x0100;
                const int WM_SYSKEYDOWN = 0x0104;
                int message = (int)wParam;

                bool ctrl = (GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;

                if (vkCode == (int)Keys.F9 && ctrl)
                {
                    if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                    {
                        ScreenSaverShortcutPressed?.Invoke();
                    }
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                if (vkCode == (int)Keys.F2)
                {
                    if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                    {
                        // شلیک رویداد جهت خروج از حالت استراحت در فرم یا کلاس اصلی
                        F2Pressed?.Invoke();
                    }
                    // اجازه عبور کلید F2 به بقیه برنامه‌ها و عدم بلاک آن
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                // در صورتی که بلاک فعال باشد، میانبرهای سیستمی بلاک می‌شوند
                if (IsBlockingEnabled)
                {
                    bool alt = (GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
                    bool shift = (GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;
                    bool win = (GetAsyncKeyState(Keys.LWin) & 0x8000) != 0
                               || (GetAsyncKeyState(Keys.RWin) & 0x8000) != 0;

                    // بلاک کردن کلیدهای میانبر سیستمی
                    bool ctrlBlock = (GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
                    if (alt && vkCode == (int)Keys.Tab) return (IntPtr)1;
                    if (win && vkCode == (int)Keys.Tab) return (IntPtr)1;
                    if (ctrlBlock && shift && vkCode == (int)Keys.Escape) return (IntPtr)1;
                    if (ctrlBlock && vkCode == (int)Keys.Tab) return (IntPtr)1;
                    if (win) return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Uninstall();
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);
    }
}
