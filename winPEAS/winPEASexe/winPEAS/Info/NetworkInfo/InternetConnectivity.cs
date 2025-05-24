using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace winPEAS.Info.NetworkInfo
{
    public class InternetConnectivityInfo
    {
        public bool HttpAccess { get; set; }
        public bool HttpsAccess { get; set; }
        public bool LambdaAccess { get; set; }
        public bool DnsAccess { get; set; }
        public bool IcmpAccess { get; set; }
        public string HttpError { get; set; }
        public string HttpsError { get; set; }
        public string LambdaError { get; set; }
        public string DnsError { get; set; }
        public string IcmpError { get; set; }
        public string SuccessfulHttpIp { get; set; }
        public string SuccessfulHttpsIp { get; set; }
        public string SuccessfulDnsIp { get; set; }
        public string SuccessfulIcmpIp { get; set; }
    }

    public static class InternetConnectivity
    {
        private const int HTTP_TIMEOUT = 5000; // 5 seconds
        private const int ICMP_TIMEOUT = 2000; // 2 seconds
        private static readonly string[] TEST_IPS = new[] { "1.1.1.1", "8.8.8.8" }; // Cloudflare DNS, Google DNS
        private const string LAMBDA_URL = "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/";

        private static bool TryHttpAccess(string ip, out string error)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HTTP_TIMEOUT));
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT) };

                var resp = client.GetAsync($"http://{ip}", cts.Token)
                                 .GetAwaiter().GetResult();

                if (resp.IsSuccessStatusCode)
                {
                    error = null;
                    return true;
                }

                error = $"HTTP status {(int)resp.StatusCode}";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryHttpsAccess(string ip, out string error)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HTTP_TIMEOUT));
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT) };

                var resp = client.GetAsync($"https://{ip}", cts.Token)
                                 .GetAwaiter().GetResult();

                if (resp.IsSuccessStatusCode)
                {
                    error = null;
                    return true;
                }

                error = $"HTTPS status {(int)resp.StatusCode}";
                return false;
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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HTTP_TIMEOUT));
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT) };

                var req = new HttpRequestMessage(HttpMethod.Get, LAMBDA_URL);
                req.Headers.UserAgent.ParseAdd("winpeas");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = client.SendAsync(req, cts.Token)
                                 .GetAwaiter().GetResult();

                if (resp.IsSuccessStatusCode)
                {
                    error = null;
                    return true;
                }

                error = $"Lambda status {(int)resp.StatusCode}";
                return false;
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
                using (var udpClient = new UdpClient())
                {
                    // Set a timeout for the connection attempt
                    udpClient.Client.ReceiveTimeout = HTTP_TIMEOUT;
                    udpClient.Client.SendTimeout = HTTP_TIMEOUT;

                    // Create DNS server endpoint
                    var dnsServer = new IPEndPoint(IPAddress.Parse(ip), 53);

                    // Create a simple DNS query for google.com (type A record)
                    byte[] dnsQuery = new byte[] {
                        0x00, 0x01,             // Transaction ID
                        0x01, 0x00,             // Flags (Standard query)
                        0x00, 0x01,             // Questions: 1
                        0x00, 0x00,             // Answer RRs: 0
                        0x00, 0x00,             // Authority RRs: 0
                        0x00, 0x00,             // Additional RRs: 0
                        // google.com
                        0x06, 0x67, 0x6f, 0x6f, 0x67, 0x6c, 0x65, 0x03, 0x63, 0x6f, 0x6d, 0x00,
                        0x00, 0x01,             // Type: A
                        0x00, 0x01              // Class: IN
                    };

                    // Send the DNS query
                    udpClient.Send(dnsQuery, dnsQuery.Length, dnsServer);

                    // Try to receive a response
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] response = udpClient.Receive(ref remoteEP);

                    // If we got a response, the DNS server is reachable
                    if (response != null && response.Length > 0)
                    {
                        error = null;
                        return true;
                    }

                    error = "No response received from DNS server";
                    return false;
                }
            }
            catch (SocketException ex)
            {
                error = $"Socket error: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryIcmpAccess(string ip, out string error)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, ICMP_TIMEOUT);
                    if (reply?.Status == IPStatus.Success)
                    {
                        error = null;
                        return true;
                    }
                    error = $"Ping failed with status: {reply?.Status}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static InternetConnectivityInfo CheckConnectivity()
        {
            var result = new InternetConnectivityInfo();

            // Test HTTP (port 80) on each IP until success
            foreach (var ip in TEST_IPS)
            {
                if (TryHttpAccess(ip, out string error))
                {
                    result.HttpAccess = true;
                    result.SuccessfulHttpIp = ip;
                    break;
                }
                else if (ip == TEST_IPS[TEST_IPS.Length - 1]) // Last IP
                {
                    result.HttpAccess = false;
                    result.HttpError = error;
                }
            }

            // Test HTTPS (port 443) on each IP until success
            foreach (var ip in TEST_IPS)
            {
                if (TryHttpsAccess(ip, out string error))
                {
                    result.HttpsAccess = true;
                    result.SuccessfulHttpsIp = ip;
                    break;
                }
                else if (ip == TEST_IPS[TEST_IPS.Length - 1]) // Last IP
                {
                    result.HttpsAccess = false;
                    result.HttpsError = error;
                }
            }

            // Test Lambda URL
            result.LambdaAccess = TryLambdaAccess(out string lambdaError);
            if (!result.LambdaAccess)
            {
                result.LambdaError = lambdaError;
            }
            else
            {
                result.HttpsAccess = true;
            }

            // Test DNS on each IP until success
            foreach (var ip in TEST_IPS)
            {
                if (TryDnsAccess(ip, out string error))
                {
                    result.DnsAccess = true;
                    result.SuccessfulDnsIp = ip;
                    break;
                }
                else if (ip == TEST_IPS[TEST_IPS.Length - 1]) // Last IP
                {
                    result.DnsAccess = false;
                    result.DnsError = error;
                }
            }

            // Test ICMP (ping) on each IP until success
            foreach (var ip in TEST_IPS)
            {
                if (TryIcmpAccess(ip, out string error))
                {
                    result.IcmpAccess = true;
                    result.SuccessfulIcmpIp = ip;
                    break;
                }
                else if (ip == TEST_IPS[TEST_IPS.Length - 1]) // Last IP
                {
                    result.IcmpAccess = false;
                    result.IcmpError = error;
                }
            }

            return result;
        }
    }
} 