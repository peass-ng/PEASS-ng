using System;
using System.Collections.Generic;
using System.Linq;
using winPEAS.Helpers;
using winPEAS.Info.EventsInfo.Logon;
using winPEAS.Info.EventsInfo.Power;
using winPEAS.Info.EventsInfo.PowerShell;
using winPEAS.Info.EventsInfo.ProcessCreation;

namespace winPEAS.Checks
{
    internal class EventsInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Interesting Events information");

            new List<Action>
            {
                PrintExplicitLogonEvents,
                PrintLogonEvents,
                PrintProcessCreationEvents,
                PrintPowerShellEvents,
                PowerOnEvents,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private static void PrintPowerShellEvents()
        {
            try
            {
                Beaprint.MainPrint("PowerShell events - script block logs (EID 4104) - searching for sensitive data.\n");
                var powerShellEventInfos = PowerShell.GetPowerShellEventInfos();

                foreach (var info in powerShellEventInfos)
                {
                    Beaprint.NoColorPrint($"   User Id         :        {info.UserId}\n" +
                                               $"   Event Id        :        {info.EventId}\n" +
                                               $"   Context         :        {info.Context}\n" +
                                               $"   Created At      :        {info.CreatedAt}\n" +
                                               $"   Command line    :        {info.Match}\n");

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintProcessCreationEvents()
        {
            try
            {
                Beaprint.MainPrint("Process creation events - searching logs (EID 4688) for sensitive data.\n");

                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                foreach (var eventInfo in ProcessCreation.GetProcessCreationEventInfos())
                {
                    Beaprint.BadPrint($"  Created (UTC)      :      {eventInfo.CreatedAtUtc}\n" +
                                      $"  Event Id           :      {eventInfo.EventId}\n" +
                                      $"  User               :      {eventInfo.User}\n" +
                                      $"  Command Line       :      {eventInfo.Match}\n");

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintLogonEvents()
        {
            try
            {
                var lastDays = 10;
                Beaprint.MainPrint($"Printing Account Logon Events (4624) for the last {lastDays} days.\n");

                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                var logonInfos = Logon.GetLogonInfos(lastDays);

                foreach (var info in logonInfos.LogonEventInfos)
                {
                    Beaprint.BadPrint($"  Subject User Name            :       {info.SubjectUserName}\n" +
                                      $"  Subject Domain Name          :       {info.SubjectDomainName}\n" +
                                      $"  Created (Utc)                :       {info.CreatedAtUtc}\n" +
                                      $"  IP Address                   :       {info.IpAddress}\n" +
                                      $"  Authentication Package       :       {info.AuthenticationPackage}\n" +
                                      $"  Lm Package                   :       {info.LmPackage}\n" +
                                      $"  Logon Type                   :       {info.LogonType}\n" +
                                      $"  Target User Name             :       {info.TargetUserName}\n" +
                                      $"  Target Domain Name           :       {info.TargetDomainName}\n" +
                                      $"  Target Outbound User Name    :       {info.TargetOutboundUserName}\n" +
                                      $"  Target Outbound Domain Name  :       {info.TargetOutboundDomainName}\n");

                    Beaprint.PrintLineSeparator();
                }

                if (logonInfos.NTLMv1LoggedUsersSet.Count > 0 || logonInfos.NTLMv2LoggedUsersSet.Count > 0)
                {
                    Beaprint.BadPrint("  NTLM relay might be possible - other users authenticate to this machine using NTLM!");
                }

                if (logonInfos.NTLMv1LoggedUsersSet.Count > 0)
                {
                    Beaprint.BadPrint("  Accounts authenticate to this machine using NTLM v1!");
                    Beaprint.BadPrint("  You can obtain these accounts' **NTLM** hashes by sniffing NTLM challenge/responses and then crack them!");
                    Beaprint.BadPrint("  NTLM v1 authentication is broken!\n");

                    PrintUsers(logonInfos.NTLMv1LoggedUsersSet);
                }

                if (logonInfos.NTLMv2LoggedUsersSet.Count > 0)
                {
                    Beaprint.BadPrint("\n  Accounts authenticate to this machine using NTLM v2!");
                    Beaprint.BadPrint("  You can obtain NetNTLMv2 for these accounts by sniffing NTLM challenge/responses.");
                    Beaprint.BadPrint("  You can then try and crack their passwords.\n");

                    PrintUsers(logonInfos.NTLMv2LoggedUsersSet);
                }

                if (logonInfos.KerberosLoggedUsersSet.Count > 0)
                {
                    Beaprint.BadPrint("\n  The following users have authenticated to this machine using Kerberos.\n");
                    PrintUsers(logonInfos.KerberosLoggedUsersSet);
                }

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintExplicitLogonEvents()
        {
            try
            {
                var lastDays = 30;

                Beaprint.MainPrint($"Printing Explicit Credential Events (4648) for last {lastDays} days - A process logged on using plaintext credentials\n");

                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                var explicitLogonInfos = Logon.GetExplicitLogonEventsInfos(lastDays);

                foreach (var logonInfo in explicitLogonInfos)
                {
                    Beaprint.BadPrint($"  Subject User       :         {logonInfo.SubjectUser}\n" +
                                      $"  Subject Domain     :         {logonInfo.SubjectDomain}\n" +
                                      $"  Created (UTC)      :         {logonInfo.CreatedAtUtc}\n" +
                                      $"  IP Address         :         {logonInfo.IpAddress}\n" +
                                      $"  Process            :         {logonInfo.Process}\n" +
                                      $"  Target User        :         {logonInfo.TargetUser}\n" +
                                      $"  Target Domain      :         {logonInfo.TargetDomain}\n");

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintUsers(HashSet<string> users)
        {
            if (users == null) return;

            var set = users.OrderBy(u => u).ToArray();

            foreach (var user in set)
            {
                Beaprint.BadPrint($"    {user}");
            }
        }

        private void PowerOnEvents()
        {
            try
            {
                var lastDays = 5;

                Beaprint.MainPrint($"Displaying Power off/on events for last {lastDays} days\n");

                var infos = Power.GetPowerEventInfos(lastDays);

                foreach (var info in infos)
                {
                    Beaprint.NoColorPrint($"  {info.DateUtc.ToLocalTime(),-23} :  {info.Description}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
