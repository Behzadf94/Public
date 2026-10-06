using System;
using System.Windows.Forms;
using T2B.UI;

namespace T2B
{
    public partial class BreakLockForm : Form
    {
        private BreakScreenSaverControl? _screen;
        private Func<TimeSpan> _getRemaining;
        private string _userDisplayName;

        public BreakLockForm(Func<TimeSpan> getRemaining, string userDisplayName)
        {
            InitializeComponent();

            _getRemaining = getRemaining ?? throw new ArgumentNullException(nameof(getRemaining));
            _userDisplayName = string.IsNullOrWhiteSpace(userDisplayName) ? "Unknown" : userDisplayName;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;

            _screen = new BreakScreenSaverControl(
                getRemaining: _getRemaining,
                getUserDisplayName: () => _userDisplayName);

            _screen.Dock = DockStyle.Fill;
            Controls.Add(_screen);
        }

        // اگر در Designer فرم، رویداد Load به این متد وصل شده باشد،
        // نبودنش خطا می‌دهد. حتی اگر کاری نکند.
        private void BreakLockForm_Load(object sender, EventArgs e)
        {
            // intentionally empty
        }
    }

}
