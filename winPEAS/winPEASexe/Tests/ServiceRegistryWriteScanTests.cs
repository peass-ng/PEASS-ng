using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class ServiceRegistryWriteScanTests
    {
        [TestMethod]
        public void UnreadableFirstKeyDoesNotHideLaterCandidate()
        {
            string[] names = { "unreadable", "later", "ordinary" };
            ServiceRegistryWriteReport report = ServicesInfoHelper.InspectWriteServiceRegs(
                names,
                name =>
                {
                    if (name == "unreadable") throw new UnauthorizedAccessException();
                    return name == "later" ? new List<string> { "Users [Allow: WriteKey]" } : new List<string>();
                },
                10,
                () => false);

            Assert.AreEqual(3, report.TotalNames);
            Assert.AreEqual(3, report.Inspected);
            Assert.AreEqual(1, report.Unreadable);
            Assert.AreEqual(1, report.Findings.Count);
            Assert.AreEqual(@"HKLM\system\currentcontrolset\services\later", report.Findings[0]["Path"]);
            Assert.IsFalse(report.Complete);
            StringAssert.Contains(report.NoFindingsSummary, "incomplete");
        }

        [TestMethod]
        public void CompleteEmptyInventoryHasCarefulNoFindingSummary()
        {
            ServiceRegistryWriteReport report = ServicesInfoHelper.InspectWriteServiceRegs(
                new[] { "first", "second" }, name => new List<string>(), 10, () => false);

            Assert.IsTrue(report.Complete);
            Assert.AreEqual(2, report.Inspected);
            StringAssert.Contains(report.NoFindingsSummary, "No matching service-registry ACL entries");
        }

        [TestMethod]
        public void EntryCapAndDeadlineLeaveCoveragePartial()
        {
            string[] names = { "first", "second", "third" };
            ServiceRegistryWriteReport capped = ServicesInfoHelper.InspectWriteServiceRegs(
                names, name => new List<string>(), 2, () => false);
            Assert.AreEqual(2, capped.Inspected);
            Assert.IsTrue(capped.LimitReached);
            Assert.IsFalse(capped.Complete);

            int clockCalls = 0;
            ServiceRegistryWriteReport timed = ServicesInfoHelper.InspectWriteServiceRegs(
                names, name => new List<string>(), 10, () => ++clockCalls > 1);
            Assert.AreEqual(1, timed.Inspected);
            Assert.IsTrue(timed.TimeLimitReached);
            Assert.IsFalse(timed.Complete);
        }

        [TestMethod]
        public void UnavailableInventoryIsUnknown()
        {
            ServiceRegistryWriteReport report = ServicesInfoHelper.InspectWriteServiceRegs(
                null, name => new List<string>(), 10, () => false);
            Assert.IsFalse(report.Available);
            Assert.IsFalse(report.Complete);
            StringAssert.Contains(report.NoFindingsSummary, "incomplete");
        }

        [TestMethod]
        public void CandidateOutputIsCappedWhileInventoryStillFinishes()
        {
            var names = new string[ServicesInfoHelper.MaxServiceRegistryWriteFindings + 2];
            for (int i = 0; i < names.Length; ++i)
                names[i] = "service" + i;

            ServiceRegistryWriteReport report = ServicesInfoHelper.InspectWriteServiceRegs(
                names, name => new List<string> { "Users [Deny: WriteKey]" }, names.Length, () => false);

            Assert.IsTrue(report.Complete);
            Assert.AreEqual(names.Length, report.Inspected);
            Assert.AreEqual(ServicesInfoHelper.MaxServiceRegistryWriteFindings, report.Findings.Count);
            Assert.AreEqual(2, report.OmittedFindings);
        }
    }
}
