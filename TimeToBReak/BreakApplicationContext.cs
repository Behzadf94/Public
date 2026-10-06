using System;
using System.Drawing;
using System.Windows.Forms;

namespace T2B
{
    public sealed class BreakApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon = null!;

        private readonly KeyboardHook _keyboardHook;
        private readonly ExplorerWatcher _explorerWatcher;
        private readonly LockScreenManager _lockScreenManager; // اضافه شد

        private BreakLockForm? _breakForm;

        private bool _isBreakActive;

        private DateTime _nextBreak;
        private DateTime _breakEnd;
        private System.Windows.Forms.Timer _timer;

        private Form? _settingsForm;

        public BreakApplicationContext()
        {
            InitializeTray();

            _keyboardHook = new KeyboardHook();
            _keyboardHook.F2Pressed += OnF2Pressed;
            _keyboardHook.ScreenSaverShortcutPressed += OnScreenSaverShortcutPressed;
            _keyboardHook.Install();

            _lockScreenManager = new LockScreenManager(GetRemainingTime, () => Environment.UserName); // نمونه‌سازی مدیریت مانیتورها

            _explorerWatcher = new ExplorerWatcher();
            _explorerWatcher.ExplorerRestarted += OnExplorerRestarted;
            _explorerWatcher.Start();

            var s = Settings.Load();
            _nextBreak = DateTime.Now.AddMinutes(s.BreakInterval);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000;
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private void InitializeTray()
        {
            var menu = new ContextMenuStrip();

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += OpenInlineSettings;

            var startScreenSaverItem = new ToolStripMenuItem("Start Screen Saver (Ctrl+F9)");
            startScreenSaverItem.Click += (_, _) => StartBreak();

            var startBreakItem = new ToolStripMenuItem("Start Break");
            startBreakItem.Click += (_, _) => StartBreak();

            var endBreakItem = new ToolStripMenuItem("End Break");
            endBreakItem.Click += (_, _) => EndBreak();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitApplication;

            menu.Items.Add(settingsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(startBreakItem);
            menu.Items.Add(endBreakItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon
            {
                Icon = new Icon("T2B.ico"),
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

            var s = Settings.Load();

            _isBreakActive = true;

            _keyboardHook.IsBlockingEnabled = true;

            _breakEnd = DateTime.Now.AddMinutes(s.BreakDuration);

            // ۱. نمایش صفحه قفل مشکی روی سایر مانیتورها
            _lockScreenManager.Show();

            // ۲. ساخت فرم اصلی متحرک روی مانیتور اصلی
            _breakForm = new BreakLockForm(
                GetRemainingTime,
                Environment.UserName);

            // موقعیت فرم متحرک دقیقاً روی مانیتور اصلی فیکس می‌شود
            _breakForm.StartPosition = FormStartPosition.Manual;
            _breakForm.Bounds = Screen.PrimaryScreen.Bounds;

            _breakForm.KeyPreview = true;
            _breakForm.KeyDown += BreakForm_KeyDown;

            _breakForm.Show();
        }

        private void OnF2Pressed()
        {
            if (_breakForm != null && _breakForm.InvokeRequired)
            {
                _breakForm.BeginInvoke(new Action(EndBreak));
            }
            else
            {
                EndBreak();
            }
        }

        private void OnScreenSaverShortcutPressed()
        {
            if (_isBreakActive)
                return;

            StartBreak();
        }

        private void BreakForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 || (e.Control && e.KeyCode == Keys.D2))
            {
                EndBreak();
            }
        }

        private TimeSpan GetRemainingTime()
        {
            return _breakEnd - DateTime.Now;
        }

        private void EndBreak()
        {
            if (!_isBreakActive)
                return;

            _isBreakActive = false;

            _keyboardHook.IsBlockingEnabled = false;

            // بستن تمام صفحات مشکی روی سایر مانیتورها
            _lockScreenManager.Hide();

            // بستن فرم متحرک اصلی
            if (_breakForm != null)
            {
                _breakForm.Close();
                _breakForm = null;
            }

            var s = Settings.Load();
            _nextBreak = DateTime.Now.AddMinutes(s.BreakInterval);
        }

        private void OpenInlineSettings(object? sender, EventArgs e)
        {
            if (_settingsForm != null)
            {
                _settingsForm.Activate();
                return;
            }

            var s = Settings.Load();

            var f = new Form();
            f.FormBorderStyle = FormBorderStyle.None;
            f.StartPosition = FormStartPosition.Manual;
            f.Size = new Size(200, 140);
            f.BackColor = Color.FromArgb(35, 35, 35);
            f.ForeColor = Color.White;
            f.TopMost = true;
            f.ShowInTaskbar = false;

            f.Location = new Point(Cursor.Position.X - 180, Cursor.Position.Y - 130);

            var lblInterval = new Label { Text = "Interval", Location = new Point(10, 10), AutoSize = true };

            var spinInterval = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 240,
                Value = s.BreakInterval,
                Location = new Point(10, 30),
                Width = 60
            };

            var lblBreak = new Label { Text = "Break", Location = new Point(10, 60), AutoSize = true };

            var spinBreak = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Value = s.BreakDuration,
                Location = new Point(10, 80),
                Width = 60
            };

            var btn = new Button
            {
                Text = "OK",
                Location = new Point(120, 90),
                Width = 60
            };

            btn.Click += (_, __) =>
            {
                s.BreakInterval = (int)spinInterval.Value;
                s.BreakDuration = (int)spinBreak.Value;

                s.Save();

                _nextBreak = DateTime.Now.AddMinutes(s.BreakInterval);

                f.Close();
            };

            f.Controls.Add(lblInterval);
            f.Controls.Add(spinInterval);
            f.Controls.Add(lblBreak);
            f.Controls.Add(spinBreak);
            f.Controls.Add(btn);

            f.Deactivate += (_, _) => f.Close();
            f.FormClosed += (_, _) => _settingsForm = null;

            _settingsForm = f;

            f.Show();
        }

        private void OnExplorerRestarted()
        {
            _keyboardHook.Uninstall();
            _keyboardHook.Install();
        }

        private void ExitApplication(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            _keyboardHook.F2Pressed -= OnF2Pressed;
            _keyboardHook.ScreenSaverShortcutPressed -= OnScreenSaverShortcutPressed;
            _keyboardHook.Uninstall();
            _explorerWatcher.Dispose();

            // بستن صفحات قفل
            _lockScreenManager.Hide();

            if (_breakForm != null)
            {
                _breakForm.Close();
                _breakForm = null;
            }

            Application.Exit();
        }
    }
}
