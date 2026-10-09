using System;
using System.Linq;
using System.Security.AccessControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class OuAndMembershipAclTests
    {
        private static readonly Guid MemberGuid = new Guid("bf9679c0-0de6-11d0-a285-00aa003049e2");
        private static readonly Guid OuClassGuid = new Guid("bf967aa5-0de6-11d0-a285-00aa003049e2");

        [TestMethod]
        public void OrdinaryOuSampleHasSeparateCapAndTruncation()
        {
            var names = Enumerable.Range(0, 102).Select(i => "OU=Unit" + i + ",DC=example,DC=test");
            var sample = ActiveDirectoryInfo.SelectOuSample(names, 50);
            Assert.AreEqual(50, sample.DistinguishedNames.Count);
            Assert.IsTrue(sample.Truncated);
            Assert.AreEqual("OU=Unit0,DC=example,DC=test", sample.DistinguishedNames[0]);
            Assert.AreEqual("OU=Unit49,DC=example,DC=test", sample.DistinguishedNames[49]);

            var exact = ActiveDirectoryInfo.SelectOuSample(names.Take(50), 50);
            Assert.AreEqual(50, exact.DistinguishedNames.Count);
            Assert.IsFalse(exact.Truncated);
        }

        [TestMethod]
        public void SelfMembershipAndMemberWritePropertyStayDistinct()
        {
            Assert.AreEqual(ActiveDirectoryInfo.MembershipWriteRight.OwnMembership,
                ActiveDirectoryInfo.ClassifyMembershipWrite(MemberGuid, true, "group"));
            Assert.AreEqual(ActiveDirectoryInfo.MembershipWriteRight.MemberAttribute,
                ActiveDirectoryInfo.ClassifyMembershipWrite(MemberGuid, false, "group"));
            Assert.AreEqual(ActiveDirectoryInfo.MembershipWriteRight.None,
                ActiveDirectoryInfo.ClassifyMembershipWrite(Guid.Empty, false, "group"));
            Assert.AreEqual(ActiveDirectoryInfo.MembershipWriteRight.None,
                ActiveDirectoryInfo.ClassifyMembershipWrite(MemberGuid, true, "user"));
            StringAssert.Contains(ActiveDirectoryInfo.DescribeMembershipCandidate(
                ActiveDirectoryInfo.MembershipWriteRight.OwnMembership), "only the current account");
            StringAssert.Contains(ActiveDirectoryInfo.DescribeMembershipCandidate(
                ActiveDirectoryInfo.MembershipWriteRight.MemberAttribute), "manage group membership");
        }

        [TestMethod]
        public void UnrelatedGuidsDoNotBecomeGroupMembershipRights()
        {
            Assert.AreEqual(ActiveDirectoryInfo.MembershipWriteRight.None,
                ActiveDirectoryInfo.ClassifyMembershipWrite(Guid.Empty, false, "group"));
            Assert.AreEqual(ActiveDirectoryInfo.MembershipWriteRight.None,
                ActiveDirectoryInfo.ClassifyMembershipWrite(Guid.Empty, true, "group"));
        }

        [TestMethod]
        public void AllowAceIsOnlyACandidateAndClassRestrictedOuInheritanceIsSkipped()
        {
            Assert.IsTrue(ActiveDirectoryInfo.IsAclCandidateAce(AccessControlType.Allow, true, false,
                false, Guid.Empty, "organizationalUnit"));
            Assert.IsFalse(ActiveDirectoryInfo.IsAclCandidateAce(AccessControlType.Allow, true, false,
                true, MemberGuid, "organizationalUnit"));
            Assert.IsTrue(ActiveDirectoryInfo.IsAclCandidateAce(AccessControlType.Allow, true, false,
                true, OuClassGuid, "organizationalUnit"));
            Assert.IsFalse(ActiveDirectoryInfo.IsAclCandidateAce(AccessControlType.Deny, true, false,
                false, Guid.Empty, "group"));
            Assert.IsFalse(ActiveDirectoryInfo.IsAclCandidateAce(AccessControlType.Allow, true, true,
                false, Guid.Empty, "group"));
        }

        [TestMethod]
        public void ValidatedSpnIsConstrainedToSupportedTargetClasses()
        {
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.WriteProperty,
                ActiveDirectoryInfo.ClassifySpnWrite("servicePrincipalName", false));
            Assert.IsTrue(ActiveDirectoryInfo.IsValidatedSpnTargetClass("computer"));
            Assert.IsTrue(ActiveDirectoryInfo.IsValidatedSpnTargetClass("msDS-ManagedServiceAccount"));
            Assert.IsFalse(ActiveDirectoryInfo.IsValidatedSpnTargetClass("user"));
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.None,
                ActiveDirectoryInfo.ClassifySpnWrite("servicePrincipalNameSuffix", false));
        }
    }
}
