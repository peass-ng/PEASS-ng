using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class DeviceDriverInventoryTests
    {
        [TestMethod]
        public void KeepsRunningVersionlessDriverAndExcludesStoppedAndMicrosoft()
        {
            var custom = new DeviceDriverRecord {
                Name = "custom", Path = @"C:\driver\custom.sys", State = "Running", StartMode = "Auto"
            };
            var stopped = new DeviceDriverRecord {
                Name = "stopped", Path = @"C:\driver\stopped.sys", State = "Stopped"
            };
            var microsoft = new DeviceDriverRecord {
                Name = "windows", Path = @"C:\Windows\System32\drivers\windows.sys",
                State = "Running", Company = "Microsoft Corporation"
            };

            DeviceDriverInventory result = DeviceDrivers.Merge(new[] { custom },
                new[] { stopped, microsoft }, 10);

            Assert.AreEqual(1, result.Drivers.Count);
            Assert.AreEqual(@"C:\driver\custom.sys", result.Drivers[0].Path);
            Assert.IsNull(result.Drivers[0].Company);
            Assert.AreEqual("Auto", result.Drivers[0].StartMode);
            Assert.IsFalse(result.UnknownOrTruncated);
        }

        [TestMethod]
        public void DeduplicatesPathsAndEnrichesLoadedDriverWithServiceMetadata()
        {
            var psapi = new DeviceDriverRecord { Path = @"C:\driver\custom.sys", State = "Running" };
            var service = new DeviceDriverRecord {
                Name = "custom", Path = @"\??\c:\DRIVER\CUSTOM.sys",
                State = "Running", StartMode = "Auto"
            };

            DeviceDriverInventory result = DeviceDrivers.Merge(new[] { psapi }, new[] { service }, 10);

            Assert.AreEqual(1, result.Drivers.Count);
            Assert.AreEqual("custom", result.Drivers[0].Name);
            Assert.AreEqual("Auto", result.Drivers[0].StartMode);
            Assert.IsNull(psapi.Name); // Merge leaves provider fixtures untouched.
        }

        [TestMethod]
        public void ReportsUnavailableProviderAndOutputCapWithoutLosingKnownRows()
        {
            var loaded = new DeviceDriverRecord { Path = @"C:\driver\loaded.sys", State = "Running" };
            DeviceDriverInventory failure = DeviceDrivers.Merge(new[] { loaded }, null, 10,
                "running driver metadata unavailable or timed out");
            Assert.AreEqual(1, failure.Drivers.Count);
            Assert.IsTrue(failure.UnknownOrTruncated);
            StringAssert.Contains(failure.Detail, "unavailable");

            var more = new DeviceDriverRecord { Path = @"C:\driver\more.sys", State = "Running" };
            DeviceDriverInventory capped = DeviceDrivers.Merge(new[] { loaded, more }, null, 1);
            Assert.AreEqual(1, capped.Drivers.Count);
            Assert.IsTrue(capped.UnknownOrTruncated);
            StringAssert.Contains(capped.Detail, "cap");
        }

        [TestMethod]
        public void DeviceNamespacePathsAreNeverFileProbed()
        {
            Assert.IsFalse(DeviceDrivers.IsLocalFilePath(@"\Device\HarddiskVolume1\custom.sys"));
            Assert.IsFalse(DeviceDrivers.IsLocalFilePath(@"\\.\Reaper"));
            Assert.IsFalse(DeviceDrivers.IsLocalFilePath(@"\\server\share\custom.sys"));
            Assert.IsTrue(DeviceDrivers.IsLocalFilePath(@"C:\driver\custom.sys"));
        }
    }
}
