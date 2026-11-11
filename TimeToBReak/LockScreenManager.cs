// مسیر فایل: LockScreenManager.cs
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using T2B.Strategies;

namespace T2B
{
	public sealed class LockScreenManager
	{
		private readonly List<Form> _screens = new();
		private readonly Func<TimeSpan> _getRemaining;
		private readonly Func<string> _getUserDisplayName;

		public LockScreenManager(Func<TimeSpan> getRemaining, Func<string> getUserDisplayName)
		{
			_getRemaining = getRemaining ?? throw new ArgumentNullException(nameof(getRemaining));
			_getUserDisplayName = getUserDisplayName ?? throw new ArgumentNullException(nameof(getUserDisplayName));
		}

		public void Show(IBreakDisplayStrategy strategy)
		{
			Hide();

			foreach (var screen in Screen.AllScreens)
			{
				var form = strategy.CreateBreakForm(screen, _getRemaining, _getUserDisplayName);
				_screens.Add(form);
				form.Show();
			}
		}

		public void Hide()
		{
			foreach (var screen in _screens)
			{
				try
				{
					screen.Close();
					screen.Dispose();
				}
				catch { }
			}
			_screens.Clear();
		}
	}
}
