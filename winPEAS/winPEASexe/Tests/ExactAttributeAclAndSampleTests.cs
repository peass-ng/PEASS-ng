using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.AccessControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class ExactAttributeAclAndSampleTests
    {
        private static readonly Guid UpnGuid = new Guid("28630ebb-41d5-11d1-a9c1-0000f80367c1");
        private static readonly Guid KeyCredentialGuid = new Guid("5b47d60f-6090-40b2-9f37-2a4de88f3063");
        private static readonly Guid AltSecurityIdentitiesGuid = new Guid("00fbf30c-91fe-11d1-aebc-0000f80367c1");

        private static ActiveDirectoryInfo.AdAccessImpact Candidate(Guid objectType, string targetClass,
            AccessControlType accessType = AccessControlType.Allow, bool inheritOnly = false, bool validatedWrite = false)
        {
            if (!ActiveDirectoryInfo.IsAclCandidateAce(accessType, true, inheritOnly, false, Guid.Empty, targetClass))
                return null;
            return ActiveDirectoryInfo.MapAttributeWriteImpact(objectType, targetClass, null, null, validatedWrite);
        }

        [TestMethod]
        public void UpnRequiresExactWritePropertyAllowOnUser()
        {
            var impact = Candidate(UpnGuid, "user");
            Assert.IsNotNull(impact);
            Assert.AreEqual("UPN WriteProperty candidate", impact.Impact);
            StringAssert.Contains(impact.Detail, "Matching allow ACE is a lead only");
            StringAssert.Contains(impact.Detail, "effective access");
            Assert.IsNull(Candidate(UpnGuid, "computer"));
            Assert.IsNull(Candidate(new Guid("28630ebc-41d5-11d1-a9c1-0000f80367c1"), "user"));
            Assert.IsNull(Candidate(UpnGuid, "user", AccessControlType.Deny));
            Assert.IsNull(Candidate(UpnGuid, "user", inheritOnly: true));
            Assert.IsNull(Candidate(UpnGuid, "user", validatedWrite: true));
        }

        [TestMethod]
        public void KeyCredentialRequiresExactWritePropertyOnUserOrComputer()
        {
            foreach (var targetClass in new[] { "user", "computer" })
            {
                var impact = Candidate(KeyCredentialGuid, targetClass);
                Assert.IsNotNull(impact);
                Assert.AreEqual("KeyCredentialLink WriteProperty candidate", impact.Impact);
                StringAssert.Contains(impact.Detail, "certificate path remain unverified");
            }
            Assert.IsNull(Candidate(KeyCredentialGuid, "group"));
            Assert.IsNull(Candidate(KeyCredentialGuid, "computer", AccessControlType.Deny));
            Assert.IsNull(Candidate(KeyCredentialGuid, "computer", inheritOnly: true));
            Assert.IsNull(Candidate(KeyCredentialGuid, "computer", validatedWrite: true));
        }

        [TestMethod]
        public void CertificateMappingRequiresExactWritePropertyOnUserOrComputer()
        {
            foreach (var targetClass in new[] { "user", "computer" })
            {
                var impact = Candidate(AltSecurityIdentitiesGuid, targetClass);
                Assert.IsNotNull(impact);
                Assert.AreEqual("altSecurityIdentities WriteProperty candidate", impact.Impact);
                StringAssert.Contains(impact.Detail, "strong binding require review");
            }
            Assert.IsNull(Candidate(AltSecurityIdentitiesGuid, "group"));
            Assert.IsNull(Candidate(AltSecurityIdentitiesGuid, "user", AccessControlType.Deny));
            Assert.IsNull(Candidate(AltSecurityIdentitiesGuid, "computer", inheritOnly: true));
            Assert.IsNull(Candidate(AltSecurityIdentitiesGuid, "user", validatedWrite: true));
            Assert.IsNull(Candidate(new Guid("00fbf30d-91fe-11d1-aebc-0000f80367c1"), "user"));
        }

        [TestMethod]
        public void NewGroupMembershipNeedsRefreshedContext()
        {
            StringAssert.Contains(ActiveDirectoryInfo.DescribeMembershipCandidate(
                ActiveDirectoryInfo.MembershipWriteRight.MemberAttribute), "refreshed token/session");
        }

        [TestMethod]
        public void ObjectSampleUsesOneSentinelWithoutAnalyzingIt()
        {
            foreach (var count in new[] { 119, 120, 121 })
            {
                var enumerated = 0;
                var source = Enumerable.Range(0, count).Select(i =>
                {
                    enumerated++;
                    return i == 120 ? "candidate beyond sample" : "ordinary";
                });
                var sample = ActiveDirectoryInfo.SelectBoundedSample(source, 120);
                Assert.AreEqual(Math.Min(count, 120), sample.Items.Count);
                Assert.AreEqual(count > 120, sample.Truncated);
                Assert.AreEqual(Math.Min(count, 121), enumerated);
                Assert.IsFalse(sample.Items.Contains("candidate beyond sample"));
                Assert.AreEqual(0, sample.Items.Count(item => item.Contains("candidate")));
            }
        }

        [TestMethod]
        public void CurrentComputerAclTargetIsSelectedBeforeCappedSample()
        {
            const string computerDn = "CN=DC,CN=Computers,DC=example,DC=test";
            var objects = Enumerable.Range(0, 120).Select(i => "CN=Object" + i + ",DC=example,DC=test")
                .Concat(new[] { computerDn });
            var sample = ActiveDirectoryInfo.SelectBoundedSample(objects, 120);
            var inspected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(ActiveDirectoryInfo.ShouldInspectAclTarget(computerDn, inspected));
            inspected.Add(computerDn);
            foreach (var dn in sample.Items)
                if (ActiveDirectoryInfo.ShouldInspectAclTarget(dn, inspected)) inspected.Add(dn);

            Assert.IsTrue(sample.Truncated);
            Assert.AreEqual(120, sample.Items.Count);
            Assert.IsFalse(sample.Items.Contains(computerDn));
            Assert.IsTrue(inspected.Contains(computerDn));
            Assert.AreEqual(121, inspected.Count);
        }

        [TestMethod]
        public void CurrentComputerAclTargetIsDeduplicatedByExactDn()
        {
            const string computerDn = "CN=DC,CN=Computers,DC=example,DC=test";
            var sample = ActiveDirectoryInfo.SelectBoundedSample(new[] {
                "cn=dc,cn=computers,dc=EXAMPLE,dc=test",
                "CN=OTHER,CN=Computers,DC=example,DC=test"
            }, 120);
            var inspected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Assert.IsTrue(ActiveDirectoryInfo.ShouldInspectAclTarget(computerDn, inspected));
            inspected.Add(computerDn);

            Assert.IsFalse(ActiveDirectoryInfo.ShouldInspectAclTarget(sample.Items[0], inspected));
            Assert.IsTrue(ActiveDirectoryInfo.ShouldInspectAclTarget(sample.Items[1], inspected));
            inspected.Add(sample.Items[1]);
            Assert.AreEqual(2, inspected.Count);
            Assert.IsFalse(ActiveDirectoryInfo.ShouldInspectAclTarget("", inspected));
        }
    }
}
