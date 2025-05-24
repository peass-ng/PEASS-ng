using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

namespace winPEAS.Info.NetworkInfo.NetworkScanner
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

        public static async Task<HostnameResolutionInfo> CheckResolution()
        {
            var result = new HostnameResolutionInfo();

            try
            {
                // Get the current hostname
                result.Hostname = Dns.GetHostName();

                // Environment.MachineName if hostname empty
                if (string.IsNullOrEmpty(result.Hostname))
                {
                    result.Hostname = Environment.MachineName;
                }

                // Prepare the request
                var content = new StringContent(
                    JsonSerializer.Serialize(new { hostname = result.Hostname }),
                    Encoding.UTF8,
                    "application/json"
                );
                httpClient.DefaultRequestHeaders.Add("User-Agent", "winpeas");
                httpClient.Timeout = TimeSpan.FromSeconds(INTERNET_SEARCH_TIMEOUT);

                // Make the request to the same endpoint as Linux version
                var response = await httpClient.PostAsync(
                    "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    result.ExternalCheckResult = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    result.ExternalCheckResult = $"External check failed with status code: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Error during hostname check: {ex.Message}";
            }

            return result;
        }
    }
} 