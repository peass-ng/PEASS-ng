using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class GmsaAndAdcsIndicatorsTests
    {
        private const string CurrentSid = "S-1-5-21-111-222-333-1001";
        private const string OtherSid = "S-1-5-21-111-222-333-1002";

        private static byte[] MembershipDescriptor(params CommonAce[] aces)
        {
            var owner = new SecurityIdentifier("S-1-5-18");
            var acl = new RawAcl(2, aces.Length);
            foreach (var ace in aces)
                acl.InsertAce(acl.Count, ace);
            var descriptor = new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent, owner, owner, null, acl);
            var bytes = new byte[descriptor.BinaryLength];
            descriptor.GetBinaryForm(bytes, 0);
            return bytes;
        }

        private static CommonAce ReadAce(string sid, AceQualifier qualifier)
        {
            return new CommonAce(AceFlags.None, qualifier, 0x10, new SecurityIdentifier(sid), false, null);
        }

        private static HashSet<string> TokenSids()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CurrentSid };
        }

        [TestMethod]
        public void GmsaMatchingAllowIsOnlyACandidate()
        {
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Candidate,
                ActiveDirectoryInfo.AssessGmsaMembership(
                    MembershipDescriptor(ReadAce(CurrentSid, AceQualifier.AccessAllowed)), TokenSids()));
        }

        [TestMethod]
        public void GmsaMatchingDenySuppressesAllow()
        {
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Denied,
                ActiveDirectoryInfo.AssessGmsaMembership(
                    MembershipDescriptor(ReadAce(CurrentSid, AceQualifier.AccessAllowed),
                        ReadAce(CurrentSid, AceQualifier.AccessDenied)), TokenSids()));
        }

        [TestMethod]
        public void GmsaUnrelatedSidDoesNotGrantAccess()
        {
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.NoMatch,
                ActiveDirectoryInfo.AssessGmsaMembership(
                    MembershipDescriptor(ReadAce(OtherSid, AceQualifier.AccessAllowed)), TokenSids()));
        }

        [TestMethod]
        public void GmsaMissingDescriptorIsUnknown()
        {
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Unknown,
                ActiveDirectoryInfo.AssessGmsaMembership(null, TokenSids()));
        }

        [TestMethod]
        public void Esc16MatchesOnlyExactMultiStringElement()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc16RegistryStatus.Present,
                ActiveDirectoryInfo.AssessEsc16(new[] { "1.2.3", "1.3.6.1.4.1.311.25.2" }));
            Assert.AreEqual(ActiveDirectoryInfo.Esc16RegistryStatus.Absent,
                ActiveDirectoryInfo.AssessEsc16(new[] { "prefix1.3.6.1.4.1.311.25.2", "1.3.6.1.4.1.311.25.20" }));
        }

        [TestMethod]
        public void Esc16MissingOrWronglyTypedValueIsUnknown()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc16RegistryStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc16(null));
            Assert.AreEqual(ActiveDirectoryInfo.Esc16RegistryStatus.Unknown,
                ActiveDirectoryInfo.AssessEsc16("1.3.6.1.4.1.311.25.2"));
            Assert.AreEqual(ActiveDirectoryInfo.Esc16RegistryStatus.Absent,
                ActiveDirectoryInfo.AssessEsc16(new string[0]));
        }

        [TestMethod]
        public void Esc6EditFlagIsBoundedAndUnknownWhenMissing()
        {
            Assert.AreEqual(ActiveDirectoryInfo.Esc6RegistryStatus.Candidate, ActiveDirectoryInfo.AssessEsc6(0x40000));
            Assert.AreEqual(ActiveDirectoryInfo.Esc6RegistryStatus.Candidate, ActiveDirectoryInfo.AssessEsc6(0x40001));
            Assert.AreEqual(ActiveDirectoryInfo.Esc6RegistryStatus.Absent, ActiveDirectoryInfo.AssessEsc6(0));
            Assert.AreEqual(ActiveDirectoryInfo.Esc6RegistryStatus.Unknown, ActiveDirectoryInfo.AssessEsc6(null));
        }
    }
}
