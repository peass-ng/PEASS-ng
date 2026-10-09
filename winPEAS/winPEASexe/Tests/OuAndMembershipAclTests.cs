using System;
using System.DirectoryServices;
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

        private static string[] MapAllow(ActiveDirectoryRights rights, string targetClass, Guid objectType)
        {
            return ActiveDirectoryInfo.MapRuleToImpacts(rights, objectType, targetClass, null, null)
                .Select(impact => impact.Impact).ToArray();
        }

        [TestMethod]
        public void ReadOnlyOuAcesDoNotBecomeFullControlCandidates()
        {
            foreach (var right in new[] { ActiveDirectoryRights.GenericRead, ActiveDirectoryRights.ReadControl,
                ActiveDirectoryRights.ReadProperty, ActiveDirectoryRights.ListChildren,
                ActiveDirectoryRights.ListObject, ActiveDirectoryRights.Delete })
                Assert.AreEqual(0, MapAllow(right, "organizationalUnit", Guid.Empty).Length, right.ToString());
        }

        [TestMethod]
        public void CompositeGenericRightsRequireTheCompleteMask()
        {
            CollectionAssert.AreEqual(new[] { "GenericAll" },
                MapAllow(ActiveDirectoryRights.GenericAll, "organizationalUnit", Guid.Empty));
            var write = MapAllow(ActiveDirectoryRights.GenericWrite, "organizationalUnit", Guid.Empty);
            CollectionAssert.Contains(write, "GenericWrite");
            CollectionAssert.DoesNotContain(write, "GenericAll");
            CollectionAssert.AreEqual(new[] { "WriteDACL" },
                MapAllow(ActiveDirectoryRights.WriteDacl | ActiveDirectoryRights.ReadControl,
                    "organizationalUnit", Guid.Empty));
        }

        [TestMethod]
        public void ExactAttributeAndMembershipAcesReachTheirClassifiers()
        {
            CollectionAssert.AreEqual(new[] { "member WriteProperty" },
                MapAllow(ActiveDirectoryRights.WriteProperty, "group", MemberGuid));
            CollectionAssert.AreEqual(new[] { "Self-membership validated write" },
                MapAllow(ActiveDirectoryRights.Self, "group", MemberGuid));
            CollectionAssert.AreEqual(new[] { "UPN WriteProperty candidate" },
                MapAllow(ActiveDirectoryRights.WriteProperty, "user",
                    new Guid("28630ebb-41d5-11d1-a9c1-0000f80367c1")));
            CollectionAssert.AreEqual(new[] { "KeyCredentialLink WriteProperty candidate" },
                MapAllow(ActiveDirectoryRights.WriteProperty, "computer",
                    new Guid("5b47d60f-6090-40b2-9f37-2a4de88f3063")));
        }

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
