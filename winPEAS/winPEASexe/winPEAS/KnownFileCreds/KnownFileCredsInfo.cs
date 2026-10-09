using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.KnownFileCreds
{
    static class KnownFileCredsInfo
    {
        public static Dictionary<string, object> GetRecentRunCommands()
        {
            Dictionary<string, object> results = new Dictionary<string, object>();
            // lists recently run commands via the RunMRU registry key
            if (MyUtils.IsHighIntegrity())
            {
                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                    {
                        results = RegistryHelper.GetRegValues("HKU", string.Format("{0}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU", SID));
                    }
                }
            }
            else
            {
                results = RegistryHelper.GetRegValues("HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU");
            }
            return results;
        }

        public static List<Dictionary<string, string>> ListCloudCreds()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // checks for various cloud credential files (AWS, Microsoft Azure, and Google Compute)
            // adapted from https://twitter.com/cmaddalena's SharpCloud project (https://github.com/chrismaddalena/SharpCloud/)
            try
            {
                var cloudCredsDict = new Dictionary<string, string>
                {
                    // AWS
                    { ".aws\\credentials", "AWS keys file" },

                    // Google Cloud
                    { "AppData\\Roaming\\gcloud\\credentials.db", "GC Compute creds" },
                    { "AppData\\Roaming\\gcloud\\legacy_credentials", "GC Compute creds legacy" },
                    { "AppData\\Roaming\\gcloud\\access_tokens.db", "GC Compute tokens" },

                    // Azure
                    { ".azure\\accessTokens.json", "Azure tokens" },
                    { ".azure\\azureProfile.json", "Azure profile" },
                    { ".azure\\TokenCache.dat", "Azure Token Cache" },
                    { ".azure\\AzureRMContext.json", "Azure RM Context" },
                    { "AppData\\Roaming\\Windows Azure Powershell\\TokenCache.dat", "Azure PowerShell Token Cache" },
                    { "AppData\\Roaming\\Windows Azure Powershell\\AzureRMContext.json", "Azure PowerShell RM Context" },

                    // Bluemix
                    { ".bluemix\\config.json", "Bluemix Config" },
                    { ".bluemix\\.cf\\config.json", "Bluemix Alternate Config" },
                };

                IEnumerable<string> userDirs;

                if (MyUtils.IsHighIntegrity())
                {
                    string userFolders = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                    userDirs = Directory.EnumerateDirectories(userFolders);
                }
                else
                {
                    var currentUserDir = Environment.GetEnvironmentVariable("USERPROFILE");
                    userDirs = new List<string> { currentUserDir };
                }

                foreach (var userDir in userDirs)
                {
                    foreach (var item in cloudCredsDict)
                    {
                        var file = item.Key;
                        var description = item.Value;

                        var fullFilePath = Path.Combine(userDir, file);

                        AddCloudCredentialFromFile(fullFilePath, description, results);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        private static void AddCloudCredentialFromFile(string filePath, string description, ICollection<Dictionary<string, string>> results)
        {
            if (File.Exists(filePath))
            {
                DateTime lastAccessed = File.GetLastAccessTime(filePath);
                DateTime lastModified = File.GetLastWriteTime(filePath);
                long size = new FileInfo(filePath).Length;

                results?.Add(new Dictionary<string, string>
                {
                    { "file", filePath },
                    { "Description", description },
                    { "Accessed", $"{lastAccessed}"},
                    { "Modified", $"{lastModified}"},
                    { "Size", $"{size}"}
                });
            }
        }

        public static List<Dictionary<string, string>> GetRecentFiles()
        {
            // parses recent file shortcuts via COM
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            int lastDays = 7;
            DateTime startTime = DateTime.Now.AddDays(-lastDays);

            try
            {
                // WshShell COM object GUID 
                Type shell = Type.GetTypeFromCLSID(new Guid("F935DC22-1CF0-11d0-ADB9-00C04FD58A0B"));
                Object shellObj = Activator.CreateInstance(shell);

                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = string.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    var dirs = Directory.EnumerateDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];

                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string recentPath = string.Format("{0}\\AppData\\Roaming\\Microsoft\\Windows\\Recent\\", dir);
                            try
                            {
                                if (Directory.Exists(recentPath))
                                {
                                    string[] recentFiles = Directory.EnumerateFiles(recentPath, "*.lnk", SearchOption.AllDirectories).ToArray();

                                    if (recentFiles.Length != 0)
                                    {
                                        Console.WriteLine("   {0} :\r\n", userName);
                                        foreach (string recentFile in recentFiles)
                                        {
                                            DateTime lastAccessed = File.GetLastAccessTime(recentFile);

                                            if (lastAccessed > startTime)
                                            {
                                                // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                                                Object shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shellObj, new object[] { recentFile });
                                                Object TargetPath = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, new object[] { });

                                                if (TargetPath.ToString().Trim() != "")
                                                {
                                                    results.Add(new Dictionary<string, string>()
                                                {
                                                    { "Target", TargetPath.ToString() },
                                                    { "Accessed", string.Format("{0}", lastAccessed) }
                                                });
                                                }
                                                Marshal.ReleaseComObject(shortcut);
                                                shortcut = null;
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    string recentPath = string.Format("{0}\\Microsoft\\Windows\\Recent\\", Environment.GetEnvironmentVariable("APPDATA"));
                    if (Directory.Exists(recentPath))
                    {
                        var recentFiles = Directory.EnumerateFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

                        foreach (string recentFile in recentFiles)
                        {
                            // old method (needed interop dll)
                            //WshShell shell = new WshShell();
                            //IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(recentFile);

                            DateTime lastAccessed = File.GetLastAccessTime(recentFile);

                            if (lastAccessed > startTime)
                            {
                                // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                                Object shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shellObj, new object[] { recentFile });
                                Object TargetPath = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, new object[] { });
                                if (TargetPath.ToString().Trim() != "")
                                {
                                    results.Add(new Dictionary<string, string>()
                                {
                                    { "Target", TargetPath.ToString() },
                                    { "Accessed", string.Format("{0}", lastAccessed) }
                                });
                                }
                                Marshal.ReleaseComObject(shortcut);
                                shortcut = null;
                            }
                        }
                    }
                }
                // release the WshShell COM object
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }

        public static List<Dictionary<string, string>> ListMasterKeys()
        {
            // List DPAPI master-key filenames; the profile root must come from each enumerated user.
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = string.Format(@"{0}\Users", Environment.GetEnvironmentVariable("SystemDrive"));
                    var profiles = Directory.EnumerateDirectories(userFolder).Where(dir =>
                        !new[] { "Public", "Default", "Default User", "All Users" }
                            .Contains(Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase));
                    return ListMasterKeysInProfiles(profiles);
                }

                return ListMasterKeysInProfiles(new[] { Environment.GetEnvironmentVariable("USERPROFILE") });
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
                return new List<Dictionary<string, string>>();
            }
        }

        internal static List<Dictionary<string, string>> ListMasterKeysInProfiles(IEnumerable<string> profiles)
        {
            var results = new List<Dictionary<string, string>>();
            foreach (string dir in profiles)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                try
                {
                    foreach (string location in new[] { "Roaming", "Local" })
                    {
                        string protectPath = Path.Combine(dir, "AppData", location, "Microsoft", "Protect");
                        if (!Directory.Exists(protectPath)) continue;
                        foreach (string sidDirectory in Directory.EnumerateDirectories(protectPath))
                        {
                            foreach (string file in Directory.EnumerateFiles(sidDirectory))
                            {
                                if (!Regex.IsMatch(Path.GetFileName(file), @"^[0-9A-Fa-f]{8}(-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}$")) continue;
                                results.Add(new Dictionary<string, string>()
                                {
                                    { "MasterKey", file },
                                    { "Accessed", string.Format("{0}", File.GetLastAccessTime(file)) },
                                    { "Modified", string.Format("{0}", File.GetLastWriteTime(file)) },
                                });
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
                catch (IOException ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
                catch (System.Security.SecurityException ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetCredFiles()
        {
            var results = new List<Dictionary<string, string>>();
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = string.Format(@"{0}\Users", Environment.GetEnvironmentVariable("SystemDrive"));
                    var profiles = Directory.EnumerateDirectories(userFolder).Where(dir =>
                        !new[] { "Public", "Default", "Default User", "All Users" }
                            .Contains(Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase));
                    results.AddRange(GetCredFilesInProfiles(profiles));
                    string systemProfile = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"),
                        "System32", "config", "systemprofile");
                    AddCredentialFilesInDirectory(Path.Combine(systemProfile, "AppData", "Local", "Microsoft", "Credentials"),
                        systemProfile, results);
                }
                else
                {
                    results.AddRange(GetCredFilesInProfiles(new[] { Environment.GetEnvironmentVariable("USERPROFILE") }));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        internal static List<Dictionary<string, string>> GetCredFilesInProfiles(IEnumerable<string> profiles)
        {
            var results = new List<Dictionary<string, string>>();
            foreach (string profile in profiles)
            {
                if (string.IsNullOrEmpty(profile)) continue;
                try
                {
                    foreach (string location in new[] { "Local", "Roaming" })
                    {
                        AddCredentialFilesInDirectory(Path.Combine(profile, "AppData", location, "Microsoft", "Credentials"),
                            profile, results);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
            }
            return results;
        }

        private static void AddCredentialFilesInDirectory(string directory, string profile,
            ICollection<Dictionary<string, string>> results)
        {
            try
            {
                if (!Directory.Exists(directory)) return;
                foreach (string file in Directory.EnumerateFiles(directory))
                {
                    var entry = new Dictionary<string, string>
                    {
                        { "CredFile", file },
                        { "Profile", profile },
                        { "Owner", Path.GetFileName(profile.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) }
                    };
                    results.Add(entry);
                    try
                    {
                        var info = new FileInfo(file);
                        entry["Size"] = info.Length.ToString();
                        entry["Accessed"] = info.LastAccessTime.ToString();
                        entry["Modified"] = info.LastWriteTime.ToString();
                        ReadCredentialMetadata(file, entry);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void ReadCredentialMetadata(string file, IDictionary<string, string> entry)
        {
            const int headerLength = 60;
            const int maxDescriptionBytes = 4096;
            Guid dpapiProvider = new Guid("df9d8cd0-1501-11d1-8c7a-00c04fc297eb");
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length < headerLength) return;
                byte[] header = new byte[headerLength];
                if (!ReadExactly(stream, header, headerLength)) return;

                byte[] providerBytes = new byte[16];
                Array.Copy(header, 16, providerBytes, 0, providerBytes.Length);
                if (BitConverter.ToInt32(header, 12) != 1 || new Guid(providerBytes) != dpapiProvider) return;

                byte[] masterKeyBytes = new byte[16];
                Array.Copy(header, 36, masterKeyBytes, 0, masterKeyBytes.Length);
                Guid masterKey = new Guid(masterKeyBytes);
                if (masterKey == Guid.Empty) return;
                entry["MasterKey"] = masterKey.ToString();

                int descriptionLength = BitConverter.ToInt32(header, 56);
                if (descriptionLength <= 0 || descriptionLength > maxDescriptionBytes ||
                    (descriptionLength & 1) != 0 || descriptionLength > stream.Length - headerLength) return;

                byte[] descriptionBytes = new byte[descriptionLength];
                if (!ReadExactly(stream, descriptionBytes, descriptionLength)) return;
                string description = Encoding.Unicode.GetString(descriptionBytes).TrimEnd('\0');
                if (description.Any(char.IsControl)) return;
                entry["Description"] = description;
            }
        }

        private static bool ReadExactly(Stream stream, byte[] bytes, int length)
        {
            for (int offset = 0; offset < length;)
            {
                int read = stream.Read(bytes, offset, length - offset);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }
    }
}
