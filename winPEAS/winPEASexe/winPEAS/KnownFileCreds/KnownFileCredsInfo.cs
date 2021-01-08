using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using winPEAS.Utils;

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
                        results = RegistryHelper.GetRegValues("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU", SID));
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
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string awsKeyFile = String.Format("{0}\\.aws\\credentials", dir);
                            if (System.IO.File.Exists(awsKeyFile))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(awsKeyFile);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(awsKeyFile);
                                long size = new System.IO.FileInfo(awsKeyFile).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", awsKeyFile },
                                    { "Description", "AWS credentials file" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string computeCredsDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\credentials.db", dir);
                            if (System.IO.File.Exists(computeCredsDb))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeCredsDb);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(computeCredsDb);
                                long size = new System.IO.FileInfo(computeCredsDb).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", computeCredsDb },
                                    { "Description", "GC Compute creds" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string computeLegacyCreds = String.Format("{0}\\AppData\\Roaming\\gcloud\\legacy_credentials", dir);
                            if (System.IO.File.Exists(computeLegacyCreds))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeLegacyCreds);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(computeLegacyCreds);
                                long size = new System.IO.FileInfo(computeLegacyCreds).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", computeLegacyCreds },
                                    { "Description", "GC Compute creds legacy" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string computeAccessTokensDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\access_tokens.db", dir);
                            if (System.IO.File.Exists(computeAccessTokensDb))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeAccessTokensDb);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(computeAccessTokensDb);
                                long size = new System.IO.FileInfo(computeAccessTokensDb).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", computeAccessTokensDb },
                                    { "Description", "GC Compute tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string azureTokens = String.Format("{0}\\.azure\\accessTokens.json", dir);
                            if (System.IO.File.Exists(azureTokens))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureTokens);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(azureTokens);
                                long size = new System.IO.FileInfo(azureTokens).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", azureTokens },
                                    { "Description", "Azure tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string azureProfile = String.Format("{0}\\.azure\\azureProfile.json", dir);
                            if (System.IO.File.Exists(azureProfile))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureProfile);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(azureProfile);
                                long size = new System.IO.FileInfo(azureProfile).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", azureProfile },
                                    { "Description", "Azure profile" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                        }
                    }
                }
                else
                {
                    string awsKeyFile = String.Format("{0}\\.aws\\credentials", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(awsKeyFile))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(awsKeyFile);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(awsKeyFile);
                        long size = new System.IO.FileInfo(awsKeyFile).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", awsKeyFile },
                                    { "Description", "AWS keys file" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string computeCredsDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\credentials.db", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(computeCredsDb))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeCredsDb);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(computeCredsDb);
                        long size = new System.IO.FileInfo(computeCredsDb).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", computeCredsDb },
                                    { "Description", "GC Compute creds" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string computeLegacyCreds = String.Format("{0}\\AppData\\Roaming\\gcloud\\legacy_credentials", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(computeLegacyCreds))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeLegacyCreds);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(computeLegacyCreds);
                        long size = new System.IO.FileInfo(computeLegacyCreds).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", computeLegacyCreds },
                                    { "Description", "GC Compute creds legacy" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string computeAccessTokensDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\access_tokens.db", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(computeAccessTokensDb))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeAccessTokensDb);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(computeAccessTokensDb);
                        long size = new System.IO.FileInfo(computeAccessTokensDb).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", computeAccessTokensDb },
                                    { "Description", "GC Compute tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string azureTokens = String.Format("{0}\\.azure\\accessTokens.json", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(azureTokens))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureTokens);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(azureTokens);
                        long size = new System.IO.FileInfo(azureTokens).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", azureTokens },
                                    { "Description", "Azure tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string azureProfile = String.Format("{0}\\.azure\\azureProfile.json", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(azureProfile))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureProfile);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(azureProfile);
                        long size = new System.IO.FileInfo(azureProfile).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", azureProfile },
                                    { "Description", "Azure profile" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }


        public static List<Dictionary<string, string>> GetRecentFiles()
        {
            // parses recent file shortcuts via COM
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            int lastDays = 7;
            DateTime startTime = System.DateTime.Now.AddDays(-lastDays);

            try
            {
                // WshShell COM object GUID 
                Type shell = Type.GetTypeFromCLSID(new Guid("F935DC22-1CF0-11d0-ADB9-00C04FD58A0B"));
                Object shellObj = Activator.CreateInstance(shell);

                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];

                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string recentPath = String.Format("{0}\\AppData\\Roaming\\Microsoft\\Windows\\Recent\\", dir);
                            try
                            {
                                string[] recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

                                if (recentFiles.Length != 0)
                                {
                                    Console.WriteLine("   {0} :\r\n", userName);
                                    foreach (string recentFile in recentFiles)
                                    {
                                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(recentFile);

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
                                                    { "Accessed", String.Format("{0}", lastAccessed) }
                                                });
                                            }
                                            Marshal.ReleaseComObject(shortcut);
                                            shortcut = null;
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
                    string recentPath = String.Format("{0}\\Microsoft\\Windows\\Recent\\", System.Environment.GetEnvironmentVariable("APPDATA"));

                    string[] recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

                    foreach (string recentFile in recentFiles)
                    {
                        // old method (needed interop dll)
                        //WshShell shell = new WshShell();
                        //IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(recentFile);

                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(recentFile);

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
                                    { "Accessed", String.Format("{0}", lastAccessed) }
                                });
                            }
                            Marshal.ReleaseComObject(shortcut);
                            shortcut = null;
                        }
                    }
                }
                // release the WshShell COM object
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
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
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            List<string> userDPAPIBasePaths = new List<string>();
                            userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));
                            userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));

                            foreach (string userDPAPIBasePath in userDPAPIBasePaths)
                            {
                                if (System.IO.Directory.Exists(userDPAPIBasePath))
                                {
                                    string[] directories = Directory.GetDirectories(userDPAPIBasePath);
                                    foreach (string directory in directories)
                                    {
                                        string[] files = Directory.GetFiles(directory);

                                        foreach (string file in files)
                                        {
                                            if (Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                                            {
                                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                                DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                                string fileName = System.IO.Path.GetFileName(file);
                                                results.Add(new Dictionary<string, string>()
                                            {
                                                { "MasterKey", file },
                                                { "Accessed", String.Format("{0}", lastAccessed) },
                                                { "Modified", String.Format("{0}", lastModified) },
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
                    List<string> userDPAPIBasePaths = new List<string>();
                    userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));
                    userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));

                    foreach (string userDPAPIBasePath in userDPAPIBasePaths) 
                    {
                        if (System.IO.Directory.Exists(userDPAPIBasePath))
                        {
                            string[] directories = Directory.GetDirectories(userDPAPIBasePath);
                            foreach (string directory in directories)
                            {
                                string[] files = Directory.GetFiles(directory);

                                foreach (string file in files)
                                {
                                    if (Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                                    {
                                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                        DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                        string fileName = System.IO.Path.GetFileName(file);
                                        results.Add(new Dictionary<string, string>()
                                    {
                                        { "MasterKey", file },
                                        { "Accessed", String.Format("{0}", lastAccessed) },
                                        { "Modified", String.Format("{0}", lastModified) },
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
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
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);

                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            List<string> userCredFilePaths = new List<string>();
                            userCredFilePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", dir));
                            userCredFilePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Credentials\\", dir));

                            foreach (string userCredFilePath in userCredFilePaths)
                            {
                                if (System.IO.Directory.Exists(userCredFilePath))
                                {
                                    string[] systemFiles = Directory.GetFiles(userCredFilePath);
                                    if ((systemFiles != null) && (systemFiles.Length != 0))
                                    {
                                        foreach (string file in systemFiles)
                                        {
                                            DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                            DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                            long size = new System.IO.FileInfo(file).Length;
                                            string fileName = System.IO.Path.GetFileName(file);

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
                                            { "MasterKey", String.Format("{0}", guidMasterKey) },
                                            { "Accessed", String.Format("{0}", lastAccessed) },
                                            { "Modified", String.Format("{0}", lastModified) },
                                            { "Size", String.Format("{0}", size) },
                                        });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string systemFolder = String.Format("{0}\\System32\\config\\systemprofile\\AppData\\Local\\Microsoft\\Credentials", Environment.GetEnvironmentVariable("SystemRoot"));
                    string[] files = Directory.GetFiles(systemFolder);
                    if ((files != null) && (files.Length != 0))
                    {
                        foreach (string file in files)
                        {
                            DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                            DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                            long size = new System.IO.FileInfo(file).Length;
                            string fileName = System.IO.Path.GetFileName(file);

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
                                { "MasterKey", String.Format("{0}", guidMasterKey) },
                                { "Accessed", String.Format("{0}", lastAccessed) },
                                { "Modified", String.Format("{0}", lastModified) },
                                { "Size", String.Format("{0}", size) },
                            });
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    List<string> userCredFilePaths = new List<string>();
                    userCredFilePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));
                    userCredFilePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Credentials\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));

                    foreach (string userCredFilePath in userCredFilePaths)
                    {
                        if (System.IO.Directory.Exists(userCredFilePath))
                        {
                            string[] files = Directory.GetFiles(userCredFilePath);

                            foreach (string file in files)
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                long size = new System.IO.FileInfo(file).Length;
                                string fileName = System.IO.Path.GetFileName(file);

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
                                { "MasterKey", String.Format("{0}", guidMasterKey) },
                                { "Accessed", String.Format("{0}", lastAccessed) },
                                { "Modified", String.Format("{0}", lastModified) },
                                { "Size", String.Format("{0}", size) },
                            });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }              
    }
}
