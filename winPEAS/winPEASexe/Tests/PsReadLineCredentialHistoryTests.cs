using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class PsReadLineCredentialHistoryTests
    {
        [TestMethod]
        public void DetectsLiteralCredentialArgumentsWithoutReturningSecrets()
        {
            Assert.AreEqual("cmdkey /pass:<value>",
                FilesInfo.ClassifyPsReadLineCommand("cmdkey /add:server /user:admin /pass:Example123!"));
            Assert.AreEqual("net user <account> <password>",
                FilesInfo.ClassifyPsReadLineCommand("net user administrator NewValue123! /domain"));
            Assert.AreEqual("net user <account> <password>",
                FilesInfo.ClassifyPsReadLineCommand("net.exe user administrator NewValue123! /domain"));
            Assert.AreEqual("schtasks /rp <password>",
                FilesInfo.ClassifyPsReadLineCommand("schtasks /create /tn job /ru admin /rp NewValue123! /sc once"));
        }

        [TestMethod]
        public void IgnoresPromptedValuesVariablesAndUnrelatedCommands()
        {
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("cmdkey /add:server /user:admin /pass:"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("cmdkey /add:server /pass: /user:admin"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("net user administrator * /domain"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("net user administrator /domain"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("schtasks /create /tn job /ru admin /rp *"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("schtasks /create /tn job /ru admin /rp $pwd"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("schtasks /create /tn job /ru admin /rp \"$pwd\""));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand("# cmdkey /pass:example"));
            Assert.IsNull(FilesInfo.ClassifyPsReadLineCommand(null));
        }
    }
}
