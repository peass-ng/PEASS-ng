using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class WritableServiceDllTests
    {
        private const string UsersSid = "S-1-5-32-545";

        [TestMethod]
        public void NormalizesServicePathsAndRequiresUnprotectedSystemSvchost()
        {
            Assert.AreEqual(
                @"C:\Windows\System32\example.dll",
                ServicesInfoHelper.NormalizeServiceDllPath(
                    @" ""%SystemRoot%\System32\example.dll"" ",
                    @"C:\Windows"));
            Assert.AreEqual(
                @"C:\Windows\System32\example.dll",
                ServicesInfoHelper.NormalizeServiceDllPath(
                    @"\??\C:\Windows\System32\example.dll",
                    @"C:\Windows"));
            Assert.AreEqual(
                @"C:\Windows\Sysnative\example.dll",
                ServicesInfoHelper.GetFileSystemAccessPath(
                    @"C:\Windows\System32\example.dll",
                    @"C:\Windows",
                    true,
                    false));

            Assert.IsTrue(ServicesInfoHelper.IsEligibleSystemService(
                0x20,
                2,
                0,
                "LocalSystem",
                @"""\SystemRoot\System32\svchost.exe"" -k netsvcs -p",
                @"C:\Windows"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleSystemService(
                0x10,
                2,
                0,
                "LocalSystem",
                @"%SystemRoot%\System32\svchost.exe -k netsvcs -p",
                @"C:\Windows"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleSystemService(
                0x20,
                4,
                0,
                "LocalSystem",
                @"%SystemRoot%\System32\svchost.exe -k netsvcs -p",
                @"C:\Windows"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleSystemService(
                0x20,
                2,
                2,
                "LocalSystem",
                @"%SystemRoot%\System32\svchost.exe -k netsvcs -p",
                @"C:\Windows"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleSystemService(
                0x20,
                2,
                0,
                @"NT AUTHORITY\LocalService",
                @"%SystemRoot%\System32\svchost.exe -k LocalService",
                @"C:\Windows"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleSystemService(
                0x20,
                2,
                0,
                "LocalSystem",
                @"C:\Users\Public\svchost.exe -k fake",
                @"C:\Windows"));
        }

        [TestMethod]
        public void EffectiveAccessHonorsDenyAceOrder()
        {
            var tokenSids = new HashSet<string> { UsersSid };
            RawSecurityDescriptor allowed = CreateDescriptor(
                new AceQualifier[] { AceQualifier.AccessAllowed },
                new int[] { 0x00000002 });
            Assert.IsTrue(ServicesInfoHelper.HasEffectiveAccess(allowed, tokenSids, 0x00000002));
            Assert.IsFalse(ServicesInfoHelper.HasEffectiveAccess(allowed, tokenSids, 0x00010000));

            RawSecurityDescriptor deniedThenAllowed = CreateDescriptor(
                new AceQualifier[] { AceQualifier.AccessDenied, AceQualifier.AccessAllowed },
                new int[] { 0x00000002, 0x10000000 });
            Assert.IsFalse(ServicesInfoHelper.HasEffectiveAccess(deniedThenAllowed, tokenSids, 0x00000002));
            Assert.IsTrue(ServicesInfoHelper.HasEffectiveAccess(deniedThenAllowed, tokenSids, 0x00010000));
        }

        [TestMethod]
        public void RequiresConcretePlantOrReplacementRights()
        {
            var tokenSids = new HashSet<string> { UsersSid };
            RawSecurityDescriptor readOnlyFile = CreateDescriptor(
                new AceQualifier[] { AceQualifier.AccessAllowed },
                new int[] { 0x00000001 });
            RawSecurityDescriptor createOnlyDirectory = CreateDescriptor(
                new AceQualifier[] { AceQualifier.AccessAllowed },
                new int[] { 0x00000002 });
            RawSecurityDescriptor replaceDirectory = CreateDescriptor(
                new AceQualifier[] { AceQualifier.AccessAllowed },
                new int[] { 0x00000042 });

            Assert.IsFalse(string.IsNullOrEmpty(ServicesInfoHelper.GetReplacementReason(
                null,
                createOnlyDirectory,
                false,
                tokenSids)));
            Assert.AreEqual(string.Empty, ServicesInfoHelper.GetReplacementReason(
                readOnlyFile,
                createOnlyDirectory,
                true,
                tokenSids));
            Assert.IsFalse(string.IsNullOrEmpty(ServicesInfoHelper.GetReplacementReason(
                readOnlyFile,
                replaceDirectory,
                true,
                tokenSids)));
        }

        private static RawSecurityDescriptor CreateDescriptor(
            AceQualifier[] qualifiers,
            int[] accessMasks)
        {
            var dacl = new RawAcl(2, qualifiers.Length);
            var users = new SecurityIdentifier(UsersSid);
            for (int index = 0; index < qualifiers.Length; index++)
            {
                dacl.InsertAce(index, new CommonAce(
                    AceFlags.None,
                    qualifiers[index],
                    accessMasks[index],
                    users,
                    false,
                    null));
            }

            return new RawSecurityDescriptor(
                ControlFlags.DiscretionaryAclPresent,
                null,
                null,
                null,
                dacl);
        }
    }
}
