using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Helpers.Search;

namespace winPEAS.Tests
{
    [TestClass]
    public class WimBackupPathTests
    {
        [TestMethod]
        public void BackupNamesAndDirectoriesAreReviewCandidates()
        {
            Assert.IsTrue(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Backups\workstation.wim"));
            Assert.IsTrue(SearchHelper.IsBackupWimPath(@"D:\Images\host-02.WIM"));
            Assert.IsTrue(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Documents\weekly_snapshot.wim"));
            Assert.IsTrue(SearchHelper.IsBackupWimPath("C:/Users/Alice/Backup/host.wim"));
        }

        [TestMethod]
        public void InstallationImagesAndUnrelatedPathsAreExcluded()
        {
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Backups\install.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Images\boot.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Images\winre.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"C:\Windows\Temp\system-backup.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Downloads\ordinary.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"C:\Users\Alice\Backups\note.wim.txt"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"\\server\share\Backups\host.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(@"relative\Backups\host.wim"));
            Assert.IsFalse(SearchHelper.IsBackupWimPath(null));
        }
    }
}
