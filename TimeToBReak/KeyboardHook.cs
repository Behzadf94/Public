using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace T2B
{
	public sealed class KeyboardHook : IDisposable
	{
		private IntPtr _hookId = IntPtr.Zero;

		// نگه‌داشتن ارجاع پایدار به Delegate در سطح نمونه کلاس برای جلوگیری از پاک شدن توسط GC
		private LowLevelKeyboardProc? _proc;

		// رویدادهایی برای اطلاع‌رسانی به برنامه جهت خروج از حالت استراحت
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
				GetModuleHandle(curModule?.ModuleName),
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
				try
				{
					int vkCode = Marshal.ReadInt32(lParam);

					const int WM_KEYDOWN = 0x0100;
					const int WM_SYSKEYDOWN = 0x0104;
					int message = (int)wParam;

					// توجه: تمام Enum های Keys به int تبدیل شدند
					bool ctrl = (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;

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
							F2Pressed?.Invoke();
						}
						return CallNextHookEx(_hookId, nCode, wParam, lParam);
					}

					if (IsBlockingEnabled)
					{
						bool alt = (GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0;
						bool shift = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
						bool win = (GetAsyncKeyState((int)Keys.LWin) & 0x8000) != 0
								   || (GetAsyncKeyState((int)Keys.RWin) & 0x8000) != 0;

						bool ctrlBlock = (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;

						if (alt && vkCode == (int)Keys.Tab) return (IntPtr)1;
						if (win && vkCode == (int)Keys.Tab) return (IntPtr)1;
						if (ctrlBlock && shift && vkCode == (int)Keys.Escape) return (IntPtr)1;
						if (ctrlBlock && vkCode == (int)Keys.Tab) return (IntPtr)1;
						if (win) return (IntPtr)1;
					}
				}
				catch
				{
					// مهار خطاهای احتمالی جهت جلوگیری از قطع زنجیره هوک سیستم‌عامل
				}
			}

			return CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		public void Dispose()
		{
			Uninstall();
			_proc = null;
		}

		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(
			int idHook,
			LowLevelKeyboardProc lpfn,
			IntPtr hMod,
			uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(
			IntPtr hhk,
			int nCode,
			IntPtr wParam,
			IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string? lpModuleName);

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);
	}
}
