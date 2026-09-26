using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.NetworkInfo.InternetSettings
{
    class InternetSettings
    {
        private const string InternetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const string InternetSettingsPolicyKey = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings";

        private static readonly HashSet<string> ProxyValueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProxyEnable",
            "ProxyServer",
            "ProxyOverride",
            "AutoConfigURL",
            "AutoDetect",
            "ProxySettingsPerUser"
        };

        private static readonly HashSet<string> ProxyEnvironmentVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HTTP_PROXY",
            "HTTPS_PROXY",
            "ALL_PROXY",
            "FTP_PROXY",
            "NO_PROXY"
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct WinHttpProxyInfo
        {
            public uint AccessType;
            public IntPtr Proxy;
            public IntPtr ProxyBypass;
        }

        [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WinHttpGetDefaultProxyConfiguration(out WinHttpProxyInfo proxyInfo);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr memory);

        public static InternetSettingsInfo GetInternetSettingsInfo()
        {
            var result = new InternetSettingsInfo();

            // List user/system internet settings for zonemapkey (local, trusted, etc.) :
            // 1 = Intranet zone – sites on your local network.
            // 2 = Trusted Sites zone – sites that have been added to your trusted sites.
            // 3 = Internet zone – sites that are on the Internet.
            // 4 = Restricted Sites zone – sites that have been specifically added to your restricted sites.
            IDictionary<string, string> zoneMapKeys = new Dictionary<string, string>()
            {
                {"0", "My Computer" },
                {"1", "Local Intranet Zone"},
                {"2", "Trusted Sites Zone"},
                {"3", "Internet Zone"},
                {"4", "Restricted Sites Zone"}
            };

            // WinINet settings are kept in the existing general listing and are also
            // summarized with their full source in the proxy-specific listing.
            AddSettings("HKCU", InternetSettingsKey, result.GeneralSettings);
            AddSettings("HKLM", InternetSettingsKey, result.GeneralSettings);
            AddProxyRegistrySettings("HKCU", InternetSettingsKey, result.ProxySettings);
            AddProxyRegistrySettings("HKLM", InternetSettingsKey, result.ProxySettings);

            // Group Policy can choose machine-wide proxy settings or define the same
            // WinINet values independently of the preferences shown to the user.
            AddProxyRegistrySettings("HKCU", InternetSettingsPolicyKey, result.ProxySettings);
            AddProxyRegistrySettings("HKLM", InternetSettingsPolicyKey, result.ProxySettings);

            AddProxyEnvironmentSettings(result.ProxySettings);
            AddWinHttpProxySettings(result.ProxySettings);

            string zoneMapKey = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMapKey";
            AddSettings("HKCU", zoneMapKey, result.ZoneMaps, zoneMapKeys);
            AddSettings("HKLM", zoneMapKey, result.ZoneMaps, zoneMapKeys);

            // List Zones settings with automatic logons
            IDictionary<uint, string> zoneAuthSettings = new Dictionary<uint, string>()
            {
                {0x00000000, "Automatically logon with current username and password"},
                {0x00010000, "Prompt for user name and password"},
                {0x00020000, "Automatic logon only in the Intranet zone"},
                {0x00030000, "Anonymous logon"}
            };

            for (int i = 0; i <= 4; i++)
            {
                var keyPath = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\" + i;
                var isParsed = uint.TryParse(RegistryHelper.GetRegValue("HKLM", keyPath, "1A00"), out uint authSetting);

                if (isParsed && zoneAuthSettings.TryGetValue(authSetting, out string authSettingStr))
                {
                    var zone = zoneMapKeys[i.ToString()];

                    result.ZoneAuthSettings.Add(new InternetSettingsKey(
                        "HKLM",
                        keyPath,
                        "1A00",
                        authSetting.ToString(),
                        $"{zone} : {authSettingStr}"
                    ));
                }
            }

            return result;
        }

        private static void AddSettings(string hive, string keyPath, IList<InternetSettingsKey> settings, IDictionary<string, string> interpretations = null)
        {
            var registrySettings = RegistryHelper.GetRegValues(hive, keyPath) ?? new Dictionary<string, object>();
            foreach (var setting in registrySettings)
            {
                string value = setting.Value?.ToString() ?? string.Empty;
                string interpretation = null;
                interpretations?.TryGetValue(value, out interpretation);

                settings.Add(new InternetSettingsKey(hive, keyPath, setting.Key, value, interpretation));
            }
        }

        private static void AddProxyRegistrySettings(string hive, string keyPath, IList<InternetSettingsKey> settings)
        {
            var registrySettings = RegistryHelper.GetRegValues(hive, keyPath) ?? new Dictionary<string, object>();
            foreach (var setting in registrySettings)
            {
                if (!ProxyValueNames.Contains(setting.Key))
                {
                    continue;
                }

                string value = setting.Value?.ToString() ?? string.Empty;
                settings.Add(new InternetSettingsKey(
                    hive,
                    keyPath,
                    setting.Key,
                    value,
                    DescribeProxySetting(setting.Key, value)
                ));
            }
        }

        private static void AddProxyEnvironmentSettings(IList<InternetSettingsKey> settings)
        {
            foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
            {
                string name = variable.Key?.ToString() ?? string.Empty;
                if (!ProxyEnvironmentVariableNames.Contains(name))
                {
                    continue;
                }

                string value = variable.Value?.ToString() ?? string.Empty;
                settings.Add(new InternetSettingsKey(
                    "Process",
                    "Environment",
                    name,
                    value,
                    DescribeProxySetting(name, value)
                ));
            }

            AddProxyEnvironmentRegistrySettings("HKCU", "Environment", settings);
            AddProxyEnvironmentRegistrySettings(
                "HKLM",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
                settings
            );
        }

        private static void AddProxyEnvironmentRegistrySettings(string hive, string keyPath, IList<InternetSettingsKey> settings)
        {
            var environmentSettings = RegistryHelper.GetRegValues(hive, keyPath) ?? new Dictionary<string, object>();
            foreach (var setting in environmentSettings)
            {
                if (!ProxyEnvironmentVariableNames.Contains(setting.Key))
                {
                    continue;
                }

                string value = setting.Value?.ToString() ?? string.Empty;
                settings.Add(new InternetSettingsKey(
                    hive,
                    keyPath,
                    setting.Key,
                    value,
                    DescribeProxySetting(setting.Key, value)
                ));
            }
        }

        private static void AddWinHttpProxySettings(IList<InternetSettingsKey> settings)
        {
            if (!WinHttpGetDefaultProxyConfiguration(out WinHttpProxyInfo proxyInfo))
            {
                return;
            }

            try
            {
                string accessTypeDescription;
                switch (proxyInfo.AccessType)
                {
                    case 0:
                        accessTypeDescription = "System/default proxy selection";
                        break;
                    case 1:
                        accessTypeDescription = "Direct access (no WinHTTP proxy)";
                        break;
                    case 3:
                        accessTypeDescription = "Named WinHTTP proxy configured";
                        break;
                    case 4:
                        accessTypeDescription = "Automatic WinHTTP proxy selection";
                        break;
                    default:
                        accessTypeDescription = "Unknown WinHTTP access type";
                        break;
                }

                settings.Add(new InternetSettingsKey(
                    "WinHTTP",
                    "WinHttpGetDefaultProxyConfiguration",
                    "AccessType",
                    proxyInfo.AccessType.ToString(),
                    accessTypeDescription
                ));

                string proxy = Marshal.PtrToStringUni(proxyInfo.Proxy);
                if (!string.IsNullOrWhiteSpace(proxy))
                {
                    settings.Add(new InternetSettingsKey(
                        "WinHTTP",
                        "WinHttpGetDefaultProxyConfiguration",
                        "Proxy",
                        proxy,
                        DescribeProxyEndpoint(proxy)
                    ));
                }

                string proxyBypass = Marshal.PtrToStringUni(proxyInfo.ProxyBypass);
                if (!string.IsNullOrWhiteSpace(proxyBypass))
                {
                    settings.Add(new InternetSettingsKey(
                        "WinHTTP",
                        "WinHttpGetDefaultProxyConfiguration",
                        "ProxyBypass",
                        proxyBypass,
                        "Bypass list (not a proxy destination)"
                    ));
                }
            }
            finally
            {
                if (proxyInfo.Proxy != IntPtr.Zero)
                {
                    GlobalFree(proxyInfo.Proxy);
                }

                if (proxyInfo.ProxyBypass != IntPtr.Zero)
                {
                    GlobalFree(proxyInfo.ProxyBypass);
                }
            }
        }

        private static string DescribeProxySetting(string name, string value)
        {
            if (name.Equals("ProxyEnable", StringComparison.OrdinalIgnoreCase))
            {
                return value == "1" ? "Explicit WinINet proxy enabled" : "Explicit WinINet proxy disabled";
            }

            if (name.Equals("AutoDetect", StringComparison.OrdinalIgnoreCase))
            {
                return value == "1" ? "Automatic proxy detection enabled" : "Automatic proxy detection disabled";
            }

            if (name.Equals("ProxySettingsPerUser", StringComparison.OrdinalIgnoreCase))
            {
                return value == "0" ? "Machine-wide proxy policy" : "Per-user proxy policy";
            }

            if (name.Equals("ProxyOverride", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("NO_PROXY", StringComparison.OrdinalIgnoreCase))
            {
                return "Bypass list (not a proxy destination)";
            }

            return DescribeProxyEndpoint(value);
        }

        private static string DescribeProxyEndpoint(string value)
        {
            var descriptions = new List<string>();
            // WINHTTP_PROXY_INFO allows proxy entries to be separated by either
            // semicolons or whitespace. Handle both forms so one unparseable entry
            // cannot hide a remote destination later in the list.
            char[] proxySeparators = { ';', ' ', '\t', '\r', '\n' };
            foreach (string rawEntry in value.Split(proxySeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                string entry = rawEntry.Trim();
                int assignmentIndex = entry.IndexOf('=');
                if (assignmentIndex >= 0 && assignmentIndex < entry.Length - 1)
                {
                    entry = entry.Substring(assignmentIndex + 1).Trim();
                }

                if (entry.Equals("DIRECT", StringComparison.OrdinalIgnoreCase))
                {
                    AddUnique(descriptions, "direct/no proxy");
                    continue;
                }

                string host = GetHost(entry);
                if (string.IsNullOrWhiteSpace(host))
                {
                    AddUnique(descriptions, "destination could not be classified");
                    continue;
                }

                AddUnique(descriptions, DescribeHost(host));
            }

            return descriptions.Count == 0
                ? "destination could not be classified"
                : string.Join(", ", descriptions);
        }

        private static string GetHost(string endpoint)
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri absoluteUri) && !string.IsNullOrWhiteSpace(absoluteUri.Host))
            {
                return absoluteUri.Host;
            }

            return Uri.TryCreate("http://" + endpoint, UriKind.Absolute, out Uri proxyUri)
                ? proxyUri.Host
                : string.Empty;
        }

        private static string DescribeHost(string host)
        {
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                return "loopback/local destination";
            }

            if (IPAddress.TryParse(host, out IPAddress address))
            {
                if (IPAddress.IsLoopback(address))
                {
                    return "loopback/local destination";
                }

                return IsPrivateOrLocalAddress(address)
                    ? "remote destination - private/link-local IP"
                    : "remote destination - public IP (trust unknown)";
            }

            if (host.IndexOf('.') < 0 ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".localdomain", StringComparison.OrdinalIgnoreCase))
            {
                return "remote destination - internal-looking hostname (trust unknown)";
            }

            return "remote destination - fully qualified hostname (trust unknown)";
        }

        private static bool IsPrivateOrLocalAddress(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] bytes = address.GetAddressBytes();
                return bytes[0] == 10 ||
                       (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) ||
                       (bytes[0] == 169 && bytes[1] == 254) ||
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168);
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] bytes = address.GetAddressBytes();
                return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || (bytes[0] & 0xFE) == 0xFC;
            }

            return false;
        }

        private static void AddUnique(ICollection<string> values, string value)
        {
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }
    }
}
