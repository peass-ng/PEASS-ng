using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ActiveDirectoryInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class LapsPasswordExposureTests
    {
        [TestMethod]
        public void DetectsLegacyAndWindowsLapsCleartextAttributes()
        {
            LapsPasswordExposureReport legacy = LapsPasswordExposure.Evaluate(true, false);
            LapsPasswordExposureReport windows = LapsPasswordExposure.Evaluate(false, true);
            LapsPasswordExposureReport both = LapsPasswordExposure.Evaluate(true, true);

            Assert.IsTrue(legacy.IsExposed);
            Assert.IsTrue(legacy.LegacyPasswordReadable);
            Assert.IsFalse(legacy.WindowsLapsPasswordReadable);

            Assert.IsTrue(windows.IsExposed);
            Assert.IsFalse(windows.LegacyPasswordReadable);
            Assert.IsTrue(windows.WindowsLapsPasswordReadable);

            Assert.IsTrue(both.LegacyPasswordReadable);
            Assert.IsTrue(both.WindowsLapsPasswordReadable);
        }

        [TestMethod]
        public void TreatsMissingAttributesAsNotExposed()
        {
            Assert.IsFalse(LapsPasswordExposure.Evaluate(false, false).IsExposed);
        }

        [TestMethod]
        public void EscapesAllLdapFilterMetacharacters()
        {
            Assert.AreEqual(
                @"host\2a\28name\29\5c\00$",
                LapsPasswordExposure.EscapeLdapFilterValue("host*(name)\\\0$"));
        }
    }
}
