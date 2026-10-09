using System;
using System.DirectoryServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class Esc1TemplateCandidateTests
    {
        private static readonly string[] ClientAuth = { "1.3.6.1.5.5.7.3.2" };

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
    }
}
