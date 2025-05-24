using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace winPEAS.Info.NetworkInfo
{
    // ───────────────────────────────────────────────────────────────
    //  POCO returned to the UI
    // ───────────────────────────────────────────────────────────────
    public class InternetConnectivityInfo
    {
        public bool   HttpAccess        { get; set; }
        public bool   HttpsAccess       { get; set; }
        public bool   LambdaAccess      { get; set; }
        public bool   DnsAccess         { get; set; }
        public bool   IcmpAccess        { get; set; }

        public string HttpError         { get; set; }
        public string HttpsError        { get; set; }
        public string LambdaError       { get; set; }
        public string DnsError          { get; set; }
        public string IcmpError         { get; set; }

        public string SuccessfulHttpIp  { get; set; }
        public string SuccessfulHttpsIp { get; set; }
        public string SuccessfulDnsIp   { get; set; }
        public string SuccessfulIcmpIp  { get; set; }
    }

    // ───────────────────────────────────────────────────────────────
    //  Connectivity tester
    // ───────────────────────────────────────────────────────────────
    public static class InternetConnectivity
    {
        private const int HTTP_TIMEOUT_MS = 5000;   // 5 s
        private const int ICMP_TIMEOUT_MS = 2000;   // 2 s

        // IPs that answer on 80 & 443
        private static readonly string[] WEB_TEST_IPS =
            { "93.184.216.34", "151.101.1.69" };   // example.com / Fastly

        // Public DNS resolvers for DNS + ICMP checks
        private static readonly string[] DNS_ICMP_IPS =
            { "1.1.1.1", "8.8.8.8" };

        private const string LAMBDA_URL =
            "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/";

        // Shared HttpClient (kept for HTTP & Lambda checks)
        private static readonly HttpClient http = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(HTTP_TIMEOUT_MS)
        };

        // ─── Helpers ───────────────────────────────────────────────
        private static bool TryHttpAccess(string ip, out string error)  =>
            TryWebRequest($"http://{ip}",  out error);

        // **NEW IMPLEMENTATION** – plain TCP connect on port 443
        private static bool TryHttpsAccess(string ip, out string error)
        {
            try
            {
                using var client = new TcpClient();

                // Start async connect and wait up to the timeout
                var connectTask = client.ConnectAsync(ip, 443);
                bool completed  = connectTask.Wait(HTTP_TIMEOUT_MS);

                if (!completed)
                {
                    error = "TCP connect timed out";
                    return false;
                }

                if (client.Connected)
                {
                    error = null;
                    return true;
                }

                error = "TCP connection failed";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryWebRequest(string url, out string error)
        {
            try
            {
                using var cts =
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(HTTP_TIMEOUT_MS));
                http.GetAsync(url, cts.Token).GetAwaiter().GetResult();

                error = null;          // any HTTP response == connectivity
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

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

                var resp = http.SendAsync(req, cts.Token).GetAwaiter().GetResult();

                error = resp.IsSuccessStatusCode ? null :
                        $"HTTP {(int)resp.StatusCode}";
                return error == null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryDnsAccess(string ip, out string error)
        {
            try
            {
                using var udp = new UdpClient();
                udp.Client.ReceiveTimeout = HTTP_TIMEOUT_MS;
                udp.Client.SendTimeout    = HTTP_TIMEOUT_MS;

                var server = new IPEndPoint(IPAddress.Parse(ip), 53);

                // minimal query for google.com A‑record
                byte[] q = {
                    0x00,0x01, 0x01,0x00, 0x00,0x01, 0x00,0x00, 0x00,0x00, 0x00,0x00,
                    0x06,0x67,0x6f,0x6f,0x67,0x6c,0x65, 0x03,0x63,0x6f,0x6d, 0x00,
                    0x00,0x01, 0x00,0x01
                };

                udp.Send(q, q.Length, server);

                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] resp = udp.Receive(ref remote);

                error = resp?.Length > 0 ? null : "No DNS response";
                return error == null;
            }
            catch (SocketException ex) { error = ex.Message; return false; }
            catch (Exception      ex) { error = ex.Message; return false; }
        }

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

        // ─── Main entry ───────────────────────────────────────────
        public static InternetConnectivityInfo CheckConnectivity()
        {
            var info = new InternetConnectivityInfo();

            // -------- HTTP / HTTPS --------------------------------
            foreach (var ip in WEB_TEST_IPS)
            {
                // HTTP
                if (!info.HttpAccess)
                {
                    string httpErr;
                    if (TryHttpAccess(ip, out httpErr))
                    {
                        info.HttpAccess       = true;
                        info.SuccessfulHttpIp = ip;
                    }
                    else
                    {
                        info.HttpError = httpErr;
                    }
                }

                // HTTPS (raw TCP 443)
                if (!info.HttpsAccess)
                {
                    string httpsErr;
                    if (TryHttpsAccess(ip, out httpsErr))
                    {
                        info.HttpsAccess       = true;
                        info.SuccessfulHttpsIp = ip;
                    }
                    else
                    {
                        info.HttpsError = httpsErr;
                    }
                }

                if (info.HttpAccess && info.HttpsAccess) break;
            }

            // -------- Lambda --------------------------------------
            info.LambdaAccess = TryLambdaAccess(out string lambdaErr);
            if (!info.LambdaAccess) info.LambdaError = lambdaErr;

            // -------- DNS / ICMP ----------------------------------
            foreach (var ip in DNS_ICMP_IPS)
            {
                // DNS
                if (!info.DnsAccess)
                {
                    string dnsErr;
                    if (TryDnsAccess(ip, out dnsErr))
                    {
                        info.DnsAccess       = true;
                        info.SuccessfulDnsIp = ip;
                    }
                    else
                    {
                        info.DnsError = dnsErr;
                    }
                }

                // ICMP
                if (!info.IcmpAccess)
                {
                    string pingErr;
                    if (TryIcmpAccess(ip, out pingErr))
                    {
                        info.IcmpAccess       = true;
                        info.SuccessfulIcmpIp = ip;
                    }
                    else
                    {
                        info.IcmpError = pingErr;
                    }
                }

                if (info.DnsAccess && info.IcmpAccess) break;
            }

            return info;
        }
    }
}
