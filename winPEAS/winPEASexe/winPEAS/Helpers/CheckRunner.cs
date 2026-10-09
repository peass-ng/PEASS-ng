using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace winPEAS.Helpers
{
    internal static class CheckRunner
    {
        // A slow or broken WMI provider must not hold up the whole enumeration.
        // Callers should also set a provider timeout inside the bounded action.
        internal static bool TryRunBounded<T>(Func<T> action, TimeSpan timeout, out T value, out string error)
        {
            value = default(T);
            error = "";
            try
            {
                var task = Task.Run(action);
                if (!task.Wait(timeout))
                {
                    error = "query timed out";
                    return false;
                }
                value = task.Result;
                return true;
            }
            catch (AggregateException ex)
            {
                error = ex.GetBaseException().Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void Run(Action action, bool isDebug, string description = null)
        {
            if (!isDebug)
            {
                action();
            }
            else
            {
                var timer = new Stopwatch();

                timer.Start();
                action();
                timer.Stop();

                TimeSpan timeTaken = timer.Elapsed;
                string descriptionText = string.IsNullOrEmpty(description) ? string.Empty : $"[{description}] ";
                string log = $"{descriptionText}Execution took : {timeTaken.Minutes:00}m:{timeTaken.Seconds:00}s:{timeTaken.Milliseconds:000}";

                Beaprint.PrintDebugLine(log);
            }
        }
    }
}
