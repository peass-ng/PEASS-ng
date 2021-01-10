using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using winPEAS.Helpers;
using winPEAS.TaskScheduler;

namespace winPEAS.Info.ApplicationInfo
{
    internal class ApplicationInfoHelper
    {
        // https://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return null;
        }

        public static List<Dictionary<string, string>> GetScheduledAppsNoMicrosoft()
        {
            var results = new List<Dictionary<string, string>>();
            
            try
            {
                void EnumFolderTasks(TaskFolder fld)
                {
                    foreach (Task task in fld.Tasks)
                    {
                        ActOnTask(task);
                    }
                    //task.Name
                    //task.Enabled
                    //task.Definition.Actions
                    //task.Definition
                    foreach (TaskFolder sfld in fld.SubFolders)
                    {
                        EnumFolderTasks(sfld);
                    }
                }

                void ActOnTask(Task t)
                {
                    if (t.Enabled && (!string.IsNullOrEmpty(t.Definition.RegistrationInfo.Author) && !t.Definition.RegistrationInfo.Author.Contains("Microsoft")))
                    {
                        List<string> f_trigger = new List<string>();
                        foreach (Trigger trigger in t.Definition.Triggers)
                        {
                            f_trigger.Add($"{trigger}");
                        }

                        results.Add(new Dictionary<string, string>
                        {
                            { "Name", t.Name },
                            { "Action", Environment.ExpandEnvironmentVariables($"{t.Definition.Actions}") },
                            { "Trigger", string.Join("\n             ", f_trigger) },
                            { "Author", string.Join(", ", t.Definition.RegistrationInfo.Author) },
                            { "Description", string.Join(", ", t.Definition.RegistrationInfo.Description) },
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
