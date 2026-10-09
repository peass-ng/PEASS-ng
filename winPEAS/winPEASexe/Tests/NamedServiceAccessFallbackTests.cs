using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class NamedServiceAccessFallbackTests
    {
        [TestMethod]
        public void OnlyChangeConfigCandidatesAreReportedWithExactStartStopRights()
        {
            var probes = new List<string>();
            ServiceAccessFallbackReport report = ServicesInfoHelper.ProbeServiceAccessByName(
                new[] { "BackupService", "ReadOnlyService" },
                (name, right) =>
                {
                    probes.Add(name + ":" + right);
                    return name == "BackupService" &&
                           (right == ServicesInfoHelper.ServiceChangeConfigAccess ||
                            right == ServicesInfoHelper.ServiceStartAccess);
                },
                4,
                () => false);

            Assert.IsTrue(report.Complete);
            Assert.AreEqual(2, report.Inspected);
            Assert.AreEqual("ChangeConfig, Start", report.Findings["BackupService"]);
            Assert.IsFalse(report.Findings.ContainsKey("ReadOnlyService"));
            Assert.IsFalse(probes.Any(p => p.StartsWith("ReadOnlyService:") &&
                !p.EndsWith(ServicesInfoHelper.ServiceChangeConfigAccess.ToString())));
        }

        [TestMethod]
        public void EntryCapAndDeadlineDeclarePartialVisibility()
        {
            var visited = new List<string>();
            ServiceAccessFallbackReport capped = ServicesInfoHelper.ProbeServiceAccessByName(
                new[] { "First", "Second", "Third" },
                (name, right) => { visited.Add(name); return false; },
                2,
                () => false);

            Assert.AreEqual(2, capped.Inspected);
            Assert.IsTrue(capped.LimitReached);
            Assert.IsFalse(capped.Complete);
            Assert.IsFalse(visited.Contains("Third"));

            int calls = 0;
            ServiceAccessFallbackReport timed = ServicesInfoHelper.ProbeServiceAccessByName(
                new[] { "First", "Second" },
                (name, right) => false,
                3,
                () => calls++ > 0);
            Assert.AreEqual(1, timed.Inspected);
            Assert.IsTrue(timed.TimeLimitReached);
            Assert.IsFalse(timed.Complete);
        }

        [TestMethod]
        public void MissingInputsDoNotReportCompleteServiceCoverage()
        {
            ServiceAccessFallbackReport report = ServicesInfoHelper.ProbeServiceAccessByName(
                null,
                (name, right) => true,
                2,
                () => false);
            Assert.IsFalse(report.Available);
            Assert.IsFalse(report.Complete);
            Assert.AreEqual(0, report.Findings.Count);
        }
    }
}
