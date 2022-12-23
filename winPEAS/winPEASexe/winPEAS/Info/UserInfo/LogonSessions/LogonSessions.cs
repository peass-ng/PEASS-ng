using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Kerberos;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.Info.UserInfo.LogonSessions
{
    internal class LogonSessions
    {
        public static IEnumerable<LogonSessionsInfo> GetLogonSessions()
        {
            if (!MyUtils.IsHighIntegrity())
            {
                // Logon Sessions (via WMI) 
                return GetLogonSessionsInfoWMI();
            }

            // Logon Sessions (via LSA)
            return GetLogonSessionsLSA();
        }

        private static IEnumerable<LogonSessionsInfo> GetLogonSessionsLSA()
        {
            var systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate

            var ret = Secur32.LsaEnumerateLogonSessions(out var count, out var luidPtr);  // get an array of pointers to LUIDs

            for (ulong i = 0; i < count; i++)
            {
                // TODO: Check return value
                ret = Secur32.LsaGetLogonSessionData(luidPtr, out var sessionData);
                var data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                // if we have a valid logon
                if (data.PSiD != IntPtr.Zero)
                {
                    // get the account username
                    var username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();

                    // convert the security identifier of the user
                    var sid = new System.Security.Principal.SecurityIdentifier(data.PSiD);

                    // domain for this account
                    var domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();

                    // authentication package
                    var authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();

                    // logon type
                    var logonType = (SECURITY_LOGON_TYPE)data.LogonType;

                    // datetime the session was logged in
                    var logonTime = systime.AddTicks((long)data.LoginTime);

                    // user's logon server
                    var logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();

                    // logon server's DNS domain
                    var dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();

                    // user principalname
                    var upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                    var logonID = "";
                    try { logonID = data.LoginID.LowPart.ToString(); }
                    catch { }

                    var userSID = "";
                    try { userSID = sid.Value; }
                    catch { }

                    yield return new LogonSessionsInfo(
                        "LSA",
                        username,
                        domain,
                        logonID,
                        logonType.ToString(),
                        authpackage,
                        null,
                        logonTime,
                        logonServer,
                        dnsDomainName,
                        upn,
                        userSID
                    );
                }

                // move the pointer forward
                luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                Secur32.LsaFreeReturnBuffer(sessionData);
            }
            Secur32.LsaFreeReturnBuffer(luidPtr);
        }

        private static IEnumerable<LogonSessionsInfo> GetLogonSessionsInfoWMI()
        {
            // https://www.pinvoke.net/default.aspx/secur32.lsalogonuser

            // list user logons combined with logon session data via WMI
            var userDomainRegex = new Regex(@"Domain=""(.*)"",Name=""(.*)""");
            var logonIdRegex = new Regex(@"LogonId=""(\d+)""");

            // Logon Sessions (via WMI) 
            var logonMap = new Dictionary<string, string[]>();

            // Win32_LoggedOnUser
            using (var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_LoggedOnUser"))
            {
                using (var data = wmiData.Get())
                {
                    foreach (ManagementObject result in data)
                    {
                        var m = logonIdRegex.Match(result["Dependent"].ToString());
                        if (!m.Success)
                        {
                            continue;
                        }

                        var logonId = m.Groups[1].ToString();
                        var m2 = userDomainRegex.Match(result["Antecedent"].ToString());
                        if (!m2.Success)
                        {
                            continue;
                        }

                        var domain = m2.Groups[1].ToString();
                        var user = m2.Groups[2].ToString();
                        logonMap.Add(logonId, new[] { domain, user });
                    }
                }
            }

            // Win32_LogonSession
            using (var wmiData2 = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_LogonSession"))
            {
                using (var data2 = wmiData2.Get())
                {
                    foreach (var o in data2)
                    {
                        var result2 = (ManagementObject)o;
                        var userDomain = new string[2] { "", "" };
                        try
                        {
                            userDomain = logonMap[result2["LogonId"].ToString()];
                        }
                        catch { }
                        var domain = userDomain[0];
                        var userName = userDomain[1];
                        var startTime = new DateTime();
                        var logonType = "";

                        try
                        {
                            startTime = ManagementDateTimeConverter.ToDateTime(result2["StartTime"].ToString());
                        }
                        catch { }

                        try
                        {
                            logonType = $"{((SECURITY_LOGON_TYPE)(int.Parse(result2["LogonType"].ToString())))}";
                        }
                        catch { }

                        yield return new LogonSessionsInfo(
                            "WMI",
                            userName,
                            domain,
                            result2["LogonId"].ToString(),
                            logonType,
                            result2["AuthenticationPackage"].ToString(),
                            startTime,
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                    }
                }
            }
        }
    }
}
