using System;
using System.Threading;

namespace winPEAS.TaskScheduler
{
    internal class CultureSwitcher : IDisposable
    {
        private readonly System.Globalization.CultureInfo cur, curUI;

        public CultureSwitcher([NotNull] System.Globalization.CultureInfo culture)
        {
            cur = Thread.CurrentThread.CurrentCulture;
            curUI = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = cur;
            Thread.CurrentThread.CurrentUICulture = curUI;
        }
    }
}
