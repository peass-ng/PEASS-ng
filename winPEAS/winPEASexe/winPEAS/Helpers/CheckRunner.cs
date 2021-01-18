using System;
using System.Diagnostics;

namespace winPEAS.Helpers
{
    internal static class CheckRunner
    {
        public static void Run(Action action, bool isDebug, string description = null)
        {
            var timer = new Stopwatch();

            if (isDebug)
            {
                timer.Start();
            }
            
            action();

            if (isDebug)
            {
                timer.Stop();

                TimeSpan timeTaken = timer.Elapsed;
                string descriptionText = string.IsNullOrEmpty(description) ? string.Empty : $"[{description}] ";
                string log = $"{descriptionText}Execution took : {timeTaken.Minutes:00}m:{timeTaken.Seconds:00}s:{timeTaken.Milliseconds:000}";
                            
                Beaprint.PrintDebugLine(log);
            }
        }
    }
}
