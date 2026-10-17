using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.SystemInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class UaspstorCve68839Tests
    {
        [TestMethod]
        public void RunningBuildTakesPrecedenceOverUnverifiedFileVersion()
        {
            var report = new UaspstorCve68839Report
            {
                Build = 26100,
                DriverRevision = 9999
            };

            UaspstorCve68839.AssessPatchStatus(report, 9445, true, 9000);

            Assert.AreEqual(UaspstorPatchStatus.Susceptible, report.PatchStatus);
        }

        [TestMethod]
        public void Server2012DoesNotTrustUbrOrUnverifiedDriverVersion()
        {
            var report = new UaspstorCve68839Report
            {
                Build = 9200,
                DriverRevision = 26349
            };

            UaspstorCve68839.AssessPatchStatus(report, 26349, true, 30000);

            Assert.AreEqual(UaspstorPatchStatus.Unknown, report.PatchStatus);
            Assert.IsTrue(report.PatchEvidence.Contains("signature could not be verified"));

            report.DriverSignatureValid = true;
            UaspstorCve68839.AssessPatchStatus(report, 26349, true, 30000);
            Assert.AreEqual(UaspstorPatchStatus.Patched, report.PatchStatus);
        }

        [TestMethod]
        public void KnownFixedRunningBuildIsPatched()
        {
            var report = new UaspstorCve68839Report { Build = 26100 };

            UaspstorCve68839.AssessPatchStatus(report, 9445, true, 9445);

            Assert.AreEqual(UaspstorPatchStatus.Patched, report.PatchStatus);
        }
    }
}
