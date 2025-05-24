using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

// ------------------------------------------------------------------
// Connectivity tester – fixed time‑outs + real HTTP/HTTPS endpoints
// ------------------------------------------------------------------
namespace winPEAS.Info.NetworkInfo
{
    public class InternetConnectivityInfo
    {
        public bool   HttpAccess          { get; set; }
        public bool   HttpsAccess         { get; set; }
        public bool   LambdaAccess        { get; set; }
        public bool   DnsAccess           { get; set; }
        public bool   IcmpAccess          { get; set; }

        public string HttpError           { get; set; }
        public string HttpsError          { get; set; }
        public string LambdaError         { get; set; }
        public string DnsError            { get; set; }
        public string IcmpError           { get; set; }

        public string SuccessfulHttpIp    { get; set; }
        public string SuccessfulHttpsIp   { get; set; }
        public string SuccessfulDnsIp     { get; set; }
        public string SuccessfulIcmpIp    { get; set; }
    }
    
    public static class InternetConnectivity
    {
        // 5 seconds expressed in *milliseconds* to avoid unit mistakes
        private const int HTTP_TIMEOUT_MS = 5000;
        private const int ICMP_TIMEOUT_MS = 2000;

        // IPs that really listen on 80/443 (example.com + Fastly CDN)
        private static readonly string[] WEB_TEST_IPS =
            { "93.184.216.34", "151.101.1.69" };

        // DNS & ICMP targets stay the same – public resolvers
        private static readonly string[] DNS_ICMP_IPS =
            { "1.1.1.1", "8.8.8.8" };

        private const string LAMBDA_URL =
            "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/";

        // Re‑use a single HttpClient to avoid socket exhaustion
        private static readonly HttpClient http = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(HTTP_TIMEOUT_MS)
        };

        // ----------------------------------------------------------
        //  HTTP  (port 80)
        // ----------------------------------------------------------
        private static bool TryHttpAccess(string ip, out string error) =>
            TryWebRequest($"http://{ip}", out error);

        // ----------------------------------------------------------
        //  HTTPS (port 443)
        // ----------------------------------------------------------
        private static bool TryHttpsAccess(string ip, out string error) =>
            TryWebRequest($"https://{ip}", out error);

        // Common HTTP/HTTPS helper
        private static bool TryWebRequest(string url, out string error)
        {
            try
            {
                using var cts =
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(HTTP_TIMEOUT_MS));

                var resp = http.GetAsync(url, cts.Token).GetAwaiter().GetResult();

                // Any response indicates that we reached the server
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ----------------------------------------------------------
        //  Lambda URL check (GET)
        // ----------------------------------------------------------
        private static bool TryLambdaAccess(out string error)
        {
            try
            {
                using var cts =
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(HTTP_TIMEOUT_MS));

                var req = new HttpRequestMessage(HttpMethod.Get, LAMBDA_URL);
                req.Headers.UserAgent.ParseAdd("winpeas");
                req.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = http.SendAsync(req, cts.Token)
                               .GetAwaiter().GetResult();

                error = resp.IsSuccessStatusCode
                        ? null
                        : $"HTTP {(int)resp.StatusCode}";
                return error == null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ----------------------------------------------------------
        //  DNS test – simple UDP query
        // ----------------------------------------------------------
        private static bool TryDnsAccess(string ip, out string error)
        {
            try
            {
                using var udp = new UdpClient();
                udp.Client.ReceiveTimeout = HTTP_TIMEOUT_MS;
                udp.Client.SendTimeout = HTTP_TIMEOUT_MS;

                var server = new IPEndPoint(IPAddress.Parse(ip), 53);

                // Minimal “A record for google.com” query
                byte[] query = new byte[]
                {
                    0x00,0x01, 0x01,0x00, 0x00,0x01, 0x00,0x00, 0x00,0x00, 0x00,0x00,
                    0x06,0x67,0x6f,0x6f,0x67,0x6c,0x65, 0x03,0x63,0x6f,0x6d, 0x00,
                    0x00,0x01, 0x00,0x01
                };

                udp.Send(query, query.Length, server);

                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = udp.Receive(ref remote);

                error = response?.Length > 0 ? null : "No DNS response";
                return error == null;
            }
            catch (SocketException ex) { error = ex.Message; return false; }
            catch (Exception ex)       { error = ex.Message; return false; }
        }

        // ----------------------------------------------------------
        //  ICMP (Ping)
        // ----------------------------------------------------------
        private static bool TryIcmpAccess(string ip, out string error)
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send(ip, ICMP_TIMEOUT_MS);

                error = reply?.Status == IPStatus.Success
                        ? null
                        : $"Ping failed: {reply?.Status}";
                return error == null;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        // ----------------------------------------------------------
        //  MAIN ENTRY
        // ----------------------------------------------------------
        public static InternetConnectivityInfo CheckConnectivity()
        {
            var info = new InternetConnectivityInfo();

            // --- HTTP / HTTPS -------------------------------------
            foreach (var ip in WEB_TEST_IPS)
            {
                if (!info.HttpAccess &&
                    TryHttpAccess(ip, out string eHttp))
                {
                    info.HttpAccess = true;
                    info.SuccessfulHttpIp = ip;
                }
                else if (!info.HttpAccess)
                {
                    info.HttpError = eHttp;
                }

                if (!info.HttpsAccess &&
                    TryHttpsAccess(ip, out string eHttps))
                {
                    info.HttpsAccess = true;
                    info.SuccessfulHttpsIp = ip;
                }
                else if (!info.HttpsAccess)
                {
                    info.HttpsError = eHttps;
                }

                if (info.HttpAccess && info.HttpsAccess) break;
            }

            // --- Lambda ------------------------------------------
            info.LambdaAccess = TryLambdaAccess(out string eLambda);
            if (!info.LambdaAccess) info.LambdaError = eLambda;

            // --- DNS / ICMP --------------------------------------
            foreach (var ip in DNS_ICMP_IPS)
            {
                if (!info.DnsAccess &&
                    TryDnsAccess(ip, out string eDns))
                {
                    info.DnsAccess = true;
                    info.SuccessfulDnsIp = ip;
                }
                else if (!info.DnsAccess)
                {
                    info.DnsError = eDns;
                }

                if (!info.IcmpAccess &&
                    TryIcmpAccess(ip, out string ePing))
                {
                    info.IcmpAccess = true;
                    info.SuccessfulIcmpIp = ip;
                }
                else if (!info.IcmpAccess)
                {
                    info.IcmpError = ePing;
                }

                if (info.DnsAccess && info.IcmpAccess) break;
            }

            return info;
        }
    }
}
