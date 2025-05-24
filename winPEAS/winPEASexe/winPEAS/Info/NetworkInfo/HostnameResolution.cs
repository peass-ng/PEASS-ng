using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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
        private const int INTERNET_SEARCH_TIMEOUT = 15;
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Attempts to resolve the local hostname via the external lambda.
        /// Always returns a populated <see cref="HostnameResolutionInfo"/> object.
        /// </summary>
        public static HostnameResolutionInfo TryExternalCheck()
        {
            var info = new HostnameResolutionInfo();

            try
            {
                // 1. Determine hostname
                info.Hostname = Dns.GetHostName();
                if (string.IsNullOrEmpty(info.Hostname))
                    info.Hostname = Environment.MachineName;

                // 2. Prepare JSON body
                var payload = new StringContent(
                    JsonSerializer.Serialize(new { hostname = info.Hostname }),
                    Encoding.UTF8,
                    "application/json");

                // 3. Configure HttpClient (header added once)
                if (!httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "winpeas");
                httpClient.Timeout = TimeSpan.FromSeconds(INTERNET_SEARCH_TIMEOUT);

                // 4. Call external checker
                var resp = httpClient
                    .PostAsync("https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/", payload)
                    .GetAwaiter().GetResult();

                if (resp.IsSuccessStatusCode)
                {
                    info.ExternalCheckResult = resp.Content.ReadAsStringAsync()
                                                         .GetAwaiter().GetResult();
                }
                else
                {
                    info.Error = $"External check failed (HTTP {(int)resp.StatusCode})";
                }
            }
            catch (Exception ex)
            {
                info.Error = $"Error during hostname check: {ex.Message}";
            }

            return info;
        }
    }
}
