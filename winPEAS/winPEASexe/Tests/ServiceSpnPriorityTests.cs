using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class ServiceSpnPriorityTests
    {
        [TestMethod]
        public void OrdinaryAndAesOnlyServiceUsersRemainVisible()
        {
            Assert.AreEqual(0, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, false, null, false, false, false));
            Assert.AreEqual(0, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, false, 0x18, false, false, false));
            Assert.AreEqual(0, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, false, 0, false, false, false));
        }

        [TestMethod]
        public void SqlServiceSpnIsShownBeforeOtherSpns()
        {
            var ordered = ActiveDirectoryInfo.PrioritizeSpns(new[]
            {
                "HTTP/", "HOST/", "MSSQLSvc/"
            }).ToArray();
            Assert.AreEqual("MSSQLSvc/", ordered[0]);
        }

        [TestMethod]
        public void ExplicitRc4AndOtherPriorityHintsRemainSeparate()
        {
            Assert.AreEqual(2, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, false, 0x1c, false, false, false));
            Assert.AreEqual(2, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, false, 0x18, false, false, true));
            Assert.AreEqual(1, ActiveDirectoryInfo.AssessSpnPriority(0x10200, false, false, 0x18, true, false, false));
            Assert.AreEqual(1, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, false, 0x18, false, true, false));
        }

        [TestMethod]
        public void DisabledAndManagedObjectsAreExcluded()
        {
            Assert.AreEqual(-1, ActiveDirectoryInfo.AssessSpnPriority(0x202, false, false, 0x4, false, false, true));
            Assert.AreEqual(-1, ActiveDirectoryInfo.AssessSpnPriority(0x200, true, false, 0x4, false, false, true));
            Assert.AreEqual(-1, ActiveDirectoryInfo.AssessSpnPriority(0x200, false, true, 0x4, false, false, true));
            Assert.AreEqual(-1, ActiveDirectoryInfo.AssessSpnPriority(null, false, false, 0x4, false, false, true));
        }
    }
}
