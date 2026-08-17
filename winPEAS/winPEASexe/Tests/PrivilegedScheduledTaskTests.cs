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
        public void ExtractsExecutableAndScriptPathsFromArguments()
        {
            List<string> paths = PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                @"-File ""C:\Program Files\Ops\rotate.ps1"" /then C:\Jobs\cleanup.cmd C:\Notes\readme.txt",
                null);

            CollectionAssert.Contains(paths, @"C:\Program Files\Ops\rotate.ps1");
            CollectionAssert.Contains(paths, @"C:\Jobs\cleanup.cmd");
            CollectionAssert.DoesNotContain(paths, @"C:\Notes\readme.txt");
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
