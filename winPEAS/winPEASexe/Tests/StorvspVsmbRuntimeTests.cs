using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using winPEAS.Helpers;
using winPEAS.Info.SystemInfo;
using SystemInfoCollector = winPEAS.Info.SystemInfo.SystemInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class StorvspVsmbRuntimeTests
    {
        [TestMethod]
        public void PatchedBuildDoesNotRunSurfaceInventory()
        {
            var basicInfo = new Dictionary<string, string>
            {
                { "ProductName", "Windows 11 Pro" },
                { "Architecture", "ARM64" },
                { "CurrentBuild", "26100" },
                { "UpdateBuildRevision", "8037" },
                { "Hotfixes", "" }
            };

            StorvspVsmbReport report = StorvspVsmbCves.GetReport(basicInfo);

            Assert.AreEqual(StorvspPatchStatus.Patched, report.PatchStatus);
            Assert.IsTrue(report.SurfaceCollectionSkipped);
            Assert.AreEqual(OptionalFeatureState.Unknown, report.VirtualMachinePlatformState);
            Assert.IsFalse(report.AttackSurfaceEnabled);
        }

        [TestMethod]
        public void SlowCollectionReturnsUnknownWithoutWaitingForProvider()
        {
            using (var release = new ManualResetEventSlim(false))
            {
                try
                {
                    int value;
                    string error;
                    bool completed = CheckRunner.TryRunBounded(
                        () => { release.Wait(); return 42; },
                        TimeSpan.FromMilliseconds(100), out value, out error);

                    Assert.IsFalse(completed);
                    Assert.AreEqual(0, value);
                    Assert.AreEqual("query timed out", error);
                }
                finally
                {
                    release.Set();
                }
            }
        }

        [TestMethod]
        public void FastCollectionPreservesResult()
        {
            int value;
            string error;
            bool completed = CheckRunner.TryRunBounded(
                () => 42, TimeSpan.FromSeconds(3), out value, out error);

            Assert.IsTrue(completed);
            Assert.AreEqual(42, value);
            Assert.AreEqual("", error);
        }

        [TestMethod]
        public void WindowsElevenRegistryProductIsNormalizedWithoutChangingServerNames()
        {
            Assert.AreEqual("Windows 11 Pro", SystemInfoCollector.NormalizeProductName("Windows 10 Pro", "26100"));
            Assert.AreEqual("Windows 10 Pro", SystemInfoCollector.NormalizeProductName("Windows 10 Pro", "19045"));
            Assert.AreEqual("Windows Server 2025", SystemInfoCollector.NormalizeProductName("Windows Server 2025", "26100"));
        }
    }
}
