using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class AIStorageAuditTests
    {
        [TestMethod]
        public void ReparseAncestorStopsInspectionBeforeRedirectedChildren()
        {
            string profile = @"C:\Users\example";
            string path = Path.Combine(profile, ".gemini", "antigravity-cli");
            bool checkedChild = false;
            bool result = AIStorageAudit.HasReparsePoint(path, candidate =>
            {
                if (candidate == path || candidate == Path.Combine(profile, ".gemini"))
                    checkedChild = true;
                return candidate == profile ? FileAttributes.ReparsePoint : FileAttributes.Directory;
            });
            Assert.IsTrue(result);
            Assert.IsFalse(checkedChild);
        }

        [TestMethod]
        public void BroadReadAndWriteAllowsRequireReview()
        {
            var security = new DirectorySecurity();
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.ReadData, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                FileSystemRights.WriteData, AccessControlType.Allow));
            Assert.AreEqual(2, AIStorageAudit.GetBroadAccessRules(security).Count);
        }

        [TestMethod]
        public void DeniesAdministratorsAndInheritOnlyRulesAreNotBroadRootAllows()
        {
            var security = new DirectorySecurity();
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.ReadData, AccessControlType.Deny));
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                FileSystemRights.ReadData, InheritanceFlags.ContainerInherit,
                PropagationFlags.InheritOnly, AccessControlType.Allow));
            Assert.AreEqual(0, AIStorageAudit.GetBroadAccessRules(security).Count);
        }

        [TestMethod]
        public void DirectoryCheckDoesNotReadStoredFiles()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                using (var file = new FileStream(Path.Combine(path, "private-data"),
                    FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.IsFalse(AIStorageAudit.HasReparsePoint(path));
                    AIStorageAudit.GetBroadAccessRules(Directory.GetAccessControl(path));
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}
