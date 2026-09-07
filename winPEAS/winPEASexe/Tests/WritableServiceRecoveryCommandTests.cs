using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class WritableServiceRecoveryCommandTests
    {
        [TestMethod]
        public void EligibilityRequiresEnabledUnprotectedLocalSystemWin32Service()
        {
            Assert.IsTrue(ServicesInfoHelper.IsEligibleRecoveryService(0x10, 2, 0, "LocalSystem"));
            Assert.IsTrue(ServicesInfoHelper.IsEligibleRecoveryService(0x20, 3, null, string.Empty));

            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(0x1, 2, 0, "LocalSystem"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(0x10, 4, 0, "LocalSystem"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(0x10, 2, 1, "LocalSystem"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(
                0x10,
                2,
                0,
                @"NT AUTHORITY\LocalService"));
        }

        [TestMethod]
        public void ParsesQuotedExpandedAndUnquotedRecoveryExecutables()
        {
            string executable;
            string arguments;

            Assert.IsTrue(ServicesInfoHelper.TryParseRecoveryCommand(
                @"""%SystemRoot%\System32\cmd.exe"" /c C:\ProgramData\Vendor\recover.cmd",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.AreEqual(@"C:\Windows\System32\cmd.exe", executable);
            Assert.AreEqual(@"/c C:\ProgramData\Vendor\recover.cmd", arguments);

            Assert.IsTrue(ServicesInfoHelper.TryParseRecoveryCommand(
                @"C:\Program Files\Vendor\recovery.exe --quiet",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.AreEqual(@"C:\Program Files\Vendor\recovery.exe", executable);
            Assert.AreEqual("--quiet", arguments);

            Assert.IsTrue(ServicesInfoHelper.TryParseRecoveryCommand(
                @"cmd /c C:\ProgramData\Vendor\helper.exe",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.AreEqual(@"C:\Windows\System32\cmd.exe", executable);
            Assert.AreEqual(@"/c C:\ProgramData\Vendor\helper.exe", arguments);
        }

        [TestMethod]
        public void ResolvesKnownInterpreterAndReferencedScriptTargets()
        {
            var targets = ServicesInfoHelper.GetRecoveryCommandTargets(
                @"cmd.exe /c C:\ProgramData\Vendor\recover.cmd",
                @"C:\Windows");

            CollectionAssert.Contains(targets, @"C:\Windows\System32\cmd.exe");
            CollectionAssert.Contains(targets, @"C:\ProgramData\Vendor\recover.cmd");
            Assert.AreEqual(2, targets.Distinct(System.StringComparer.OrdinalIgnoreCase).Count());
        }

        [TestMethod]
        public void RejectsRemoteUnexpandedAndMalformedCommands()
        {
            string executable;
            string arguments;

            Assert.IsFalse(ServicesInfoHelper.TryParseRecoveryCommand(
                @"\\server\share\recover.exe",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.IsFalse(ServicesInfoHelper.TryParseRecoveryCommand(
                @"%UNKNOWN_RECOVERY_ROOT%\recover.exe",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.IsFalse(ServicesInfoHelper.TryParseRecoveryCommand(
                @"""C:\Program Files\Vendor\recovery.exe --quiet",
                @"C:\Windows",
                out executable,
                out arguments));
        }
    }
}
