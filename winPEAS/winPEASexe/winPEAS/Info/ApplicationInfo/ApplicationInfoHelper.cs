using System;
using System.Collections.Generic;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Native;
using winPEAS.TaskScheduler;

namespace winPEAS.Info.ApplicationInfo
{
    internal class ApplicationInfoHelper
    {

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = User32.GetForegroundWindow();

            if (User32.GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return null;
        }

        public static List<Dictionary<string, string>> GetScheduledAppsNoMicrosoft()
        {
            var results = new List<Dictionary<string, string>>();

            void ProcessTaskFolder(TaskFolder taskFolder)
            {
                foreach (var runTask in taskFolder.GetTasks()) // browse all tasks in folder
                {
                    ActOnTask(runTask);
                }

                foreach (var taskFolderSub in taskFolder.SubFolders) // recursively browse subfolders
                {
                    ProcessTaskFolder(taskFolderSub);
                }
            }

            void ActOnTask(Task t)
            {
                try
                {
                    if (t.Enabled &&
                        !string.IsNullOrEmpty(t.Path) && !t.Path.Contains("Microsoft") &&
                        !string.IsNullOrEmpty(t.Definition.RegistrationInfo.Author) &&
                        !t.Definition.RegistrationInfo.Author.Contains("Microsoft"))
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
                catch (Exception ex)
                {
                    Beaprint.PrintException($"failed to process scheduled task: '{t.Name}': {ex.Message}");
                }
            }

            TaskFolder folder = TaskService.Instance.GetFolder("\\");

            ProcessTaskFolder(folder);

            return results;
        }
    }
}
