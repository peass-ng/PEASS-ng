using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Security.Principal;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Threading;

namespace winPEAS
{
    class MyUtils
    {
        //////////////////////
        /// IsDomainJoined ///
        //////////////////////
        /// The clases and functions here are dedicated to discover if the current host is joined in a domain or not, and get the domain name if so
        /// It can be done using .Net (default) and WMI (used if .Net fails)
        internal class Win32
        {
            public const int ErrorSuccess = 0;

            [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

            [DllImport("Netapi32.dll")]
            public static extern int NetApiBufferFree(IntPtr Buffer);

            public enum NetJoinStatus
            {
                NetSetupUnknownStatus = 0,
                NetSetupUnjoined,
                NetSetupWorkgroupName,
                NetSetupDomainName
            }

        }

        public static string IsDomainJoined()
        {
            // returns Compuer Domain if the system is inside an AD (an nothing if it is not)
            try
            {
                Win32.NetJoinStatus status = Win32.NetJoinStatus.NetSetupUnknownStatus;
                IntPtr pDomain = IntPtr.Zero;
                int result = Win32.NetGetJoinInformation(null, out pDomain, out status);
                if (pDomain != IntPtr.Zero)
                {
                    Win32.NetApiBufferFree(pDomain);
                }

                if (result == Win32.ErrorSuccess)
                {
                    // If in domain, return domain name, if not, return empty
                    if (status == Win32.NetJoinStatus.NetSetupDomainName)
                        return Environment.UserDomainName;
                    return "";
                }

            }

            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}\n Trying to check if domain is joined using WMI", ex.Message));
                IsDomainJoinedWmi();
            }
            return "";
        }

        public static string IsDomainJoinedWmi()
        {
            // returns Compuer Domain if the system is inside an AD (an nothing if it is not)
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    using (var items = searcher.Get())
                    {
                        foreach (var item in items)
                        {
                            return (string)item["Domain"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            //By default local
            return "";
        }


        ///////////////////////////////////////////
        /// Interf. for Keys and Values in Reg. ///
        ///////////////////////////////////////////
        /// Functions related to obtain keys and values from the registry
        /// Some parts adapted from Seatbelt
        public static string GetRegValue(string hive, string path, string value)
        {
            // returns a single registry value under the specified path in the specified hive (HKLM/HKCU)
            string regKeyValue = "";
            if (hive == "HKCU")
            {
                var regKey = Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
        }

        public static Dictionary<string, object> GetRegValues(string hive, string path)
        {
            // returns all registry values under the specified path in the specified hive (HKLM/HKCU)
            Dictionary<string, object> keyValuePairs = null;
            try
            {
                if (hive == "HKCU")
                {
                    using (var regKeyValues = Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                else if (hive == "HKU")
                {
                    using (var regKeyValues = Registry.Users.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                else
                {
                    using (var regKeyValues = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                return keyValuePairs;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] GetRegValueBytes(string hive, string path, string value)
        {
            // returns a byte array of single registry value under the specified path in the specified hive (HKLM/HKCU)
            byte[] regKeyValue = null;
            if (hive == "HKCU")
            {
                var regKey = Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
        }

        public static string[] GetRegSubkeys(string hive, string path)
        {
            // returns an array of the subkeys names under the specified path in the specified hive (HKLM/HKCU/HKU)
            try
            {
                Microsoft.Win32.RegistryKey myKey = null;
                if (hive == "HKLM")
                {
                    myKey = Registry.LocalMachine.OpenSubKey(path);
                }
                else if (hive == "HKU")
                {
                    myKey = Registry.Users.OpenSubKey(path);
                }
                else
                {
                    myKey = Registry.CurrentUser.OpenSubKey(path);
                }
                String[] subkeyNames = myKey.GetSubKeyNames();
                return myKey.GetSubKeyNames();
            }
            catch
            {
                return new string[0];
            }
        }

        public static string GetCLSIDBinPath(string CLSID)
        {
            return GetRegValue("HKLM", @"SOFTWARE\Classes\CLSID\" + CLSID + @"\InprocServer32", ""); //To get the default object you need to use an empty string
        }


        ///////////////////////////////////
        //////// Check Permissions ////////
        ///////////////////////////////////
        /// Get interesting permissions from Files, Folders and Registry
        public static List<string> GetPermissionsFile(string path, Dictionary<string,string> SIDs)
        {
            /*Permisos especiales para carpetas 
             *https://docs.microsoft.com/en-us/windows/win32/secauthz/access-mask-format?redirectedfrom=MSDN
             *https://docs.microsoft.com/en-us/windows/win32/fileio/file-security-and-access-rights?redirectedfrom=MSDN
             */

            List<string> results = new List<string>();
            path = path.Trim();
            if (path == null || path == "")
                return results;

            Match reg_path = Regex.Match(path.ToString(), @"\W*([a-z]:\\.+?(\.[a-zA-Z0-9_-]+))\W*", RegexOptions.IgnoreCase);
            string binaryPath = reg_path.Groups[1].ToString();
            path = binaryPath;
            if (path == null || path == "")
                return results;

            try
            {
                FileSecurity fSecurity = File.GetAccessControl(path);
                results = GetMyPermissionsF(fSecurity, SIDs);
            }
            catch
            {
                //By some reason some times it cannot find a file or cannot get permissions (normally with some binaries inside system32)
            }
            return results;
        }

        public static List<string> GetPermissionsFolder(string path, Dictionary<string, string> SIDs)
        {
            List<string> results = new List<string>();

            try
            {
                path = path.Trim();
                if (String.IsNullOrEmpty(path))
                    return results;

                path = GetFolderFromString(path);

                if (String.IsNullOrEmpty(path))
                    return results;

                FileSecurity fSecurity = File.GetAccessControl(path);
                results = GetMyPermissionsF(fSecurity, SIDs);
            }
            catch
            {
                //Te exceptions here use to be "Not access to a file", nothing interesting
            }
            return results;
        }

        public static List<string> GetMyPermissionsF(FileSecurity fSecurity, Dictionary<string, string> SIDs)
        {
            // Get interesting permissions in fSecurity (Only files and folders)
            List<string> results = new List<string>();
            Dictionary<string, string> container = new Dictionary<string, string>();

            foreach (FileSystemAccessRule rule in fSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                //First, check if the rule to check is interesting
                int current_perm = (int)rule.FileSystemRights;
                string current_perm_str = PermInt2Str(current_perm, false);
                if (current_perm_str == "")
                    continue;

                foreach (KeyValuePair<string, string> mySID in SIDs)
                {
                    // If the rule is interesting, check if any of my SIDs is in the rule
                    if (rule.IdentityReference.Value.ToLower() == mySID.Key.ToLower())
                    {
                        string SID_name = String.IsNullOrEmpty(mySID.Value) ? mySID.Key : mySID.Value;

                        if (container.ContainsKey(SID_name))
                        {
                            if (!container[SID_name].Contains(current_perm_str))
                                container[SID_name] += " " + current_perm_str;
                        }
                        else
                            container[SID_name] = current_perm_str;

                        string to_add = String.Format("{0} [{1}]", SID_name, current_perm_str);
                    }
                }
            }
            foreach (KeyValuePair<string, string> SID_input in container)
            {
                string to_add = String.Format("{0} [{1}]", SID_input.Key, SID_input.Value);
                results.Add(to_add);
            }
            return results;
        }

        public static List<string> GetMyPermissionsR(RegistryKey key, Dictionary<string, string> SIDs)
        {
            // Get interesting permissions in rSecurity (Only Registry)
            List<string> results = new List<string>();
            Dictionary<string, string> container = new Dictionary<string, string>();

            try
            {
                var rSecurity = key.GetAccessControl();

                //Go through the rules returned from the DirectorySecurity
                foreach (RegistryAccessRule rule in rSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    int current_perm = (int)rule.RegistryRights;
                    string current_perm_str = PermInt2Str(current_perm, true);
                    if (current_perm_str == "")
                        continue;

                    foreach (KeyValuePair<string, string> mySID in SIDs)
                    {
                        // If the rule is interesting, check if any of my SIDs is in the rule
                        if (rule.IdentityReference.Value.ToLower() == mySID.Key.ToLower())
                        {
                            string SID_name = String.IsNullOrEmpty(mySID.Value) ? mySID.Key : mySID.Value;

                            if (container.ContainsKey(SID_name))
                            {
                                if (!container[SID_name].Contains(current_perm_str))
                                    container[SID_name] += " " + current_perm_str;
                            }
                            else
                                container[SID_name] = current_perm_str;

                            string to_add = String.Format("{0} [{1}]", SID_name, current_perm_str);
                        }
                    }
                }
                foreach (KeyValuePair<string, string> SID_input in container)
                {
                    string to_add = String.Format("{0} [{1}]", SID_input.Key, SID_input.Value);
                    results.Add(to_add);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static string PermInt2Str(int current_perm, bool only_write_or_equivalent = false, bool is_service=false)
        {
            Dictionary<string, int> interesting_perms = new Dictionary<string, int>()
                {
                    // This isn't an exhaustive list of possible permissions. Just the interesting ones.
                    { "AllAccess", 0xf01ff},
                    { "GenericAll", 0x10000000},
                    { "FullControl", (int)FileSystemRights.FullControl },
                    { "TakeOwnership", (int)FileSystemRights.TakeOwnership },
                    { "GenericWrite", 0x40000000 },
                    { "WriteData/CreateFiles", (int)FileSystemRights.WriteData },
                    { "Modify", (int)FileSystemRights.Modify },
                    { "Write", (int)FileSystemRights.Write },
                    { "ChangePermissions", (int)FileSystemRights.ChangePermissions },
                    { "Delete", (int)FileSystemRights.Delete },
                    { "DeleteSubdirectoriesAndFiles", (int)FileSystemRights.DeleteSubdirectoriesAndFiles },
                    { "AppendData/CreateDirectories", (int)FileSystemRights.AppendData },
                    { "WriteAttributes", (int)FileSystemRights.WriteAttributes },
                    { "WriteExtendedAttributes", (int)FileSystemRights.WriteExtendedAttributes },
                };

            if (only_write_or_equivalent)
            {
                interesting_perms = new Dictionary<string, int>()
                {
                    { "AllAccess", 0xf01ff},
                    { "GenericAll", 0x10000000},
                    { "FullControl", (int)FileSystemRights.FullControl }, //0x1f01ff
                    { "TakeOwnership", (int)FileSystemRights.TakeOwnership }, //0x80000
                    { "GenericWrite", 0x40000000 },
                    { "WriteData/CreateFiles", (int)FileSystemRights.WriteData }, //0x2
                    { "Modify", (int)FileSystemRights.Modify }, //0x301bf
                    { "Write", (int)FileSystemRights.Write }, //0x116
                    { "ChangePermissions", (int)FileSystemRights.ChangePermissions }, //0x40000
                };
            }

            if (is_service)
            {
                interesting_perms["Start"] = 0x00000010;
                interesting_perms["Stop"] = 0x00000020;
            }

            try
            {
                foreach (KeyValuePair<string, int> entry in interesting_perms)
                {
                    if ((entry.Value & current_perm) == entry.Value)
                        return entry.Key;
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error in PermInt2Str: " + ex);
            }
            return "";
        }

        //From https://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
        public static Dictionary<string, string> GetRecursivePrivs(string path, int cont=0)
        {
            /*string root = @Path.GetPathRoot(Environment.SystemDirectory) + path;
            var dirs = from dir in Directory.EnumerateDirectories(root) select dir;
            return dirs.ToList();*/
            Dictionary<string, string> results = new Dictionary<string, string>();
            int max_dir_recurse = 130;
            if (cont > max_dir_recurse)
                return results; //"Limit" for apps with hundreds of thousands of folders
            
            results[path] = ""; //If you cant open, then there are no privileges for you (and the try will explode)
            try
            {
                results[path] = String.Join(", ", GetPermissionsFolder(path, Program.currentUserSIDs));
                if (String.IsNullOrEmpty(results[path]))
                {
                    foreach (string d in Directory.GetDirectories(path))
                    {
                        foreach (string f in Directory.GetFiles(d))
                        {
                            results[f] = String.Join(", ", GetPermissionsFile(f, Program.currentUserSIDs));
                        }
                        cont += 1;
                        results.Concat(GetRecursivePrivs(d, cont)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                }
            }
            catch
            {
                //Access denied to a path
            }
            return results;
        }

        ////////////////////////////////////
        /////// MISC - Files & Paths ///////
        ////////////////////////////////////
        public static bool CheckIfDotNet(string path)
        {
            bool isDotNet = false;
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(path);
            string companyName = myFileVersionInfo.CompanyName;
            if ((String.IsNullOrEmpty(companyName)) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
            {
                try
                {
                    AssemblyName myAssemblyName = AssemblyName.GetAssemblyName(path);
                    isDotNet = true;
                }
                catch (System.IO.FileNotFoundException)
                {
                    // System.Console.WriteLine("The file cannot be found.");
                }
                catch (System.BadImageFormatException exception)
                {
                    if (Regex.IsMatch(exception.Message, ".*This assembly is built by a runtime newer than the currently loaded runtime and cannot be loaded.*", RegexOptions.IgnoreCase))
                    {
                        isDotNet = true;
                    }
                }
                catch
                {
                    // System.Console.WriteLine("The assembly has already been loaded.");
                }
            }
            return isDotNet;
        }

        public static string GetExecutableFromPath(string path)
        {
            string binaryPath = "";
            Match match_path = Regex.Match(path, @"^\W*([a-z]:\\.+?(\.exe|\.dll|\.sys))\W*", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
            if (match_path.Groups.Count > 1)
                binaryPath = match_path.Groups[1].ToString();

            //Check if rundll32
            string[] binaryPathdll32 = binaryPath.Split(new string[] {"Rundll32.exe"}, StringSplitOptions.None);

            if (binaryPathdll32.Length > 1)
            {
                binaryPath = binaryPathdll32[1].Trim();
            }
            return binaryPath;
        }

        public static string ReconstructExecPath(string path)
        {
            if (!path.Contains(".exe") && !path.Contains(".dll") && !path.Contains(".sys"))
                return "";

            string system32dir = Environment.SystemDirectory; // C:\windows\system32
            string windowsdir = Directory.GetParent(system32dir).ToString();
            string windrive = Path.GetPathRoot(system32dir); // C:\

            string binaryPath = GetExecutableFromPath(path);
            if (binaryPath == "")
            {
                binaryPath = GetExecutableFromPath(system32dir + "\\" + path);
                if (!File.Exists(binaryPath))
                {
                    binaryPath = GetExecutableFromPath(windowsdir + "\\" + path);
                    if (!File.Exists(binaryPath))
                    {
                        binaryPath = GetExecutableFromPath(windrive + "\\" + path);
                        if (!File.Exists(binaryPath))
                        {
                            binaryPath = "";
                        }
                    }
                }
            }
            return binaryPath;
        }

        public static string GetFolderFromString(string path)
        {
            string fpath = path;
            if (!Directory.Exists(path))
            {
                Match reg_path = Regex.Match(path.ToString(), @"\W*([a-z]:\\.+?(\.[a-zA-Z0-9_-]+))\W*", RegexOptions.IgnoreCase);
                string binaryPath = reg_path.Groups[1].ToString();
                if (File.Exists(binaryPath))
                    fpath = Path.GetDirectoryName(binaryPath);
                else
                    fpath = "";
            }
            return fpath;
        }


        public static bool CheckQuoteAndSpace(string path)
        {
            if (!path.Contains('"') && !path.Contains("'"))
            {
                if (path.Contains(" "))
                    return true;
            }
            return false;
        }

        public static List<string> FindFiles(string path, string patterns)
        {
            // finds files matching one or more patterns under a given path, recursive
            // adapted from http://csharphelper.com/blog/2015/06/find-files-that-match-multiple-patterns-in-c/
            //      pattern: "*pass*;*.png;"

            var files = new List<string>();

            try
            {
                // search every pattern in this directory's files
                foreach (string pattern in patterns.Split(';'))
                {
                    files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                }

                // go recurse in all sub-directories
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(FindFiles(directory, patterns));
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }

            return files;
        }

        public static void FindFiles(string path, string patterns, Dictionary<string, string> color)
        {
            try
            {
                // search every pattern in this directory's files
                foreach (string pattern in patterns.Split(';'))
                {
                    Beaprint.AnsiPrint("    "+String.Join("\n    ", Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly).Where(filepath => !filepath.Contains(".dll"))), color);
                }

                if (!Program.search_fast)
                    Thread.Sleep(Program.search_time);

                // go recurse in all sub-directories
                foreach (string directory in Directory.GetDirectories(path))
                {
                    if (!directory.Contains("AppData"))
                        FindFiles(directory, patterns, color);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
        }


        //////////////////////
        //////// MISC ////////
        //////////////////////
        public static List<string> ListFolder(String path)
        {
            string root = @Path.GetPathRoot(Environment.SystemDirectory) + path;
            var dirs = from dir in Directory.EnumerateDirectories(root) select dir;
            return dirs.ToList();
        }

        
        //From Seatbelt
        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        //From https://stackoverflow.com/questions/3519539/how-to-check-if-a-string-contains-any-of-some-strings
        public static bool ContainsAnyRegex(string haystack, List<string> regexps)
        {
            foreach (string regex in regexps)
            {
                if (Regex.Match(haystack, regex, RegexOptions.IgnoreCase).Success)
                    return true;
            }
            return false;
        }

        
        // From https://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results
        public static string ExecCMD(string args, string alternative_binary="")
        {
            //Create process
            Process pProcess = new Process();

            //No new window
            pProcess.StartInfo.CreateNoWindow = true;

            //strCommand is path and file name of command to run
            pProcess.StartInfo.FileName = (String.IsNullOrEmpty(alternative_binary)) ? "cmd.exe" : alternative_binary;

            //strCommandParameters are parameters to pass to program
            pProcess.StartInfo.Arguments = (String.IsNullOrEmpty(alternative_binary)) ? "/C " + args : args;

            pProcess.StartInfo.UseShellExecute = false;

            //Set output of program to be written to process output stream
            pProcess.StartInfo.RedirectStandardOutput = true;

            //Start the process
            pProcess.Start();

            //Get program output
            string strOutput = pProcess.StandardOutput.ReadToEnd();

            //Wait for process to finish
            pProcess.WaitForExit();

            return strOutput;
        }
    }
}
