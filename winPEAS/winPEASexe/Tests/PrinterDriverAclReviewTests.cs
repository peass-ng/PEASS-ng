using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.SystemInfo.Printers;

namespace winPEAS.Tests
{
    [TestClass]
    public class PrinterDriverAclReviewTests
    {
        private const string CandidateRights = "Users [Allow: WriteData]";

        [TestMethod]
        public void ExactDllAclKeepsDotsInDriverDirectory()
        {
            const string path = @"C:\ProgramData\RICOH_DRV\driver.1\_common\dlz\support.dll";
            string opened = null;
            var security = new FileSecurity();
            string rights = Printers.GetExactFileAclCandidate(path,
                candidate => { opened = candidate; return security; },
                descriptor => descriptor == security ? new[] { CandidateRights } : new string[0]);

            Assert.AreEqual(path, opened);
            Assert.AreEqual(CandidateRights, rights);
            Assert.IsNull(Printers.GetExactFileAclCandidate(path,
                candidate => { throw new UnauthorizedAccessException(); },
                descriptor => new[] { CandidateRights }));
        }

        [TestMethod]
        public void ExactSupportPathReportsOnlyAclMetadata()
        {
            string programData = Path.Combine(Path.GetTempPath(), "program-data");
            string root = Path.Combine(programData, "RICOH_DRV");
            string driver = Path.Combine(root, "driver-a");
            string common = Path.Combine(driver, "_common");
            string dlz = Path.Combine(common, "dlz");
            string dll = Path.Combine(dlz, "support.dll");
            string unrelated = Path.Combine(driver, "other.dll");
            int fileRightsCalls = 0;

            var report = Printers.ReviewDriverAclPaths(programData,
                path => path == root ? new[] { driver } : new string[0],
                path => path == dlz ? new[] { dll, unrelated } : new string[0],
                path => path == root || path == common || path == dlz,
                path => false,
                path => path == dlz ? CandidateRights : null,
                path => { fileRightsCalls++; return path == dll ? CandidateRights : null; },
                () => 0);

            Assert.IsTrue(report.RootPresent);
            Assert.IsFalse(report.Partial);
            Assert.AreEqual(1, report.DriversInspected);
            Assert.AreEqual(1, report.DllsInspected);
            Assert.AreEqual(1, fileRightsCalls);
            Assert.AreEqual(2, report.Findings.Count);
            Assert.AreEqual(dlz, report.Findings[0].Path);
            Assert.AreEqual(dll, report.Findings[1].Path);
            Assert.AreEqual(CandidateRights, report.Findings[1].Rights);
        }

        [TestMethod]
        public void MissingOrReparseAncestorCannotProduceCandidate()
        {
            string programData = Path.Combine(Path.GetTempPath(), "program-data");
            string root = Path.Combine(programData, "RICOH_DRV");
            string driver = Path.Combine(root, "driver-a");
            string common = Path.Combine(driver, "_common");
            string dlz = Path.Combine(common, "dlz");
            Func<string, IEnumerable<string>> dirs = path => path == root ? new[] { driver } : new string[0];
            Func<string, IEnumerable<string>> dlls = path => new[] { Path.Combine(dlz, "support.dll") };

            foreach (string blocked in new[] { programData, root, driver, common, dlz, Path.Combine(dlz, "support.dll") })
            {
                var report = Printers.ReviewDriverAclPaths(programData, dirs, dlls,
                    path => path == root || path == common || path == dlz,
                    path => path == blocked,
                    path => CandidateRights,
                    path => CandidateRights,
                    () => 0);
                Assert.AreEqual(0, report.Findings.Count, blocked);
            }

            var missing = Printers.ReviewDriverAclPaths(programData, dirs, dlls,
                path => false, path => false, path => CandidateRights,
                path => CandidateRights, () => 0);
            Assert.IsFalse(missing.RootPresent);
        }

        [TestMethod]
        public void DriverAndTimeLimitsMarkPartialWithoutUnboundedAclReads()
        {
            string programData = Path.Combine(Path.GetTempPath(), "program-data");
            string root = Path.Combine(programData, "RICOH_DRV");
            var drivers = new List<string>();
            for (int i = 0; i < Printers.MaxDriverFolders + 1; i++)
                drivers.Add(Path.Combine(root, "driver-" + i));
            int directoryRightsCalls = 0;
            var capped = Printers.ReviewDriverAclPaths(programData,
                path => drivers,
                path => new string[0],
                path => true,
                path => false,
                path => { directoryRightsCalls++; return null; },
                path => null,
                () => 0);
            Assert.IsTrue(capped.Partial);
            Assert.AreEqual(Printers.MaxDriverFolders, capped.DriversInspected);
            Assert.IsTrue(directoryRightsCalls <= Printers.MaxDriverFolders * 4);

            var timed = Printers.ReviewDriverAclPaths(programData,
                path => drivers,
                path => new string[0],
                path => true,
                path => false,
                path => CandidateRights,
                path => CandidateRights,
                () => Printers.MaxAclMilliseconds);
            Assert.IsTrue(timed.Partial);
            Assert.AreEqual(0, timed.Findings.Count);
        }

        [TestMethod]
        public void InaccessibleDirectoryIsReportedAsPartialWithoutAFalseFinding()
        {
            string programData = Path.Combine(Path.GetTempPath(), "program-data");
            var report = Printers.ReviewDriverAclPaths(programData,
                path => { throw new UnauthorizedAccessException(); },
                path => new string[0],
                path => true,
                path => false,
                path => CandidateRights,
                path => CandidateRights,
                () => 0);
            Assert.IsTrue(report.RootPresent);
            Assert.IsTrue(report.Partial);
            Assert.AreEqual(0, report.Findings.Count);
        }
    }
}
