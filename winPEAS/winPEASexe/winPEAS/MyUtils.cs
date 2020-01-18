using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Security.Principal;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
//using Colorful;
using System.Threading;

namespace winPEAS
{
    class MyUtils
    {
        public static string IsDomainJoined()
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
            //By default true, because this way will check domain and local, but never should get here the code
            return "";
        }

        public static Dictionary<string, string> RemoveEmptyKeys(Dictionary<string, string> dic_in)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                foreach (KeyValuePair<string, string> entry in dic_in)
                    if (!String.IsNullOrEmpty(entry.Value.Trim()))
                        results[entry.Key] = entry.Value;
                return results;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
        public static List<string> ListFolder(String path)
        {
            string root = @Path.GetPathRoot(Environment.SystemDirectory) + path;
            var dirs = from dir in Directory.EnumerateDirectories(root) select dir;
            return dirs.ToList();
        }

        //From https://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
        public static Dictionary<string, string> GecRecursivePrivs(string path)
        {
            /*string root = @Path.GetPathRoot(Environment.SystemDirectory) + path;
            var dirs = from dir in Directory.EnumerateDirectories(root) select dir;
            return dirs.ToList();*/
            Dictionary<string, string> results = new Dictionary<string, string>();
            results[path] = ""; //If you cant open, then there are no privileges for you (and the try will explode)
            try
            {
                results[path] = String.Join(", ", GetPermissionsFolder(path, Program.interestingUsersGroups));
                if (String.IsNullOrEmpty(results[path]))
                {
                    foreach (string d in Directory.GetDirectories(path))
                    {
                        foreach (string f in Directory.GetFiles(d))
                        {
                            results[f] = String.Join(", ", GetPermissionsFile(f, Program.interestingUsersGroups));
                        }
                        results.Concat(GecRecursivePrivs(d)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                }
            }
            catch
            {
                //Access denied to a path
            }
            return results;
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

        // From Seatbelt
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
            Match match_path = Regex.Match(path, @"^\W*([a-z]:\\.+?(\.exe|\.dll|\.sys))\W*", RegexOptions.IgnoreCase);
            if (match_path.Groups.Count > 1)
                binaryPath = match_path.Groups[1].ToString();
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
                binaryPath = GetExecutableFromPath(system32dir +"\\" + path);
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

        public static List<string> GetPermissionsFile(string path, List<string> lowgroups)
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
                foreach (FileSystemAccessRule rule in fSecurity.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    int current_perm = (int)rule.FileSystemRights;
                    string current_perm_str = permInt2Str(current_perm);
                    if (current_perm_str == "")
                        continue;

                    foreach (string group in lowgroups)
                    {
                        if (rule.IdentityReference.Value.ToLower().Contains(group.ToLower()))
                        {
                            string to_add = String.Format("{0} [{1}]", rule.IdentityReference.Value, current_perm_str);
                            if (!results.Contains(to_add))
                            {
                                results.Add(to_add);
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                //By some reason some times it cannot find a file or cannot get permissions (normally with some binaries inside system32)
            }
            return results;
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

        public static List<string> GetPermissionsFolder(string path, List<string> NtAccountNames)
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
                //Go through the rules returned from the DirectorySecurity
                foreach (FileSystemAccessRule rule in fSecurity.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    int current_perm = (int)rule.FileSystemRights;
                    string current_perm_str = permInt2Str(current_perm);
                    if (current_perm_str == "")
                        continue;

                    //If we find one that matches the identity we are looking for
                    foreach (string name in NtAccountNames)
                    {
                        if (rule.IdentityReference.Value.ToLower().Contains(name.ToLower()))
                        {
                            string to_add = String.Format("{0} [{1}]", rule.IdentityReference.Value, current_perm_str);
                            if (!results.Contains(to_add))
                            {
                                results.Add(to_add);
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                //Te exceptions here use to be "Not access to a file", nothing interesting
            }
            return results;
        }

        public static string permInt2Str(int current_perm)
        {
            Dictionary<string, int> interesting_perms = new Dictionary<string, int>()
            {
                { "GenericAll", 268435456},
                { "FullControl", (int)FileSystemRights.FullControl },
                { "TakeOwnership", (int)FileSystemRights.TakeOwnership },
                { "GenericWrite", 1073741824 },
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
                Beaprint.GrayPrint("Error in permInt2Str: " + ex);
            }
            return "";
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

        //Adapted from https://social.msdn.microsoft.com/Forums/vstudio/en-US/378491d6-23a3-4ae7-a702-c52c5abb0e8d/access-to-both-32-and-64-bit-registry-using-c-and-regmultisz?forum=csharpgeneral
        [DllImport("Advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode)]
        static extern int RegOpenKeyEx(IntPtr hKey, [In] string lpSubKey, int ulOptions, int samDesired, out IntPtr phkResult);
        [DllImport("Advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode)]
        static extern int RegQueryValueEx(IntPtr hKey, [In] string lpValueName, IntPtr lpReserved, out int lpType, [Out] byte[] lpData, ref int lpcbData);
        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(IntPtr hKey);

        static public readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(-2147483648);
        static public readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);
        static public readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);
        static public readonly IntPtr HKEY_USERS = new IntPtr(-2147483645);
        static public readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(-2147483644);
        static public readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(-2147483643);
        static public readonly IntPtr HKEY_DYN_DATA = new IntPtr(-2147483642);

        public const int KEY_READ = 0x20019;
        public const int KEY_WRITE = 0x20006;
        public const int KEY_QUERY_VALUE = 0x0001;
        public const int KEY_SET_VALUE = 0x0002;
        public const int KEY_WOW64_64KEY = 0x0100;
        public const int KEY_WOW64_32KEY = 0x0200;
        public const int KEY_ALL_ACCESS = 0xF003F;

        public const int REG_NONE = 0;
        public const int REG_SZ = 1;
        public const int REG_EXPAND_SZ = 2;
        public const int REG_BINARY = 3;
        public const int REG_DWORD = 4;
        public const int REG_DWORD_BIG_ENDIAN = 5;
        public const int REG_LINK = 6;
        public const int REG_MULTI_SZ = 7;
        public const int REG_RESOURCE_LIST = 8;
        public const int REG_FULL_RESOURCE_DESCRIPTOR = 9;
        public const int REG_RESOURCE_REQUIREMENTS_LIST = 10;
        public const int REG_QWORD = 11;

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct SECURITY_DESCRIPTOR
        {
            public byte revision;
            public byte size;
            public short control;
            public IntPtr owner;
            public IntPtr group;
            public IntPtr sacl;
            public IntPtr dacl;
        }

        public static bool CheckWriteAccessReg(string root_name, string reg_path)
        {
            IntPtr key;
            IntPtr root = HKEY_LOCAL_MACHINE;
            if (root_name.Contains("HKCU") || root_name.Contains("CURRENT_USER"))
                root = HKEY_CURRENT_USER;
            else if (root_name.Contains("HKLM") || root_name.Contains("LOCAL_MACHINE"))
                root = HKEY_LOCAL_MACHINE;

            if (RegOpenKeyEx(root, reg_path, 0, KEY_ALL_ACCESS, out key) != 0)
            {
                if (RegOpenKeyEx(root, reg_path, 0, KEY_WRITE, out key) != 0)
                {
                    if (RegOpenKeyEx(root, reg_path, 0, KEY_SET_VALUE, out key) != 0)
                        return false;
                }
            }
            return true;
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
                    Beaprint.AnsiPrint(String.Join("\n", Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly).Where(filepath => !filepath.Contains(".dll"))), color);
                }

                if (!Program.search_fast)
                    Thread.Sleep(Program.search_time);

                // go recurse in all sub-directories
                foreach (var directory in Directory.GetDirectories(path))
                    FindFiles(directory, patterns, color);
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
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
