using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class PrivilegedWmiEventConsumerTests
    {
        [TestMethod]
        public void SplitsQuotedAndUnquotedCommandTemplates()
        {
            WmiCommandParts quoted = PrivilegedWmiEventConsumers.SplitCommandLineTemplate(
                "\"C:\\Program Files\\Agent\\agent.exe\" --run \"C:\\Data\\job.xml\"");
            WmiCommandParts unquoted = PrivilegedWmiEventConsumers.SplitCommandLineTemplate(
                "C:\\Program Files\\Agent\\agent.exe --run");

            Assert.AreEqual(@"C:\Program Files\Agent\agent.exe", quoted.Executable);
            Assert.AreEqual("--run \"C:\\Data\\job.xml\"", quoted.Arguments);
            Assert.AreEqual(@"C:\Program Files\Agent\agent.exe", unquoted.Executable);
            Assert.AreEqual("--run", unquoted.Arguments);
        }

        [TestMethod]
        public void FindsInterpreterAndScriptTargetsWithoutReadingFiles()
        {
            List<string> targets = PrivilegedWmiEventConsumers.GetCommandLineTargetPaths(
                @"C:\Windows\System32\cscript.exe",
                "C:\\Windows\\System32\\cscript.exe //B \"C:\\ProgramData\\Vendor Agent\\event.vbs\"",
                null);

            CollectionAssert.Contains(targets, @"C:\Windows\System32\cscript.exe");
            CollectionAssert.Contains(targets, @"C:\ProgramData\Vendor Agent\event.vbs");
        }

        [TestMethod]
        public void RejectsDynamicWmiEventPlaceholdersAsFilesystemTargets()
        {
            List<string> targets = PrivilegedWmiEventConsumers.GetCommandLineTargetPaths(
                null,
                @"C:\ProgramData\Agent\%TargetEvent.ProcessName%.exe --run",
                null);

            Assert.AreEqual(0, targets.Count);
        }

        [TestMethod]
        public void NormalizesFullWmiConsumerReferences()
        {
            string normalized = PrivilegedWmiEventConsumers.NormalizeConsumerReference(
                "\\\\HOST\\root\\subscription:CommandLineEventConsumer.Name=\"Updater\"");

            Assert.AreEqual("CommandLineEventConsumer.Name=\"Updater\"", normalized);
        }
    }
}
