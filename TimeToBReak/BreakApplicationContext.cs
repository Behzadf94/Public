using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using T2B.Strategies;

namespace T2B
{
	public sealed class BreakApplicationContext : ApplicationContext
	{
		private NotifyIcon _trayIcon = null!;
		private readonly KeyboardHook _keyboardHook;
		private readonly ExplorerWatcher _explorerWatcher;
		private readonly List<Form> _activeBreakForms = new();
		private Settings _settings;
		private bool _isBreakActive;
		private DateTime _nextBreak;
		private DateTime _breakEnd;
		private System.Windows.Forms.Timer _timer;

		public BreakApplicationContext()
		{
			_settings = Settings.Load();

			InitializeTray();

			_keyboardHook = new KeyboardHook();
			_keyboardHook.F2Pressed += OnF2Pressed;
			_keyboardHook.ScreenSaverShortcutPressed += OnScreenSaverShortcutPressed;
			_keyboardHook.Install();

			_explorerWatcher = new ExplorerWatcher();
			_explorerWatcher.ExplorerRestarted += OnExplorerRestarted;
			_explorerWatcher.Start();

			_nextBreak = DateTime.Now.AddMinutes(_settings.BreakInterval);

			_timer = new System.Windows.Forms.Timer { Interval = 1000 };
			_timer.Tick += TimerTick;
			_timer.Start();
		}

		private void InitializeTray()
		{
			var menu = new ContextMenuStrip();

			var settingsItem = new ToolStripMenuItem("Settings");
			settingsItem.Click += (_, _) => OpenInlineSettings();

			var startBreakItem = new ToolStripMenuItem("Start Break (Ctrl+F9)");
			startBreakItem.Click += (_, _) => StartBreak();

			var endBreakItem = new ToolStripMenuItem("End Break (F2)");
			endBreakItem.Click += (_, _) => EndBreak();

			menu.Items.Add(settingsItem);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(startBreakItem);
			menu.Items.Add(endBreakItem);
			menu.Items.Add(new ToolStripSeparator());

			var exitItem = new ToolStripMenuItem("Exit");
			exitItem.Click += ExitApplication;
			menu.Items.Add(exitItem);

			_trayIcon = new NotifyIcon
			{
				Icon = SystemIcons.Application,
				ContextMenuStrip = menu,
				Visible = true,
				Text = "BehzadRest"
			};
		}

		private void TimerTick(object? sender, EventArgs e)
		{
			if (_isBreakActive)
			{
				var remain = _breakEnd - DateTime.Now;
				if (remain <= TimeSpan.Zero)
				{
					EndBreak();
					return;
				}
				_trayIcon.Text = $"Break {remain:mm\\:ss}";
				return;
			}

			var intervalRemain = _nextBreak - DateTime.Now;
			if (intervalRemain <= TimeSpan.Zero)
			{
				StartBreak();
				return;
			}
			_trayIcon.Text = $"Next Break {intervalRemain:mm\\:ss}";
		}

		private void StartBreak()
		{
			if (_isBreakActive)
				return;

			_closeSettingsIfOpen();

			_settings = Settings.Load();
			_isBreakActive = true;
			_keyboardHook.IsBlockingEnabled = true;
			_breakEnd = DateTime.Now.AddMinutes(_settings.BreakDuration);

			CloseAllBreakForms();

			var availableVideos = _settings.GetAvailableVideos();
			var screens = Screen.AllScreens;

			for (int i = 0; i < screens.Length; i++)
			{
				var screen = screens[i];
				var strategy = BreakStrategyFactory.CreateStrategyForScreen(_settings, availableVideos, i);
				var form = strategy.CreateBreakForm(screen, GetRemainingTime, () => Environment.UserName);
				form.KeyPreview = true;
				form.KeyDown += BreakForm_KeyDown;
				_activeBreakForms.Add(form);
				form.Show();
			}
		}

		private void EndBreak()
		{
			if (!_isBreakActive)
				return;

			_isBreakActive = false;
			_keyboardHook.IsBlockingEnabled = false;
			CloseAllBreakForms();

			_settings = Settings.Load();
			_nextBreak = DateTime.Now.AddMinutes(_settings.BreakInterval);
		}

		private void OnF2Pressed() => EndBreak();

		private void OnScreenSaverShortcutPressed()
		{
			if (!_isBreakActive)
				StartBreak();
		}

		private void BreakForm_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F2 || (e.Control && e.KeyCode == Keys.D2))
				EndBreak();
		}

		private TimeSpan GetRemainingTime() => _breakEnd - DateTime.Now;

		private void CloseAllBreakForms()
		{
			foreach (var form in _activeBreakForms)
			{
				try { form.Close(); form.Dispose(); }
				catch { }
			}
			_activeBreakForms.Clear();
		}

		private void OpenInlineSettings()
		{
			// اگر در حالت استراحت هستیم، اجازهٔ تغییر مسیر ویدیو نده
			if (_isBreakActive)
			{
				MessageBox.Show("لطفاً ابتدا از حالت استراحت خارج شوید.",
					"BehzadRest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// یک فرم تنظیمات به‌صورت مودال بدون WebView2 فعال
			using var dlg = new SettingsForm(_settings)
			{
				StartPosition = FormStartPosition.CenterScreen,
				ShowInTaskbar = false
			};

			// owner = null مجاز است (برنامه tray است و پنجره‌ی اصلی ندارد)
			// ShowDialog() مودال، روی UI thread اصلی اجرا می‌شود
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				_settings = dlg.Result;
				_settings.Save();
				_nextBreak = DateTime.Now.AddMinutes(_settings.BreakInterval);
			}
		}

		private void _closeSettingsIfOpen()
		{
			// در صورت باز بودن فرم تنظیمات هنگام شروع استراحت، آن را ببند
			// نگهداری رفرنس اختیاری است؛ فعلاً بدون state.
		}

		private void OnExplorerRestarted()
		{
			try
			{
				_keyboardHook.Uninstall();
				_keyboardHook.Install();
			}
			catch { }
		}

		private void ExitApplication(object? sender, EventArgs e)
		{
			_trayIcon.Visible = false;
			_keyboardHook.F2Pressed -= OnF2Pressed;
			_keyboardHook.ScreenSaverShortcutPressed -= OnScreenSaverShortcutPressed;

			try { _keyboardHook.Uninstall(); } catch { }
			_explorerWatcher.Dispose();

			CloseAllBreakForms();
			Application.Exit();
		}
	}
}
