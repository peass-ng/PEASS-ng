using System;
using System.Threading;

namespace winPEAS.TaskScheduler
{
	internal class CultureSwitcher : IDisposable
	{
		private readonly System.Globalization.CultureInfo cur, curUI;

		public void Dispose()
		{
			Thread.CurrentThread.CurrentCulture = cur;
			Thread.CurrentThread.CurrentUICulture = curUI;
		}
	}
}
