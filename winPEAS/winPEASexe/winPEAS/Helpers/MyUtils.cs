using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace winPEAS.Helpers
{
    public class MyUtils
    {                     
        public static string GetCLSIDBinPath(string CLSID)
        {
            return RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Classes\CLSID\" + CLSID + @"\InprocServer32", ""); //To get the default object you need to use an empty string
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
            { 
                binaryPath = match_path.Groups[1].ToString();
            }

            if (binaryPath.Contains('"'))
            {
                binaryPath = binaryPath.Split('"')[0];
                binaryPath = binaryPath.Trim();
            }

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
