using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class Esc1TemplateCandidateTests
    {
        private static readonly string[] ClientAuth = { "1.3.6.1.5.5.7.3.2" };

        [TestMethod]
        public void DisabledUserAclCueRequiresConfirmedDisabledUser()
        {
            StringAssert.Contains(ActiveDirectoryInfo.DescribeDisabledUserAclTarget("user", 0x202),
                "password reset alone");
            Assert.IsNull(ActiveDirectoryInfo.DescribeDisabledUserAclTarget("user", 0x200));
            Assert.IsNull(ActiveDirectoryInfo.DescribeDisabledUserAclTarget("user", null));
            Assert.IsNull(ActiveDirectoryInfo.DescribeDisabledUserAclTarget("computer", 0x1002));
            Assert.IsNull(ActiveDirectoryInfo.DescribeDisabledUserAclTarget("group", 0x2));
        }

        [TestMethod]
        public void Esc15RequiresPublishedV1SuppliedSubjectAndAutomaticIssuance()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc15Template(1, 1, 0, 0, true));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc15Template(2, 1, 0, 0, true));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc15Template(1, 0, 0, 0, true));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc15Template(1, 1, 2, 0, true));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc15Template(1, 1, 0, 1, true));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc15Template(1, 1, 0, 0, false));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc15Template(1, 1, 0, 0, null));
            Assert.AreEqual(ActiveDirectoryInfo.Esc15TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc15Template(null, 1, 0, 0, true));
        }

        [TestMethod]
        public void DeletedObjectSidFilterUsesExactBinarySid()
        {
            Assert.AreEqual("\\01\\05\\00\\00\\00\\00\\00\\05\\15\\00\\00\\00\\01\\00\\00\\00\\02\\00\\00\\00\\03\\00\\00\\00\\04\\00\\00\\00",
                ActiveDirectoryInfo.SidToLdapFilterBytes("S-1-5-21-1-2-3-4"));
            Assert.IsNull(ActiveDirectoryInfo.SidToLdapFilterBytes("S-1-5-21-1-2-3-4)(objectClass=*)"));
        }

        [TestMethod]
        public void CaRoleMaskSeparatesAdministratorOfficerAndEnrollmentRights()
        {
            Assert.AreEqual("CA Administrator, Enroll", ActiveDirectoryInfo.DescribeCaAccessMask(0x201));
            Assert.AreEqual("Certificate Manager", ActiveDirectoryInfo.DescribeCaAccessMask(0x2));
            Assert.IsNull(ActiveDirectoryInfo.DescribeCaAccessMask(0x100));
        }

        [TestMethod]
        public void TemplateContainerCreateAceRequiresApplicableClassAndCurrentContainer()
        {
            var templateClass = new Guid("e5209ca2-3bba-11d2-90cc-00c04fd91ab1");
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.CreateChild, templateClass, PropagationFlags.None));
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.CreateChild, Guid.Empty, PropagationFlags.None));
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.GenericAll, Guid.Empty, PropagationFlags.None));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.CreateChild, Guid.NewGuid(), PropagationFlags.None));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.WriteProperty, templateClass, PropagationFlags.None));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.DeleteChild, templateClass, PropagationFlags.None));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateTemplateCreateAce(
                ActiveDirectoryRights.CreateChild, templateClass, PropagationFlags.InheritOnly));
        }

        [TestMethod]
        public void CompleteClientAuthenticationTemplateIsConfigurationCandidate()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc1Template(1, 0, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc1Template(0x10000, 0, 0, ClientAuth));
        }

        [TestMethod]
        public void ApprovalSignaturesOrUnrelatedEkuExcludeTemplate()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc1Template(1, 2, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc1Template(1, 0, 1, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc1Template(0, 0, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc1Template(1, 0, 0, new[] { "1.3.6.1.5.5.7.3.1" }));
        }

        [TestMethod]
        public void MissingConfigurationIsUnknown()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc1Template(null, 0, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc1Template(1, 0, null, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc1TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc1Template(1, 0, 0, null));
        }

        [TestMethod]
        public void EnrollmentRequiresCorrectExtendedRightOrBroadAce()
        {
            var enroll = new Guid("0e10c968-78fb-11d2-90d4-00c04f79dc55");
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateEnrollAce(ActiveDirectoryRights.ExtendedRight, enroll));
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateEnrollAce(ActiveDirectoryRights.ExtendedRight, Guid.Empty));
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateEnrollAce(ActiveDirectoryRights.GenericAll, Guid.Empty));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateEnrollAce(ActiveDirectoryRights.ExtendedRight, Guid.NewGuid()));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateEnrollAce(ActiveDirectoryRights.WriteProperty, enroll));
        }

        [TestMethod]
        public void UnscopedReadAndWriteRightsAreNotEnrollmentRights()
        {
            foreach (var rights in new[] {
                ActiveDirectoryRights.GenericRead, ActiveDirectoryRights.ReadControl,
                ActiveDirectoryRights.ReadProperty, ActiveDirectoryRights.ListChildren,
                ActiveDirectoryRights.ListObject, ActiveDirectoryRights.WriteProperty,
                ActiveDirectoryRights.GenericWrite, ActiveDirectoryRights.WriteDacl,
                ActiveDirectoryRights.WriteOwner
            })
                Assert.IsFalse(ActiveDirectoryInfo.IsCertificateEnrollAce(rights, Guid.Empty),
                    "Non-enrollment rights must not match the composite GenericAll mask: " + rights);
        }

        [TestMethod]
        public void InheritOnlyEnrollmentAcesDoNotApplyToTheTemplate()
        {
            var enroll = new Guid("0e10c968-78fb-11d2-90d4-00c04f79dc55");
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateEnrollAce(
                ActiveDirectoryRights.ExtendedRight, enroll, inheritOnly: true));
            Assert.IsFalse(ActiveDirectoryInfo.IsCertificateEnrollAce(
                ActiveDirectoryRights.GenericAll, Guid.Empty, inheritOnly: true));
            Assert.IsTrue(ActiveDirectoryInfo.IsCertificateEnrollAce(
                ActiveDirectoryRights.ExtendedRight, enroll, inheritOnly: false));
        }

        [TestMethod]
        public void DomainComputersSidNeedsTokenDomainGroupContext()
        {
            Assert.AreEqual("S-1-5-21-10-20-30-515", ActiveDirectoryInfo.DomainComputersSidFromToken(
                new[] { "S-1-5-21-10-20-30-1104", "S-1-5-21-10-20-30-513" }));
            Assert.IsNull(ActiveDirectoryInfo.DomainComputersSidFromToken(
                new[] { "S-1-5-21-10-20-30-1104", "S-1-5-32-545" }));
        }

        [TestMethod]
        public void EnrollmentEvidenceKeepsPrincipalSpecificDenyAndUnknownSeparate()
        {
            Assert.AreEqual("enrollment ACL unavailable",
                ActiveDirectoryInfo.DescribeEsc1EnrollAceEvidence(false, true, false, false, false, true));
            Assert.AreEqual("current-token Enroll allow ACE; Domain Computers matching deny ACE",
                ActiveDirectoryInfo.DescribeEsc1EnrollAceEvidence(true, true, false, true, true, true));
            Assert.AreEqual("current-token matching deny ACE; Domain Computers Enroll allow ACE",
                ActiveDirectoryInfo.DescribeEsc1EnrollAceEvidence(true, true, true, true, false, true));
            Assert.AreEqual("Domain Computers SID unavailable from token",
                ActiveDirectoryInfo.DescribeEsc1EnrollAceEvidence(true, false, false, false, false, false));
        }

        [TestMethod]
        public void MinimumKeySizeIsReportedWithoutAssumingAClientDefault()
        {
            Assert.AreEqual("minimum key size 4096", ActiveDirectoryInfo.DescribeTemplateMinimumKeySize(4096));
            Assert.AreEqual("minimum key size unknown", ActiveDirectoryInfo.DescribeTemplateMinimumKeySize(null));
        }

        [TestMethod]
        public void Esc9RequiresNoSecurityExtensionAndClientAuthentication()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, 0, new[] { "2.5.29.37.0" }));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc9Template(0, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, 0, new[] { "1.3.6.1.5.5.7.3.1" }));
        }

        [TestMethod]
        public void Esc9ApprovalOrSignaturesExcludeThisPassivePath()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc9Template(0x80002, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, 1, ClientAuth));
        }

        [TestMethod]
        public void Esc9IncompleteAttributesRemainUnknown()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc9Template(null, 0, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, null, ClientAuth));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, 0, null));
            Assert.AreEqual(ActiveDirectoryInfo.Esc9TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc9Template(0x80000, 0, new string[0]));
        }

        [TestMethod]
        public void Esc13RequiresExactLinkedIssuancePolicyAndAuthentication()
        {
            var links = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "1.2.3.4", "CN=LinkedGroup,DC=example,DC=test" }
            };
            string group;
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    0, 0, ClientAuth, out group));
            Assert.AreEqual("CN=LinkedGroup,DC=example,DC=test", group);
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.40" }, links, true,
                    0, 0, ClientAuth, out group));
            Assert.IsNull(group);
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc13Template(new string[0], links, false,
                    0, 0, ClientAuth, out group));
        }

        [TestMethod]
        public void Esc13IncompleteOidLookupAndConflictingRecordsRemainUnknown()
        {
            string group;
            var links = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "1.2.3.4", string.Empty }
            };
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    0, 0, ClientAuth, out group));
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.5" }, links, false,
                    0, 0, ClientAuth, out group));
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, false,
                    0, 0, ClientAuth, out group));
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc13Template(null, links, true,
                    0, 0, ClientAuth, out group));
            links.Add("1.2.3.6", "CN=OtherLinkedGroup");
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4", "1.2.3.6" }, links, true,
                    0, 0, ClientAuth, out group));
            Assert.AreEqual("CN=OtherLinkedGroup", group);
        }

        [TestMethod]
        public void Esc13IssuanceGatingAndEkuAreConservative()
        {
            var links = new Dictionary<string, string> { { "1.2.3.4", "CN=LinkedGroup" } };
            string group;
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    2, 0, ClientAuth, out group)); // Manager approval required.
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    0, 1, ClientAuth, out group)); // Authorized signature required.
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.NotCandidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    0, 0, new[] { "1.3.6.1.5.5.7.3.1" }, out group));
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Candidate,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    0, 0, new[] { "2.5.29.37.0" }, out group));
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    null, 0, ClientAuth, out group));
            Assert.AreEqual(ActiveDirectoryInfo.Esc13TemplateStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc13Template(new[] { "1.2.3.4" }, links, true,
                    0, 0, new string[0], out group));
        }
    }
}
