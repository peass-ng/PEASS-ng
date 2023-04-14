using System;
using System.Diagnostics;
using System.Text;

namespace winPEAS.Info.FilesInfo.WSL
{
    public class WSL
    {
        public static void RunLinpeas(string linpeasUrl)
        {
            string linpeasCmd = $"curl -L {linpeasUrl} --silent | sh";
            string command = Environment.Is64BitProcess ?
                                $@"bash -c ""{linpeasCmd}""" :
                                Environment.GetEnvironmentVariable("WinDir") + $"\\SysNative\\bash.exe -c \"{linpeasCmd}\"";

            ExecuteCommand(command);
        }

        private static void ExecuteCommand(string command,
            string workingFolder = null,
            string verb = "OPEN")
        {
            string executable = command;
            string args = null;

            if (executable.StartsWith("\""))
            {
                int at = executable.IndexOf("\" ");
                if (at > 0)
                {
                    args = executable.Substring(at + 1).Trim();
                    executable = executable.Substring(0, at);
                }
            }
            else
            {
                int at = executable.IndexOf(" ");
                if (at > 0)
                {
                    if (executable.Length > at + 1)
                    {
                        args = executable.Substring(at + 1).Trim();
                    }
                    executable = executable.Substring(0, at);
                }
            }

            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                Verb = verb,
                CreateNoWindow = true,
                FileName = executable,
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
