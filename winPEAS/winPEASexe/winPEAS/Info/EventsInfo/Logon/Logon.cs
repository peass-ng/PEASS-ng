using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Native.Enums;

namespace winPEAS.Info.EventsInfo.Logon
{
    internal class Logon
    {
        public static LogonInfo GetLogonInfos(int lastDays)
        {
            var result = new LogonInfo();
            var logonEventInfos = new List<LogonEventInfo>();
            var NTLMv1LoggedUsersSet = new HashSet<string>();
            var NTLMv2LoggedUsersSet = new HashSet<string>();
            var kerberosLoggedUsersSet = new HashSet<string>();

            string userRegex = null;

            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            var query = $@"*[System/EventID=4624] and *[System[TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime.ToUniversalTime():o}']]]";
            var logReader = MyUtils.GetEventLogReader("Security", query);

            // read the event log
            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                //var subjectUserSid = eventDetail.GetPropertyValue(0);
                var subjectUserName = eventDetail.GetPropertyValue(1);
                var subjectDomainName = eventDetail.GetPropertyValue(2);
                //var subjectLogonId = eventDetail.GetPropertyValue(3);
                //var targetUserSid = eventDetail.GetPropertyValue(4);
                var targetUserName = eventDetail.GetPropertyValue(5);
                var targetDomainName = eventDetail.GetPropertyValue(6);
                //var targetLogonId = eventDetail.GetPropertyValue(7);
                //var logonType = eventDetail.GetPropertyValue(8);
                var logonType = $"{(SECURITY_LOGON_TYPE)(int.Parse(eventDetail.GetPropertyValue(8)))}";
                //var logonProcessName = eventDetail.GetPropertyValue(9);
                var authenticationPackageName = eventDetail.GetPropertyValue(10);
                //var workstationName = eventDetail.GetPropertyValue(11);
                //var logonGuid = eventDetail.GetPropertyValue(12);
                //var transmittedServices = eventDetail.GetPropertyValue(13);
                var lmPackageName = eventDetail.GetPropertyValue(14);
                lmPackageName = lmPackageName == "-" ? "" : lmPackageName;
                //var keyLength = eventDetail.GetPropertyValue(15);
                //var processId = eventDetail.GetPropertyValue(16);
                //var processName = eventDetail.GetPropertyValue(17);
                var ipAddress = eventDetail.GetPropertyValue(18);
                //var ipPort = eventDetail.GetPropertyValue(19);
                //var impersonationLevel = eventDetail.GetPropertyValue(20);
                //var restrictedAdminMode = eventDetail.GetPropertyValue(21);

                var targetOutboundUserName = "-";
                var targetOutboundDomainName = "-";
                if (eventDetail.Properties.Count > 22)  // Not available on older versions of Windows
                {
                    targetOutboundUserName = eventDetail.GetPropertyValue(22);
                    targetOutboundDomainName = eventDetail.GetPropertyValue(23);
                }
                //var VirtualAccount = eventDetail.GetPropertyValue(24);
                //var TargetLinkedLogonId = eventDetail.GetPropertyValue(25);
                //var ElevatedToken = eventDetail.GetPropertyValue(26);

                // filter out SYSTEM, computer accounts, local service accounts, UMFD-X accounts, and DWM-X accounts (for now)
                var userIgnoreRegex = "^(SYSTEM|LOCAL SERVICE|NETWORK SERVICE|UMFD-[0-9]+|DWM-[0-9]+|ANONYMOUS LOGON|" + Environment.MachineName + "\\$)$";
                if (userRegex == null && Regex.IsMatch(targetUserName, userIgnoreRegex, RegexOptions.IgnoreCase))
                {
                    continue;
                }

                var domainIgnoreRegex = "^(NT VIRTUAL MACHINE)$";
                if (userRegex == null && Regex.IsMatch(targetDomainName, domainIgnoreRegex, RegexOptions.IgnoreCase))
                {
                    continue;
                }

                // Handle the user filter
                if (userRegex != null && !Regex.IsMatch(targetUserName, userRegex, RegexOptions.IgnoreCase))
                {
                    continue;
                }

                // Analyze the output
                if (logonType == "Network")
                {
                    var accountName = $"{targetDomainName}\\{targetUserName}";
                    if (authenticationPackageName == "NTLM")
                    {
                        switch (lmPackageName)
                        {
                            case "NTLM V1":
                                NTLMv1LoggedUsersSet.Add(accountName);
                                break;
                            case "NTLM V2":
                                NTLMv2LoggedUsersSet.Add(accountName);
                                break;
                        }
                    }
                    else if (authenticationPackageName == "Kerberos")
                    {
                        kerberosLoggedUsersSet.Add(accountName);
                    }
                }

                logonEventInfos.Add(
                    new LogonEventInfo(
                        eventDetail.TimeCreated?.ToUniversalTime(),
                        targetUserName,
                        targetDomainName,
                        logonType,
                        ipAddress,
                        subjectUserName,
                        subjectDomainName,
                        authenticationPackageName,
                        lmPackageName,
                        targetOutboundUserName,
                        targetOutboundDomainName
                    )
               );
            }

            result.KerberosLoggedUsersSet = kerberosLoggedUsersSet;
            result.NTLMv1LoggedUsersSet = NTLMv1LoggedUsersSet;
            result.NTLMv2LoggedUsersSet = NTLMv2LoggedUsersSet;
            result.LogonEventInfos = logonEventInfos;

            return result;
        }

        public static IEnumerable<ExplicitLogonEventInfo> GetExplicitLogonEventsInfos(int lastDays)
        {
            const string eventId = "4648";
            string userFilterRegex = null;

            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            var query = $@"*[System/EventID={eventId}] and *[System[TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime.ToUniversalTime():o}']]]";

            var logReader = MyUtils.GetEventLogReader("Security", query);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                //string subjectUserSid = eventDetail.GetPropertyValue(0);
                var subjectUserName = eventDetail.GetPropertyValue(1);
                var subjectDomainName = eventDetail.GetPropertyValue(2);
                //var subjectLogonId = eventDetail.GetPropertyValue(3);
                //var logonGuid = eventDetail.GetPropertyValue(4);
                var targetUserName = eventDetail.GetPropertyValue(5);
                var targetDomainName = eventDetail.GetPropertyValue(6);
                //var targetLogonGuid = eventDetail.GetPropertyValue(7);
                //var targetServerName = eventDetail.GetPropertyValue(8);
                //var targetInfo = eventDetail.GetPropertyValue(9);
                //var processId = eventDetail.GetPropertyValue(10);
                var processName = eventDetail.GetPropertyValue(11);
                var ipAddress = eventDetail.GetPropertyValue(12);
                //var IpPort = eventDetail.GetPropertyValue(13);

                // Ignore the current machine logging on and 
                if (Regex.IsMatch(targetUserName, Environment.MachineName) ||
                    Regex.IsMatch(targetDomainName, @"^(Font Driver Host|Window Manager)$"))
                {
                    continue;
                }

                if (userFilterRegex != null && !Regex.IsMatch(targetUserName, userFilterRegex))
                {
                    continue;
                }

                yield return new ExplicitLogonEventInfo
                {
                    CreatedAtUtc = eventDetail.TimeCreated?.ToUniversalTime(),
                    SubjectUser = subjectUserName,
                    SubjectDomain = subjectDomainName,
                    TargetUser = targetUserName,
                    TargetDomain = targetDomainName,
                    Process = processName,
                    IpAddress = ipAddress
                };
            }
        }
    }

    internal static class EventRecordExtensions
    {
        internal static string GetPropertyValue(this EventRecord record, int index)
        {
            return record == null ? string.Empty : record.Properties[index].Value.ToString();
        }
    }
}
