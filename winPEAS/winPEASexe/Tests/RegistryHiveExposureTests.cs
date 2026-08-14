using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.AccessControl;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class RegistryHiveExposureTests
    {
        private static readonly HashSet<string> StandardUserSids = new HashSet<string>
        {
            "S-1-5-21-1000-1000-1000-1001",
            "S-1-5-32-545",
        };

        [TestMethod]
        public void DetectsBuiltInUsersReadAce()
        {
            var descriptor = new RawSecurityDescriptor("O:BAG:SYD:AI(A;ID;0x1200a9;;;BU)");

            string trustee = RegistryHiveExposure.FindReadTrustee(descriptor, StandardUserSids);

            Assert.AreEqual("S-1-5-32-545", trustee);
        }

        [TestMethod]
        public void IgnoresAdministratorAndSystemOnlyAcl()
        {
            var descriptor = new RawSecurityDescriptor("O:BAG:SYD:AI(A;ID;FA;;;SY)(A;ID;FA;;;BA)");

            string trustee = RegistryHiveExposure.FindReadTrustee(descriptor, StandardUserSids);

            Assert.IsNull(trustee);
        }

        [TestMethod]
        public void HonorsReadDenyBeforeAllow()
        {
            var descriptor = new RawSecurityDescriptor("O:BAG:SYD:(D;;FR;;;BU)(A;;FR;;;BU)");

            string trustee = RegistryHiveExposure.FindReadTrustee(descriptor, StandardUserSids);

            Assert.IsNull(trustee);
        }

        [TestMethod]
        public void BuildsValidatedShadowHivePath()
        {
            string path = RegistryHiveExposure.BuildShadowHivePath(
                @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy12",
                @"C:\Windows",
                "SECURITY");

            Assert.AreEqual(
                @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy12\Windows\System32\config\SECURITY",
                path);
            Assert.IsNull(RegistryHiveExposure.BuildShadowHivePath(@"\\server\share", @"C:\Windows", "SAM"));
            Assert.IsNull(RegistryHiveExposure.BuildShadowHivePath(
                @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy12",
                @"C:\Windows",
                @"..\SAM"));
        }
    }
}
