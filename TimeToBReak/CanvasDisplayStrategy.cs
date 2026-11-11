// مسیر فایل: Strategies/CanvasDisplayStrategy.cs
using System;
using System.Windows.Forms;

namespace T2B.Strategies
{
	public class CanvasDisplayStrategy : IBreakDisplayStrategy
	{
		public Form CreateBreakForm(Screen screen, Func<TimeSpan> getRemaining, Func<string> getUserDisplayName)
		{
			var form = new BreakLockForm(getRemaining, getUserDisplayName())
			{
				StartPosition = FormStartPosition.Manual,
				Bounds = screen.Bounds,
				TopMost = true,
				FormBorderStyle = FormBorderStyle.None,
				ShowInTaskbar = false
			};
			return form;
		}

		public void Dispose() { }
	}
}
