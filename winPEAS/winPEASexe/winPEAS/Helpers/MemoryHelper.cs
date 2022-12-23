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
                                         $"  Physical memory usage     : {MyUtils.ConvertBytesToHumanReadable(process.WorkingSet64)}\n" +
                                         $"  Paged system memory size  : {MyUtils.ConvertBytesToHumanReadable(process.PagedSystemMemorySize64)}\n" +
                                         $"  Paged memory size         : {MyUtils.ConvertBytesToHumanReadable(process.PagedMemorySize64)}\n";

                    Beaprint.PrintDebugLine(memoryStats);
                }
            }
        }


    }
}
