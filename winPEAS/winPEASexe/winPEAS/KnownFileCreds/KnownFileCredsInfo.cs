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
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // lists any found DPAPI master keys
            try
            {
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
                            List<string> userDPAPIBasePaths = new List<string>
                            {
                                string.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", Environment.GetEnvironmentVariable("USERPROFILE")),
                                string.Format("{0}\\AppData\\Local\\Microsoft\\Protect\\", Environment.GetEnvironmentVariable("USERPROFILE"))
                            };

                            foreach (string userDPAPIBasePath in userDPAPIBasePaths)
                            {
                                if (Directory.Exists(userDPAPIBasePath))
                                {
                                    var directories = Directory.EnumerateDirectories(userDPAPIBasePath);
                                    foreach (string directory in directories)
                                    {
                                        var files = Directory.EnumerateFiles(directory);

                                        foreach (string file in files)
                                        {
                                            if (Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                                            {
                                                DateTime lastAccessed = File.GetLastAccessTime(file);
                                                DateTime lastModified = File.GetLastWriteTime(file);
                                                string fileName = Path.GetFileName(file);
                                                results.Add(new Dictionary<string, string>()
                                                {
                                                    { "MasterKey", file },
                                                    { "Accessed", string.Format("{0}", lastAccessed) },
                                                    { "Modified", string.Format("{0}", lastModified) },
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    List<string> userDPAPIBasePaths = new List<string>
                    {
                        string.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", Environment.GetEnvironmentVariable("USERPROFILE")),
                        string.Format("{0}\\AppData\\Local\\Microsoft\\Protect\\", Environment.GetEnvironmentVariable("USERPROFILE"))
                    };

                    foreach (string userDPAPIBasePath in userDPAPIBasePaths)
                    {
                        if (Directory.Exists(userDPAPIBasePath))
                        {
                            var directories = Directory.EnumerateDirectories(userDPAPIBasePath);
                            foreach (string directory in directories)
                            {
                                var files = Directory.EnumerateFiles(directory);

                                foreach (string file in files)
                                {
                                    if (Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                                    {
                                        DateTime lastAccessed = File.GetLastAccessTime(file);
                                        DateTime lastModified = File.GetLastWriteTime(file);
                                        string fileName = Path.GetFileName(file);
                                        results.Add(new Dictionary<string, string>()
                                    {
                                        { "MasterKey", file },
                                        { "Accessed", string.Format("{0}", lastAccessed) },
                                        { "Modified", string.Format("{0}", lastModified) },
                                    });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetCredFiles()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // lists any found files in Local\Microsoft\Credentials\*
            try
            {
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
                            List<string> userCredFilePaths = new List<string>
                            {
                                string.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", dir),
                                string.Format("{0}\\AppData\\Roaming\\Microsoft\\Credentials\\", dir)
                            };

                            foreach (string userCredFilePath in userCredFilePaths)
                            {
                                if (Directory.Exists(userCredFilePath))
                                {
                                    var systemFiles = Directory.EnumerateFiles(userCredFilePath);
                                    if ((systemFiles != null))
                                    {
                                        foreach (string file in systemFiles)
                                        {
                                            DateTime lastAccessed = File.GetLastAccessTime(file);
                                            DateTime lastModified = File.GetLastWriteTime(file);
                                            long size = new FileInfo(file).Length;
                                            string fileName = Path.GetFileName(file);

                                            // jankily parse the bytes to extract the credential type and master key GUID
                                            // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                                            byte[] credentialArray = File.ReadAllBytes(file);
                                            byte[] guidMasterKeyArray = new byte[16];
                                            Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                                            Guid guidMasterKey = new Guid(guidMasterKeyArray);

                                            byte[] stringLenArray = new byte[16];
                                            Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                                            int descLen = BitConverter.ToInt32(stringLenArray, 0);

                                            byte[] descBytes = new byte[descLen];
                                            Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                                            string desc = Encoding.Unicode.GetString(descBytes);
                                            results.Add(new Dictionary<string, string>()
                                            {
                                                { "CredFile", file },
                                                { "Description", desc },
                                                { "MasterKey", string.Format("{0}", guidMasterKey) },
                                                { "Accessed", string.Format("{0}", lastAccessed) },
                                                { "Modified", string.Format("{0}", lastModified) },
                                                { "Size", string.Format("{0}", size) },
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string systemFolder = string.Format("{0}\\System32\\config\\systemprofile\\AppData\\Local\\Microsoft\\Credentials", Environment.GetEnvironmentVariable("SystemRoot"));
                    if (Directory.Exists(systemFolder))
                    {
                        var files = Directory.EnumerateFiles(systemFolder);
                        if ((files != null))
                        {
                            foreach (string file in files)
                            {
                                DateTime lastAccessed = File.GetLastAccessTime(file);
                                DateTime lastModified = File.GetLastWriteTime(file);
                                long size = new System.IO.FileInfo(file).Length;
                                string fileName = Path.GetFileName(file);

                                // jankily parse the bytes to extract the credential type and master key GUID
                                // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                                byte[] credentialArray = File.ReadAllBytes(file);
                                byte[] guidMasterKeyArray = new byte[16];
                                Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                                Guid guidMasterKey = new Guid(guidMasterKeyArray);

                                byte[] stringLenArray = new byte[16];
                                Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                                int descLen = BitConverter.ToInt32(stringLenArray, 0);

                                byte[] descBytes = new byte[descLen];
                                Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                                string desc = Encoding.Unicode.GetString(descBytes);
                                results.Add(new Dictionary<string, string>()
                                {
                                    { "CredFile", file },
                                    { "Description", desc },
                                    { "MasterKey", string.Format("{0}", guidMasterKey) },
                                    { "Accessed", string.Format("{0}", lastAccessed) },
                                    { "Modified", string.Format("{0}", lastModified) },
                                    { "Size", string.Format("{0}", size) },
                                });
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    List<string> userCredFilePaths = new List<string>
                    {
                        string.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", Environment.GetEnvironmentVariable("USERPROFILE")),
                        string.Format("{0}\\AppData\\Roaming\\Microsoft\\Credentials\\", Environment.GetEnvironmentVariable("USERPROFILE"))
                    };

                    foreach (string userCredFilePath in userCredFilePaths)
                    {
                        if (Directory.Exists(userCredFilePath))
                        {
                            var files = Directory.EnumerateFiles(userCredFilePath);

                            foreach (string file in files)
                            {
                                DateTime lastAccessed = File.GetLastAccessTime(file);
                                DateTime lastModified = File.GetLastWriteTime(file);
                                long size = new System.IO.FileInfo(file).Length;
                                string fileName = Path.GetFileName(file);

                                // jankily parse the bytes to extract the credential type and master key GUID
                                // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                                byte[] credentialArray = File.ReadAllBytes(file);
                                byte[] guidMasterKeyArray = new byte[16];
                                Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                                Guid guidMasterKey = new Guid(guidMasterKeyArray);

                                byte[] stringLenArray = new byte[16];
                                Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                                int descLen = BitConverter.ToInt32(stringLenArray, 0);

                                byte[] descBytes = new byte[descLen];
                                Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                                string desc = Encoding.Unicode.GetString(descBytes);
                                results.Add(new Dictionary<string, string>()
                                {
                                { "CredFile", file },
                                { "Description", desc },
                                { "MasterKey", string.Format("{0}", guidMasterKey) },
                                { "Accessed", string.Format("{0}", lastAccessed) },
                                { "Modified", string.Format("{0}", lastModified) },
                                { "Size", string.Format("{0}", size) },
                            });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
