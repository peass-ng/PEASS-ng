using System;
using System.Diagnostics;

namespace winPEAS.Helpers
{
    internal static class MeasureHelper
    {
        public static void MeasureMethod(Action action, string description = null)
        {
            var timer = new Stopwatch();
            timer.Start();
            action();
            timer.Stop();

            TimeSpan timeTaken = timer.Elapsed;
            string log = $"({description ?? string.Empty}) Time taken: " + timeTaken.ToString(@"m\:ss\.fff");
            Beaprint.LinkPrint(log);
        }
    }
}
