using Microsoft.Win32;
using System;
using System.IO;

namespace winPEAS.Info.ServicesInfo
{
    internal sealed class VisualStudioCollectorAssessment
    {
        public bool IsReviewCandidate { get; set; }
        public bool SetupWmiCompilerPresent { get; set; }
    }

    internal static class VisualStudioCollectorIndicator
    {
        internal const string ServiceKey = @"SYSTEM\CurrentControlSet\Services\VSStandardCollectorService150";
        private const string CollectorSuffix = @"\DiagnosticsHub.Collection.Service\StandardCollector.Service.exe";

        internal static VisualStudioCollectorAssessment Collect()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(ServiceKey))
                {
                    if (key == null) return null;
                    return CollectCore(name => key.GetValue(name) as string, File.Exists,
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
                }
            }
            catch (Exception)
            {
                // Service metadata can be inaccessible. Unknown is not a negative finding.
                return null;
            }
        }

        internal static VisualStudioCollectorAssessment CollectCore(Func<string, string> readServiceValue,
            Func<string, bool> fileExists, string commonApplicationData)
        {
            if (readServiceValue == null) return null;
            VisualStudioCollectorAssessment assessment = Assess(
                readServiceValue("ObjectName"), readServiceValue("ImagePath"), false);
            if (!assessment.IsReviewCandidate || fileExists == null ||
                string.IsNullOrWhiteSpace(commonApplicationData)) return assessment;

            string compilerPath = Path.Combine(commonApplicationData,
                @"Microsoft\VisualStudio\SetupWMI\MofCompiler.exe");
            assessment.SetupWmiCompilerPresent = fileExists(compilerPath);
            return assessment;
        }

        internal static VisualStudioCollectorAssessment Assess(string account, string imagePath,
            bool compilerPresent)
        {
            bool system = string.Equals(account, "LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(account, @"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase);
            ServiceCommandLineAssessment command = ServicesInfoHelper.AssessServiceCommandLine(imagePath);
            string executable = command.ExecutablePath;
            bool expectedImage = !string.IsNullOrEmpty(executable) &&
                executable.Replace('/', '\\').IndexOf(@"\Microsoft Visual Studio\",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                executable.Replace('/', '\\').EndsWith(CollectorSuffix,
                    StringComparison.OrdinalIgnoreCase);

            bool candidate = system && expectedImage;
            return new VisualStudioCollectorAssessment
            {
                IsReviewCandidate = candidate,
                SetupWmiCompilerPresent = candidate && compilerPresent
            };
        }
    }
}
