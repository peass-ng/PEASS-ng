using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Info.FilesInfo.Office.OneDrive;
using winPEAS.Native;

namespace winPEAS.Info.FilesInfo.Office
{
    internal class Office
    {
        public static IEnumerable<OfficeRecentFileInfo> GetOfficeRecentFileInfos(int limit)
        {
            var orderedRecentFiles = GetRecentOfficeFiles().OrderByDescending(e => e.LastAccessDate).Take(limit);

            foreach (var file in orderedRecentFiles)
            {
                yield return file;
            }
        }

        public static IEnumerable<CloudSyncProviderInfo> GetCloudSyncProviderInfos()
        {
            var keys = new List<string> { "DisplayName", "Business", "ServiceEndpointUri", "SPOResourceId", "UserEmail", "UserFolder", "UserName", "WebServiceUrl" };

            // Get all of the user SIDs (so will cover all users if run as an admin or has access to other user's reg keys)
            var SIDs = RegistryHelper.GetUserSIDs();
            var account = new Dictionary<string, string>();

            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes")) // Disregard anything that isn't a user
                    continue;

                var oneDriveSyncProviderInfo = new OneDriveSyncProviderInfo();

                // Now get each of the IDs (they aren't GUIDs but are an identity value for the specific library to sync)
                var subKeys = RegistryHelper.GetRegSubkeys("HKU", $"{sid}\\Software\\SyncEngines\\Providers\\OneDrive");
                if (subKeys == null)
                {
                    continue;
                }

                // Now go through each of them, get the metadata and stick it in the 'provider' dict. It'll get cross referenced later.
                foreach (string rname in subKeys)
                {
                    var provider = new Dictionary<string, string>();
                    foreach (string x in new List<string> { "LibraryType", "LastModifiedTime", "MountPoint", "UrlNamespace" })
                    {
                        var result = RegistryHelper.GetRegValue("HKU", $"{sid}\\Software\\SyncEngines\\Providers\\OneDrive\\{rname}", x);
                        if (!string.IsNullOrEmpty(result))
                        {
                            provider[x] = result;
                        }
                    }
                    oneDriveSyncProviderInfo.MpList[rname] = provider;
                }

                var odAccounts = RegistryHelper.GetRegSubkeys("HKU", $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts");
                if (odAccounts == null)
                {
                    continue;
                }

                foreach (string acc in odAccounts)
                {
                    var business = false;
                    foreach (string x in keys)
                    {
                        var result = RegistryHelper.GetRegValue("HKU", $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts\\{acc}", x);
                        if (!string.IsNullOrEmpty(result))
                        {
                            account[x] = result;
                        }

                        if (x == "Business")
                        {
                            business = (String.Compare(result, "1") == 0) ? true : false;
                        }
                    }
                    var odMountPoints = RegistryHelper.GetRegValues("HKU", $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts\\{acc}\\ScopeIdToMountPointPathCache");
                    var scopeIds = new List<string>();

                    if (business)
                    {
                        scopeIds.AddRange(odMountPoints.Select(mp => mp.Key));
                    }
                    else
                    {
                        scopeIds.Add(acc); // If its a personal account, OneDrive adds it as 'Personal' or the name of the account, not by the ScopeId itself. You can only have one personal account.
                    }

                    oneDriveSyncProviderInfo.AccountToMountpointDict[acc] = scopeIds;
                    oneDriveSyncProviderInfo.OneDriveList[acc] = account;
                    oneDriveSyncProviderInfo.UsedScopeIDs.AddRange(scopeIds);
                }

                yield return new CloudSyncProviderInfo(sid, oneDriveSyncProviderInfo);
            }
        }

        private static IEnumerable<OfficeRecentFileInfo> GetRecentOfficeFiles()
        {
            foreach (var sid in Registry.Users.GetSubKeyNames())
            {
                if (!sid.StartsWith("S-1") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                string userName = null;
                try
                {
                    userName = Advapi32.TranslateSid(sid);
                }
                catch
                {
                    userName = sid;
                }

                var officeVersion = RegistryHelper.GetRegSubkeys("HKU", $"{sid}\\Software\\Microsoft\\Office")
                                        ?.Where(k => float.TryParse(k, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB"), out _));

                if (officeVersion is null)
                {
                    continue;
                }

                foreach (var version in officeVersion)
                {
                    foreach (OfficeRecentFileInfo mru in GetMRUsFromVersionKey($"{sid}\\Software\\Microsoft\\Office\\{version}"))
                    {
                        //if (mru.LastAccessDate <= DateTime.Now.AddDays(-lastDays)) continue;

                        mru.User = userName;
                        yield return mru;
                    }
                }
            }
        }

        private static IEnumerable<OfficeRecentFileInfo> GetMRUsFromVersionKey(string officeVersionSubkeyPath)
        {
            var officeApplications = RegistryHelper.GetRegSubkeys("HKU", officeVersionSubkeyPath);
            if (officeApplications == null)
            {
                yield break;
            }

            foreach (var app in officeApplications)
            {
                // 1) HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\<OFFICE APP>\File MRU
                foreach (var mru in GetMRUsValues($"{officeVersionSubkeyPath}\\{app}\\File MRU"))
                {
                    yield return mru;
                }

                // 2) HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Word\User MRU\ADAL_B7C22499E768F03875FA6C268E771D1493149B23934326A96F6CDFEEEE7F68DA72\File MRU
                // or HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Word\User MRU\LiveId_CC4B824314B318B42E93BE93C46A61575D25608BBACDEEEA1D2919BCC2CF51FF\File MRU

                var logonAapps = RegistryHelper.GetRegSubkeys("HKU", $"{officeVersionSubkeyPath}\\{app}\\User MRU");
                if (logonAapps == null)
                {
                    continue;
                }

                foreach (var logonApp in logonAapps)
                {
                    foreach (var mru in GetMRUsValues($"{officeVersionSubkeyPath}\\{app}\\User MRU\\{logonApp}\\File MRU"))
                    {
                        ((OfficeRecentFileInfo)mru).Application = app;
                        yield return mru;
                    }
                }
            }
        }

        private static IEnumerable<OfficeRecentFileInfo> GetMRUsValues(string keyPath)
        {
            var values = RegistryHelper.GetRegValues("HKU", keyPath);
            if (values == null)
            {
                yield break;
            }

            foreach (var mruString in values.Values.Cast<string>().Select(ParseMruString).Where(mruString => mruString != null))
            {
                yield return mruString;
            }
        }

        private static OfficeRecentFileInfo ParseMruString(string mru)
        {
            var matches = Regex.Matches(mru, "\\[[a-zA-Z0-9]+?\\]\\[T([a-zA-Z0-9]+?)\\](\\[[a-zA-Z0-9]+?\\])?\\*(.+)");
            if (matches.Count == 0)
            {
                return null;
            }

            long timestamp = 0;
            var dateHexString = matches[0].Groups[1].Value;
            var filename = matches[0].Groups[matches[0].Groups.Count - 1].Value;

            try
            {
                timestamp = long.Parse(dateHexString, NumberStyles.HexNumber);
            }
            catch
            {
                Beaprint.PrintException($"Could not parse MRU timestamp. Parsed timestamp: {dateHexString} MRU value: {mru}");
            }

            return new OfficeRecentFileInfo
            {
                Application = "Office",
                User = null,
                Target = filename,
                LastAccessDate = DateTime.FromFileTimeUtc(timestamp),
            };
        }
    }
}
