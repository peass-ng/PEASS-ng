using System;
using System.Diagnostics;
using System.Threading;

namespace winPEAS.Helpers
{
    internal static class ObjectManagerHelper
    {
        public static bool TryCreateSessionEvent(out string objectName, out string error)
        {
            objectName = $"PEAS_OMNS_{Process.GetCurrentProcess().Id}_{Guid.NewGuid():N}";
            error = string.Empty;

            try
            {
                using (var handle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, objectName, out var createdNew))
                {
                    if (!createdNew)
                    {
                        error = "A test event with the generated name already existed.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
