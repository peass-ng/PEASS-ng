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
        private static readonly Guid ReaderListGuid = new Guid("888eedd6-ce04-df40-b462-b8a50e41ba38");

        private static byte[] MembershipDescriptor(params GenericAce[] aces)
        {
            var owner = new SecurityIdentifier("S-1-5-18");
            var acl = new RawAcl(GenericAcl.AclRevisionDS, aces.Length);
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

        private static CommonAce WriteOnlyAce(string sid)
        {
            return new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x20,
                new SecurityIdentifier(sid), false, null);
        }

        private static ObjectAce ReaderListWriteAce(string sid, AceQualifier qualifier,
            Guid? attribute = null, AceFlags flags = AceFlags.None, bool callback = false)
        {
            return new ObjectAce(flags, qualifier, 0x20, new SecurityIdentifier(sid),
                ObjectAceFlags.ObjectAceTypePresent, attribute ?? ReaderListGuid, Guid.Empty, callback, null);
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
        public void GmsaReaderSidsAreSeparateFromCurrentTokenMatches()
        {
            var descriptor = MembershipDescriptor(ReadAce(CurrentSid, AceQualifier.AccessAllowed),
                ReadAce(OtherSid, AceQualifier.AccessAllowed));
            var report = ActiveDirectoryInfo.InspectGmsaMembership(descriptor, TokenSids());
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Candidate, report.Status);
            CollectionAssert.AreEquivalent(new[] { CurrentSid, OtherSid }, report.ReaderSids);
            CollectionAssert.AreEqual(new[] { CurrentSid }, report.MatchingReaderSids);
        }

        [TestMethod]
        public void GmsaWriteOnlyAceIsNotAReaderCandidate()
        {
            var report = ActiveDirectoryInfo.InspectGmsaMembership(
                MembershipDescriptor(WriteOnlyAce(CurrentSid)), TokenSids());
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.NoMatch, report.Status);
            Assert.AreEqual(0, report.ReaderSids.Count);
        }

        [TestMethod]
        public void GmsaMatchingDenyKeepsAccessUnproven()
        {
            var report = ActiveDirectoryInfo.InspectGmsaMembership(
                MembershipDescriptor(ReadAce(CurrentSid, AceQualifier.AccessAllowed),
                    ReadAce(CurrentSid, AceQualifier.AccessDenied)), TokenSids());
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Denied, report.Status);
            CollectionAssert.AreEqual(new[] { CurrentSid }, report.MatchingDenySids);
        }

        [TestMethod]
        public void GmsaConditionalOrObjectScopedAceIsUnknown()
        {
            var callback = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x10,
                new SecurityIdentifier(CurrentSid), true, null);
            var scoped = new ObjectAce(AceFlags.None, AceQualifier.AccessAllowed, 0x10,
                new SecurityIdentifier(CurrentSid), ObjectAceFlags.ObjectAceTypePresent,
                new Guid("11111111-2222-3333-4444-555555555555"), Guid.Empty, false, null);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Unknown,
                ActiveDirectoryInfo.AssessGmsaMembership(MembershipDescriptor(callback), TokenSids()));
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Unknown,
                ActiveDirectoryInfo.AssessGmsaMembership(MembershipDescriptor(scoped), TokenSids()));
        }

        [TestMethod]
        public void GmsaMalformedAndOversizedDescriptorsAreUnknown()
        {
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Unknown,
                ActiveDirectoryInfo.AssessGmsaMembership(new byte[] { 1, 2, 3 }, TokenSids()));
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Unknown,
                ActiveDirectoryInfo.AssessGmsaMembership(new byte[16385], TokenSids()));
        }

        [TestMethod]
        public void GmsaGroupJoinNeedsRefreshedTokenSidSet()
        {
            var descriptor = MembershipDescriptor(ReadAce(OtherSid, AceQualifier.AccessAllowed));
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.NoMatch,
                ActiveDirectoryInfo.AssessGmsaMembership(descriptor, TokenSids()));
            var refreshedSids = TokenSids();
            refreshedSids.Add(OtherSid);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaAccessStatus.Candidate,
                ActiveDirectoryInfo.AssessGmsaMembership(descriptor, refreshedSids));
        }

        [TestMethod]
        public void GmsaReaderListWriteRequiresExactAttributeAndClass()
        {
            Assert.AreEqual(ActiveDirectoryInfo.ExactAttributeWriteRight.GmsaReaderList,
                ActiveDirectoryInfo.ClassifyExactAttributeWrite(ReaderListGuid, false,
                    "msDS-GroupManagedServiceAccount"));
            Assert.AreEqual(ActiveDirectoryInfo.ExactAttributeWriteRight.None,
                ActiveDirectoryInfo.ClassifyExactAttributeWrite(ReaderListGuid, false, "user"));
            Assert.AreEqual(ActiveDirectoryInfo.ExactAttributeWriteRight.None,
                ActiveDirectoryInfo.ClassifyExactAttributeWrite(ReaderListGuid, true,
                    "msDS-GroupManagedServiceAccount"));
            var mapped = ActiveDirectoryInfo.MapAttributeWriteImpact(ReaderListGuid,
                "msDS-GroupManagedServiceAccount", null, null, false);
            Assert.AreEqual("gMSA reader-list WriteProperty candidate", mapped.Impact);

            var candidate = ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                MembershipDescriptor(ReaderListWriteAce(CurrentSid, AceQualifier.AccessAllowed)), TokenSids());
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Candidate, candidate.Status);
            Assert.IsFalse(candidate.BroadWrite);
            CollectionAssert.AreEqual(new[] { CurrentSid }, candidate.MatchingWriterSids);

            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.NoMatch,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                    MembershipDescriptor(ReaderListWriteAce(CurrentSid, AceQualifier.AccessAllowed,
                        new Guid("888eedd7-ce04-df40-b462-b8a50e41ba38"))), TokenSids()).Status);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.NoMatch,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                    MembershipDescriptor(ReaderListWriteAce(OtherSid, AceQualifier.AccessAllowed)), TokenSids()).Status);
        }

        [TestMethod]
        public void GmsaReaderListWriteDenyAndComplexScopeStayUnproven()
        {
            var denied = ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                MembershipDescriptor(ReaderListWriteAce(CurrentSid, AceQualifier.AccessAllowed),
                    ReaderListWriteAce(CurrentSid, AceQualifier.AccessDenied)), TokenSids());
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Denied, denied.Status);
            CollectionAssert.AreEqual(new[] { CurrentSid }, denied.MatchingDenySids);

            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Unknown,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                    MembershipDescriptor(ReaderListWriteAce(CurrentSid, AceQualifier.AccessAllowed,
                        flags: AceFlags.Inherited, callback: true)), TokenSids()).Status);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.NoMatch,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                    MembershipDescriptor(ReaderListWriteAce(CurrentSid, AceQualifier.AccessAllowed,
                        flags: AceFlags.InheritOnly)), TokenSids()).Status);
            var scopedInheritance = new ObjectAce(AceFlags.Inherited, AceQualifier.AccessAllowed, 0x20,
                new SecurityIdentifier(CurrentSid), ObjectAceFlags.ObjectAceTypePresent |
                ObjectAceFlags.InheritedObjectAceTypePresent, ReaderListGuid,
                new Guid("11111111-2222-3333-4444-555555555555"), false, null);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Unknown,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                    MembershipDescriptor(scopedInheritance), TokenSids()).Status);
        }

        [TestMethod]
        public void GmsaReaderListBroadWriteAndMalformedDescriptorAreBounded()
        {
            var broad = ActiveDirectoryInfo.InspectGmsaReaderListWrite(
                MembershipDescriptor(new CommonAce(AceFlags.Inherited, AceQualifier.AccessAllowed, 0x20,
                    new SecurityIdentifier(CurrentSid), false, null)), TokenSids());
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Candidate, broad.Status);
            Assert.IsTrue(broad.BroadWrite);
            Assert.IsTrue(broad.InheritedAce);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Unknown,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(new byte[] { 1, 2, 3 }, TokenSids()).Status);
            Assert.AreEqual(ActiveDirectoryInfo.GmsaReaderListWriteStatus.Unknown,
                ActiveDirectoryInfo.InspectGmsaReaderListWrite(new byte[65537], TokenSids()).Status);
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
