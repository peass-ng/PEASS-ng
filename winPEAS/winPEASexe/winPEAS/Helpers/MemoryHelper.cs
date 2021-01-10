using System;
using System.Diagnostics;

namespace winPEAS.Helpers
{
    internal static class MemoryHelper
    {
        public static void DisplayMemoryStats()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                if (!process.HasExited)
                {
                    process.Refresh();

                    string memoryStats = $"{process.ProcessName} - Memory Stats\n" +
                                         $"-------------------------------------\n" +
                                         $"  Physical memory usage     : {ToSize(process.WorkingSet64)}\n" +
                                         $"  Paged system memory size  : {ToSize(process.PagedSystemMemorySize64)}\n" +
                                         $"  Paged memory size         : {ToSize(process.PagedMemorySize64)}\n";

                    Beaprint.PrintDebugLine(memoryStats);
                }
            }
        }

        private static string[] suffixes = new[] { " B", " KB", " MB", " GB", " TB", " PB" };

        private static string ToSize(double number, int precision = 2)
        {
            // unit's number of bytes
            const double unit = 1024;
            // suffix counter
            int i = 0;
            // as long as we're bigger than a unit, keep going
            while (number > unit)
            {
                number /= unit;
                i++;
            }
            // apply precision and current suffix
            return Math.Round(number, precision) + suffixes[i];
        }
    }
}
