using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.AccessControl;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class PrivilegedScheduledTaskTests
    {
        private static readonly HashSet<string> StandardUserSids = new HashSet<string>
        {
            "S-1-5-32-545",
        };

        [TestMethod]
        public void RecognizesOnlyLocalSystemPrincipalForms()
        {
            Assert.IsTrue(PrivilegedScheduledTasks.IsLocalSystemPrincipal("S-1-5-18"));
            Assert.IsTrue(PrivilegedScheduledTasks.IsLocalSystemPrincipal("SYSTEM"));
            Assert.IsTrue(PrivilegedScheduledTasks.IsLocalSystemPrincipal(@"NT AUTHORITY\SYSTEM"));
            Assert.IsTrue(PrivilegedScheduledTasks.IsLocalSystemPrincipal("LocalSystem"));
            Assert.IsFalse(PrivilegedScheduledTasks.IsLocalSystemPrincipal(@"BUILTIN\Administrators"));
            Assert.IsFalse(PrivilegedScheduledTasks.IsLocalSystemPrincipal("S-1-5-19"));
        }

        [TestMethod]
        public void ExtractsPowerShellFileArgument()
        {
            List<string> paths = PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                @"-NoProfile -File ""C:\Program Files\Ops\rotate.ps1""",
                null);

            CollectionAssert.Contains(paths, @"C:\Program Files\Ops\rotate.ps1");
        }

        [TestMethod]
        public void IgnoresDataAndOutputArgumentsForOrdinaryExecutables()
        {
            List<string> paths = PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                @"C:\Tools\processor.exe",
                @"/copy C:\Data\payload.dll --output C:\Reports\result.jar",
                null);

            Assert.AreEqual(0, paths.Count);
        }

        [TestMethod]
        public void IgnoresInlineInterpreterCodeThatLooksLikeAPath()
        {
            List<string> paths = PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                @"C:\Python311\python.exe",
                @"-c ""C:\Reports\not-a-script.py""",
                null);

            Assert.AreEqual(0, paths.Count);
        }

        [TestMethod]
        public void ExtractsCmdAndJavaExecutionTargetsOnlyFromKnownSwitches()
        {
            List<string> cmdPaths = PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                @"C:\Windows\System32\cmd.exe",
                @"/c call C:\Jobs\cleanup.cmd --output C:\Reports\result.jar",
                null);
            List<string> javaPaths = PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                @"C:\Program Files\Java\bin\java.exe",
                @"--class-path C:\Libraries\support.jar -jar C:\Apps\worker.jar",
                null);

            CollectionAssert.AreEqual(new List<string> { @"C:\Jobs\cleanup.cmd" }, cmdPaths);
            CollectionAssert.AreEqual(new List<string> { @"C:\Apps\worker.jar" }, javaPaths);
        }

        [TestMethod]
        public void RejectsNetworkDriveTypes()
        {
            Assert.IsTrue(PrivilegedScheduledTasks.IsAllowedDriveType(System.IO.DriveType.Fixed));
            Assert.IsFalse(PrivilegedScheduledTasks.IsAllowedDriveType(System.IO.DriveType.Network));
            Assert.IsFalse(PrivilegedScheduledTasks.IsAllowedDriveType(System.IO.DriveType.Unknown));
        }

        [TestMethod]
        public void EnforcesGlobalTargetAndTimeBudgets()
        {
            var targetLimited = new PrivilegedScheduledTaskReport
            {
                TargetsInspected = PrivilegedScheduledTasks.MaxTargets,
            };
            var timeLimited = new PrivilegedScheduledTaskReport();

            Assert.IsTrue(PrivilegedScheduledTasks.ApplySafetyLimits(targetLimited, 0));
            Assert.IsTrue(targetLimited.TargetLimitReached);
            Assert.IsTrue(PrivilegedScheduledTasks.ApplySafetyLimits(
                timeLimited,
                PrivilegedScheduledTasks.MaxInspectionMilliseconds));
            Assert.IsTrue(timeLimited.TimeLimitReached);
        }

        [TestMethod]
        public void HonorsWriteDenyBeforeAllow()
        {
            var allowed = new RawSecurityDescriptor("O:BAG:SYD:(A;;0x2;;;BU)");
            var denied = new RawSecurityDescriptor("O:BAG:SYD:(D;;0x2;;;BU)(A;;0x2;;;BU)");

            Assert.AreEqual(
                "S-1-5-32-545",
                PrivilegedScheduledTasks.FindWriteTrustee(allowed, StandardUserSids, 0x2));
            Assert.IsNull(PrivilegedScheduledTasks.FindWriteTrustee(denied, StandardUserSids, 0x2));
        }

        [TestMethod]
        public void RequiresCreateAndDeleteChildForExistingFileReplacement()
        {
            var createOnly = new RawSecurityDescriptor("O:BAG:SYD:(A;;0x2;;;BU)");
            var replace = new RawSecurityDescriptor("O:BAG:SYD:(A;;0x42;;;BU)");

            Assert.IsNull(PrivilegedScheduledTasks.FindDirectoryReplacementTrustee(createOnly, StandardUserSids));
            Assert.AreEqual(
                "S-1-5-32-545",
                PrivilegedScheduledTasks.FindDirectoryReplacementTrustee(replace, StandardUserSids));
        }
    }
}
