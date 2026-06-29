using System;
namespace winPEAS.Info.NetworkInfo
{
    public class HostnameResolutionInfo
    {
        public string Hostname { get; set; }
        public string ExternalCheckResult { get; set; }
        public string Error { get; set; }
    }

    public static class HostnameResolution
    {
        /// <summary>
        /// Attempts to resolve the local hostname via the external lambda.
        /// Always returns a populated <see cref="HostnameResolutionInfo"/> object.
        /// </summary>
        public static HostnameResolutionInfo TryExternalCheck()
        {
            var info = new HostnameResolutionInfo();

            try
            {
                var result = HackTricksHostChecker.GetResult();
                info.Hostname = result.Hostname;
                info.ExternalCheckResult = result.HostOnlyResponse;
                info.Error = result.Error;
            }
            catch (Exception ex)
            {
                info.Error = $"Error during hostname check: {ex.Message}";
            }

            return info;
        }
    }
}
