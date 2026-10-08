using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class VeeamBackupVersionTests
    {
        [TestMethod]
        public void RequiresPatchEvidenceAtElevenAndTwelveBoundaryBuilds()
        {
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Unknown,
                ApplicationsInfo.ClassifyVeeamBackupVersion("11.0.1.1261"));
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Candidate,
                ApplicationsInfo.ClassifyVeeamBackupVersion("11.0.1.1261 P20220302"));
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Fixed,
                ApplicationsInfo.ClassifyVeeamBackupVersion("11.0.1.1261 P20230227"));
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Fixed,
                ApplicationsInfo.ClassifyVeeamBackupVersion("11.0.1.1261 P20240304"));

            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Unknown,
                ApplicationsInfo.ClassifyVeeamBackupVersion("12.0.0.1420"));
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Candidate,
                ApplicationsInfo.ClassifyVeeamBackupVersion("12.0.0.1420 P20230222"));
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Fixed,
                ApplicationsInfo.ClassifyVeeamBackupVersion("12.0.0.1420 P20230223"));
            Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Fixed,
                ApplicationsInfo.ClassifyVeeamBackupVersion("12.0.0.1420 P20230718"));
        }

        [TestMethod]
        public void EarlierReleasesAreCandidatesAndLaterReleasesIncludeFix()
        {
            foreach (string version in new[] { "10.0.1.4854", "10.0.1.4854 P20220304", "11.0.0.837", "11.0.1.1260", "12.0.0.1402" })
            {
                Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Candidate,
                    ApplicationsInfo.ClassifyVeeamBackupVersion(version), version);
            }
            foreach (string version in new[] { "12.1.0.2131", "12.3.2.3617", "13.0.0.4967" })
            {
                Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Fixed,
                    ApplicationsInfo.ClassifyVeeamBackupVersion(version), version);
            }
        }

        [TestMethod]
        public void MissingOrUnreliableVersionDataStaysUnknown()
        {
            foreach (string version in new[] { null, "", "11.0.1", "11.0.1.1261 P", "11.0.1.1261 P20231340", "12.0.0.1420 P2023022", "12.0.0.1421", "11.0.1.1262", "12.0.1.1", "unknown", "11.0.1.1261 P20230227 P20220302" })
            {
                Assert.AreEqual(ApplicationsInfo.VeeamBackupVersionStatus.Unknown,
                    ApplicationsInfo.ClassifyVeeamBackupVersion(version), version ?? "null");
            }
        }

        [TestMethod]
        public void OnlyBackupAndReplicationUninstallEntriesAreConsidered()
        {
            Assert.IsTrue(ApplicationsInfo.IsVeeamBackupProduct("Veeam Backup & Replication"));
            Assert.IsTrue(ApplicationsInfo.IsVeeamBackupProduct("Veeam Backup & Replication 11a"));
            Assert.IsTrue(ApplicationsInfo.IsVeeamBackupProduct("Veeam Backup and Replication Server"));
            Assert.IsFalse(ApplicationsInfo.IsVeeamBackupProduct("Veeam Agent for Microsoft Windows"));
            Assert.IsFalse(ApplicationsInfo.IsVeeamBackupProduct("Veeam Service Provider Console"));
            Assert.IsFalse(ApplicationsInfo.IsVeeamBackupProduct("Veeam Backup for Microsoft 365"));
            Assert.IsFalse(ApplicationsInfo.IsVeeamBackupProduct("Other Veeam Backup & Replication"));
        }
    }
}
