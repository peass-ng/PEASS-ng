using System;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ActiveDirectoryInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class CurrentComputerRbcdTests
    {
        private static byte[] Descriptor(int aceCount)
        {
            var owner = new SecurityIdentifier("S-1-5-18");
            var acl = new RawAcl(2, aceCount);
            for (int i = 0; i < aceCount; i++)
            {
                var sid = new SecurityIdentifier("S-1-5-21-111-222-333-" + (1000 + i));
                acl.InsertAce(acl.Count, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed,
                    0x100, sid, false, null));
            }
            var descriptor = new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent, owner, owner, null, acl);
            var bytes = new byte[descriptor.BinaryLength];
            descriptor.GetBinaryForm(bytes, 0);
            return bytes;
        }

        [TestMethod]
        public void AbsentAttributeHasNoTrustees()
        {
            var report = CurrentComputerRbcd.Evaluate(null);
            Assert.AreEqual(CurrentComputerRbcdStatus.Absent, report.Status);
            Assert.AreEqual(0, report.TrusteeSids.Count);
        }

        [TestMethod]
        public void ValidDescriptorReportsTrusteeSidsAsState()
        {
            var report = CurrentComputerRbcd.Evaluate(Descriptor(2));
            Assert.AreEqual(CurrentComputerRbcdStatus.Present, report.Status);
            Assert.AreEqual(2, report.AceCount);
            CollectionAssert.AreEqual(new[] {
                "S-1-5-21-111-222-333-1000", "S-1-5-21-111-222-333-1001"
            }, (System.Collections.ICollection)report.TrusteeSids);
            Assert.IsFalse(report.Truncated);
        }

        [TestMethod]
        public void MalformedDescriptorIsUnknown()
        {
            Assert.AreEqual(CurrentComputerRbcdStatus.Malformed, CurrentComputerRbcd.Evaluate(new byte[] { 1, 2, 3 }).Status);
            Assert.AreEqual(CurrentComputerRbcdStatus.Malformed, CurrentComputerRbcd.Evaluate(new byte[0]).Status);
        }

        [TestMethod]
        public void OversizedDescriptorIsNotParsed()
        {
            var report = CurrentComputerRbcd.Evaluate(new byte[CurrentComputerRbcd.MaxDescriptorBytes + 1]);
            Assert.AreEqual(CurrentComputerRbcdStatus.Oversized, report.Status);
            Assert.AreEqual(0, report.TrusteeSids.Count);
        }

        [TestMethod]
        public void TrusteeAndAceOutputAreBounded()
        {
            var report = CurrentComputerRbcd.Evaluate(Descriptor(CurrentComputerRbcd.MaxAcesInspected + 1));
            Assert.AreEqual(CurrentComputerRbcdStatus.Present, report.Status);
            Assert.AreEqual(CurrentComputerRbcd.MaxTrusteeSids, report.TrusteeSids.Count);
            Assert.AreEqual(CurrentComputerRbcd.MaxAcesInspected + 1, report.AceCount);
            Assert.IsTrue(report.Truncated);
        }
    }
}
