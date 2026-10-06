using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using T2B.UI;

namespace T2B
{
    public sealed class LockScreenManager
    {
        private readonly List<Form> _forms = new();
        private readonly Func<TimeSpan> _getRemaining;
        private readonly Func<string> _getUserDisplayName;

        public LockScreenManager(Func<TimeSpan> getRemaining, Func<string> getUserDisplayName)
        {
            _getRemaining = getRemaining ?? throw new ArgumentNullException(nameof(getRemaining));
            _getUserDisplayName = getUserDisplayName ?? throw new ArgumentNullException(nameof(getUserDisplayName));
        }

        public void Show()
        {
            // بستن فرم‌های قبلی در صورت وجود برای پیشگیری از باز ماندن پنجره‌ها
            Hide();

            foreach (Screen screen in Screen.AllScreens)
            {
                // مانیتور اصلی را نادیده می‌گیریم چون فرم اصلی BreakLockForm آنجا باز می‌شود
                if (screen.Primary)
                    continue;

                Form f = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    Bounds = screen.Bounds,
                    TopMost = true,
                    BackColor = Color.Black,
                    ShowInTaskbar = false
                };

                var saver = new BreakScreenSaverControl(_getRemaining, _getUserDisplayName)
                {
                    Dock = DockStyle.Fill
                };

                f.Controls.Add(saver);
                _forms.Add(f);
                f.Show();
            }
        }

        public void Hide()
        {
            foreach (var f in _forms)
            {
                f.Close();
                f.Dispose();
            }

            _forms.Clear();
        }
    }
}
