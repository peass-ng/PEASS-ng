using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Native;
using winPEAS.TaskScheduler;

namespace winPEAS.Info.ApplicationInfo
{
    internal sealed class ScheduledAppsResult
    {
        public List<Dictionary<string, string>> Apps { get; } = new List<Dictionary<string, string>>();
        public bool LimitReached { get; set; }
        public bool WithoutAuthorLimitReached { get; set; }
    }

    internal class ApplicationInfoHelper
    {
        internal const int MaxScheduledTasksInspected = 2000;
        internal const int MaxScheduledFoldersInspected = 200;
        internal const int MaxScheduledAppsDisplayed = 150;
        internal const int MaxScheduledAppsWithoutAuthor = 30;

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

        internal static ScheduledAppsResult GetScheduledAppsNoMicrosoft()
        {
            var results = new ScheduledAppsResult();
            int tasksInspected = 0;
            int foldersInspected = 0;
            int withoutAuthor = 0;

            void ProcessTaskFolder(TaskFolder taskFolder)
            {
                if (results.LimitReached)
                    return;
                if (++foldersInspected > MaxScheduledFoldersInspected)
                {
                    results.LimitReached = true;
                    return;
                }

                try
                {
                    foreach (var runTask in taskFolder.GetTasks())
                    {
                        if (++tasksInspected > MaxScheduledTasksInspected || results.Apps.Count >= MaxScheduledAppsDisplayed)
                        {
                            results.LimitReached = true;
                            return;
                        }
                        ActOnTask(runTask);
                    }

                    foreach (var taskFolderSub in taskFolder.SubFolders)
                    {
                        if (results.LimitReached)
                            return;
                        ProcessTaskFolder(taskFolderSub);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException($"failed to browse scheduled task folder: '{taskFolder.Path}': {ex.Message}");
                }
            }

            void ActOnTask(Task t)
            {
                try
                {
                    if (t.Enabled && ShouldIncludeScheduledTask(t.Path, t.Definition.RegistrationInfo.Author))
                    {
                        string author = t.Definition.RegistrationInfo.Author;
                        if (string.IsNullOrWhiteSpace(author) && withoutAuthor >= MaxScheduledAppsWithoutAuthor)
                        {
                            results.WithoutAuthorLimitReached = true;
                            return;
                        }
                        List<string> f_trigger = new List<string>();
                        foreach (Trigger trigger in t.Definition.Triggers)
                        {
                            f_trigger.Add($"{trigger}");
                        }

                        List<string> actionPaths = new List<string>();
                        foreach (winPEAS.TaskScheduler.Action action in t.Definition.Actions)
                        {
                            if (action is winPEAS.TaskScheduler.Action.ExecAction executable && !string.IsNullOrWhiteSpace(executable.Path))
                                actionPaths.Add(Environment.ExpandEnvironmentVariables(executable.Path));
                        }

                        var principal = t.Definition.Principal;
                        results.Apps.Add(CreateScheduledApp(t.Name, author, t.Definition.RegistrationInfo.Description,
                            Environment.ExpandEnvironmentVariables($"{t.Definition.Actions}"), f_trigger,
                            actionPaths, principal.UserId, principal.GroupId, principal.LogonType, principal.RunLevel));
                        if (string.IsNullOrWhiteSpace(author))
                            withoutAuthor++;
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

        internal static bool ShouldIncludeScheduledTask(string path, string author)
        {
            return !string.IsNullOrEmpty(path) &&
                path.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0 &&
                (author == null || author.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0);
        }

        internal static string GetScheduledCredentialLead(TaskLogonType logonType)
        {
            return logonType == TaskLogonType.Password || logonType == TaskLogonType.InteractiveTokenOrPassword
                ? "Possible LSA-secret lead: password-based task logon may store a credential; recovery requires admin or SYSTEM access."
                : string.Empty;
        }

        internal static Dictionary<string, string> CreateScheduledApp(string name, string author, string description,
            string action, IEnumerable<string> triggers, IEnumerable<string> actionPaths,
            string userId, string groupId, TaskLogonType logonType, TaskRunLevel runLevel)
        {
            return new Dictionary<string, string>
            {
                { "Name", name ?? string.Empty },
                { "Author", string.IsNullOrWhiteSpace(author) ? "author unknown" : author },
                { "Description", description ?? string.Empty },
                { "Action", action ?? string.Empty },
                { "Trigger", string.Join("\n             ", triggers ?? Enumerable.Empty<string>()) },
                { "ActionPath", string.Join("\n", (actionPaths ?? Enumerable.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p))) },
                { "Principal", !string.IsNullOrWhiteSpace(groupId) ? groupId : (string.IsNullOrWhiteSpace(userId) ? "unknown" : userId) },
                { "LogonType", logonType.ToString() },
                { "RunLevel", runLevel.ToString() },
                { "CredentialLead", GetScheduledCredentialLead(logonType) },
            };
        }
    }
}
