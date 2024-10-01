using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using winPEAS.Helpers.Registry;

namespace winPEAS.Helpers
{
    public class MyUtils
    {
        public static string GetCLSIDBinPath(string CLSID)
        {
            return RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Classes\CLSID\" + CLSID + @"\InprocServer32",
                ""); //To get the default object you need to use an empty string
        }

        ////////////////////////////////////
        /////// MISC - Files & Paths ///////
        ////////////////////////////////////
        public static bool CheckIfDotNet(string path)
        {
            bool isDotNet = false;
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(path);
            string companyName = myFileVersionInfo.CompanyName;
            if ((string.IsNullOrEmpty(companyName)) ||
                (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
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
                    if (Regex.IsMatch(exception.Message,
                        ".*This assembly is built by a runtime newer than the currently loaded runtime and cannot be loaded.*",
                        RegexOptions.IgnoreCase))
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
            Match match_path = Regex.Match(path, @"^\W*([a-z]:\\.+?(\.exe|\.dll|\.sys))\W*",
                RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
            if (match_path.Groups.Count > 1)
            {
                binaryPath = match_path.Groups[1].ToString();
            }

            if (binaryPath.Contains('"'))
            {
                binaryPath = binaryPath.Split('"')[0];
                binaryPath = binaryPath.Trim();
            }

            //Check if rundll32
            string[] binaryPathdll32 = binaryPath.Split(new string[] { "Rundll32.exe" }, StringSplitOptions.None);

            if (binaryPathdll32.Length > 1)
            {
                binaryPath = binaryPathdll32[1].Trim();
            }

            return binaryPath;
        }

        internal static bool IsBase64String(string text)
        {
            text = text.Trim();
            return (text.Length % 4 == 0) && Regex.IsMatch(text, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
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

        public static bool CheckQuoteAndSpaceWithPermissions(string path, out List<string> injectablePaths)
        {
            List<string> result = new List<string>();
            bool isInjectable = false;

            if (!path.Contains('"') && !path.Contains("'"))
            {
                if (path.Contains(" "))
                {
                    string currentPath = string.Empty;
                    foreach (var pathPart in Regex.Split(path, @"\s"))
                    {
                        currentPath += pathPart + " ";

                        if (File.Exists(currentPath) || Directory.Exists(currentPath))
                        {
                            var permissions = PermissionsHelper.GetPermissionsFolder(currentPath, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);

                            if (permissions.Any())
                            {
                                result.Add(currentPath);
                                isInjectable = true;
                            }
                        }
                        else
                        {
                            var firstPathPart = currentPath;
                            DirectoryInfo di = new DirectoryInfo(firstPathPart);
                            var exploitablePath = di.Parent.FullName;
                            var folderPermissions = PermissionsHelper.GetPermissionsFolder(exploitablePath, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);

                            if (folderPermissions.Any())
                            {
                                result.Add(exploitablePath);
                                isInjectable = true;
                            };
                        }
                    }
                }
            }

            injectablePaths = result.Select(i => i).Distinct().ToList();
            return isInjectable;
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


        //////////////////////
        //////// MISC ////////
        //////////////////////
        public static List<string> ListFolder(String path)
        {
            string root = @Path.GetPathRoot(Environment.SystemDirectory) + path;
            var dirs = from dir in Directory.EnumerateDirectories(root) select dir;
            return dirs.ToList();
        }

        internal static byte[] CombineArrays(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        //From Seatbelt
        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
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

        internal static bool IsUrlReachable(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 5000;
                request.Method = "HEAD";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK && response.ContentLength > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }


        // From https://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results
        public static string ExecCMD(string args, string alternative_binary = "")
        {
            //Create process
            Process pProcess = new Process();

            //No new window
            pProcess.StartInfo.CreateNoWindow = true;

            //strCommand is path and file name of command to run
            pProcess.StartInfo.FileName = (string.IsNullOrEmpty(alternative_binary)) ? "cmd.exe" : alternative_binary;

            //strCommandParameters are parameters to pass to program
            pProcess.StartInfo.Arguments = (string.IsNullOrEmpty(alternative_binary)) ? "/C " + args : args;

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

        private static string[] suffixes = new[] { " B", " KB", " MB", " GB", " TB", " PB" };

        public static string ConvertBytesToHumanReadable(double number, int precision = 2)
        {
            // unit's number of bytes
            const double unit = 1024;
            // suffix counter
            int i = 0;
            // as long as we're bigger than a unit, keep going
            while (number > unit)
            {
                number /= unit;
                i++;
            }

            // apply precision and current suffix
            return Math.Round(number, precision) + suffixes[i];
        }

        public static bool IsUnicode(string input)
        {
            var asciiBytesCount = Encoding.ASCII.GetByteCount(input);
            var unicodBytesCount = Encoding.UTF8.GetByteCount(input);
            return asciiBytesCount != unicodBytesCount;
        }

        public static EventLogReader GetEventLogReader(string path, string query, string computerName = null)
        {
            // TODO: investigate https://docs.microsoft.com/en-us/previous-versions/windows/desktop/eventlogprov/win32-ntlogevent

            var eventsQuery = new EventLogQuery(path, PathType.LogName, query) { ReverseDirection = true };

            if (!string.IsNullOrEmpty(computerName))
            {
                //EventLogSession session = new EventLogSession(
                //    ComputerName,
                //    "Domain",                                  // Domain
                //    "Username",                                // Username
                //    pw,
                //    SessionAuthentication.Default); // TODO password specification! https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.eventing.reader.eventlogsession.-ctor?view=dotnet-plat-ext-3.1#System_Diagnostics_Eventing_Reader_EventLogSession__ctor_System_String_System_String_System_String_System_Security_SecureString_System_Diagnostics_Eventing_Reader_SessionAuthentication_

                var session = new EventLogSession(computerName);
                eventsQuery.Session = session;
            }

            var logReader = new EventLogReader(eventsQuery);
            return logReader;
        }
    }
}
