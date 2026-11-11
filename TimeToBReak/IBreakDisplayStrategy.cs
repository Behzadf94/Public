using System;
using System.Windows.Forms;

namespace T2B.Strategies
{
	public interface IBreakDisplayStrategy : IDisposable
	{
		Form CreateBreakForm(Screen screen, Func<TimeSpan> getRemaining, Func<string> getUserDisplayName);
	}
}
