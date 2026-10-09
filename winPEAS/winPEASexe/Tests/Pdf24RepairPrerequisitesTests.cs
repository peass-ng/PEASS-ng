using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class Pdf24RepairPrerequisitesTests
    {
        private static Pdf24RepairEvidence Ready(string version)
        {
            return new Pdf24RepairEvidence
            {
                RegistryVersion = version,
                BinaryVersion = version,
                MsiRegistered = true,
                ReadablePackage = true,
                InteractiveSession = true
            };
        }

        [TestMethod]
        public void RequiresVersionAndRegisteredReadablePackage()
        {
            Assert.AreEqual(Pdf24RepairAssessment.ConditionalLead,
                Pdf24RepairPrerequisites.Assess(Ready("11.15.1")));
            Pdf24RepairEvidence withoutPackage = Ready("11.15.1");
            withoutPackage.ReadablePackage = false;
            Assert.AreEqual(Pdf24RepairAssessment.MissingRepairEvidence,
                Pdf24RepairPrerequisites.Assess(withoutPackage));
            Pdf24RepairEvidence withoutRegistration = Ready("11.15.1");
            withoutRegistration.MsiRegistered = false;
            Assert.AreEqual(Pdf24RepairAssessment.MissingRepairEvidence,
                Pdf24RepairPrerequisites.Assess(withoutRegistration));
            Assert.AreEqual(Pdf24RepairAssessment.FixedVersion,
                Pdf24RepairPrerequisites.Assess(Ready("11.15.2")));
        }

        [TestMethod]
        public void SessionAndUiConditionsGateLead()
        {
            Pdf24RepairEvidence sessionZero = Ready("11.15.1");
            sessionZero.InteractiveSession = false;
            Assert.AreEqual(Pdf24RepairAssessment.MissingRepairEvidence,
                Pdf24RepairPrerequisites.Assess(sessionZero));
            Pdf24RepairEvidence hiddenRepair = Ready("11.15.1");
            hiddenRepair.RepairUiHidden = true;
            Assert.AreEqual(Pdf24RepairAssessment.MissingRepairEvidence,
                Pdf24RepairPrerequisites.Assess(hiddenRepair));
        }

        [TestMethod]
        public void MissingMalformedAndConflictingVersionsStayUnknown()
        {
            Assert.AreEqual(Pdf24RepairAssessment.UnknownVersion,
                Pdf24RepairPrerequisites.Assess(Ready(null)));
            Assert.AreEqual(Pdf24RepairAssessment.UnknownVersion,
                Pdf24RepairPrerequisites.Assess(Ready("11.15")));
            Pdf24RepairEvidence conflict = Ready("11.15.1");
            conflict.BinaryVersion = "11.15.2";
            Assert.AreEqual(Pdf24RepairAssessment.UnknownVersion,
                Pdf24RepairPrerequisites.Assess(conflict));
            Pdf24RepairEvidence binaryOnly = Ready(null);
            binaryOnly.BinaryVersion = "11.15.1.0";
            Assert.AreEqual(Pdf24RepairAssessment.ConditionalLead,
                Pdf24RepairPrerequisites.Assess(binaryOnly));
        }

        [TestMethod]
        public void MatchesProductAndPacksInstallerGuid()
        {
            Assert.IsTrue(Pdf24RepairPrerequisites.IsProduct("PDF24 Creator"));
            Assert.IsFalse(Pdf24RepairPrerequisites.IsProduct("PDF24 Toolbox"));
            Assert.IsFalse(Pdf24RepairPrerequisites.IsProduct("Other PDF24 Creator"));
            Assert.AreEqual("78563412BC9AF0DE1032547698BADCFE",
                Pdf24RepairPrerequisites.PackGuid(new System.Guid("12345678-9abc-def0-0123-456789abcdef")));
        }
    }
}
