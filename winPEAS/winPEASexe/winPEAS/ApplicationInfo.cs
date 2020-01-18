using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace winPEAS
{
    class ApplicationInfo
    {
        // https://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public static List<string> GetAppsRegistry()
        {
            List<string> retList = new List<string>();
            try
            {
                RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
                foreach (string subKeyName in softwareKey.GetSubKeyNames())
                {
                    retList.Add(subKeyName);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: "+ex);
            }
            return retList;
        }

        public static Dictionary<string, Dictionary<string, string>> GetInstalledAppsPermsPath(string fpath)
        {
            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                foreach (string f in Directory.GetFiles(fpath))
                {
                    results[f] = new Dictionary<string, string>(){
                            { f, String.Join(", ", MyUtils.GetPermissionsFile(f, Program.interestingUsersGroups)) }
                        };
                }
                foreach (string d in Directory.GetDirectories(fpath))
                {
                    results[d] = MyUtils.GecRecursivePrivs(d);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

        public static Dictionary<string, Dictionary<string, string>> GetInstalledAppsPerms()
        {
            Dictionary<string, Dictionary<string, string>> results1 = GetInstalledAppsPermsPath(@Path.GetPathRoot(Environment.SystemDirectory) + "Program Files");
            Dictionary<string, Dictionary<string, string>> results2 = GetInstalledAppsPermsPath(@Path.GetPathRoot(Environment.SystemDirectory) + "Program Files (x86)");
            results1.Concat(results2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return results1;
        }

        public static List<Dictionary<string, string>> GetAutoRunsFolder()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            List<string> autorunLocations = new List<string>();
            autorunLocations.Add(Environment.ExpandEnvironmentVariables(@"%appdata%\Microsoft\Windows\Start Menu\Programs\Startup"));
            autorunLocations.Add(Environment.ExpandEnvironmentVariables(@"%programdata%\Microsoft\Windows\Start Menu\Programs\Startup"));

            foreach (string path in autorunLocations)
            {
                foreach (string filepath in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
                {
                    string folder = Path.GetDirectoryName(filepath);
                    results.Add(new Dictionary<string, string>() {
                            { "Reg", "" },
                            { "Folder", folder },
                            { "File", filepath },
                            { "isWritableReg", ""},
                            { "interestingFolderRights", String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.interestingUsersGroups))},
                            { "interestingFileRights", String.Join(", ", MyUtils.GetPermissionsFile(filepath, Program.interestingUsersGroups))},
                            { "isUnquotedSpaced", "" }
                    });
                }
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetAutoRuns()
        {
            List<Dictionary<string, string>> reg_autorus = ServicesInfo.GetRegistryAutoRuns();
            List<Dictionary<string, string>> file_autorus = GetAutoRunsFolder();
            reg_autorus.AddRange(file_autorus);
            return reg_autorus;
        }

        public static List<Dictionary<string, string>> GetScheduledAppsNoMicrosoft()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                void EnumFolderTasks(TaskFolder fld)
                {
                    foreach (Microsoft.Win32.TaskScheduler.Task task in fld.Tasks)
                        ActOnTask(task);
                    //task.Name
                    //task.Enabled
                    //task.Definition.Actions
                    //task.Definition
                    foreach (TaskFolder sfld in fld.SubFolders)
                        EnumFolderTasks(sfld);
                }

                void ActOnTask(Microsoft.Win32.TaskScheduler.Task t)
                {
                    if (t.Enabled && (!String.IsNullOrEmpty(t.Definition.RegistrationInfo.Author) && !t.Definition.RegistrationInfo.Author.Contains("Microsoft")))
                    {
                        List<string> f_trigger = new List<string>();
                        foreach (Trigger trigger in t.Definition.Triggers)
                            f_trigger.Add(String.Format("{0}", trigger));

                        results.Add(new Dictionary<string, string>()
                    {
                        { "Name", t.Name },
                        { "Action", Environment.ExpandEnvironmentVariables(String.Format("{0}", t.Definition.Actions)) },
                        { "Trigger", String.Join("\n             ", f_trigger) },
                        { "Author", String.Join(", ", t.Definition.RegistrationInfo.Author) },
                        { "Description", String.Join(", ", t.Definition.RegistrationInfo.Description) },
                    });
                    }
                }
                EnumFolderTasks(TaskService.Instance.RootFolder);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

    }
}
