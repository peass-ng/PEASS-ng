using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class ServiceManagerCreateAccessTests
    {
        [TestMethod]
        public void GrantedAccessRequestsOnlyCreateServiceAndClosesHandle()
        {
            uint requested = 0;
            IntPtr closed = IntPtr.Zero;
            var handle = new IntPtr(17);
            ScmCreateServiceAccess result = ServicesInfoHelper.ProbeScmCreateServiceAccess(
                access => { requested = access; return handle; },
                () => { throw new AssertFailedException("Last error is irrelevant after a successful open."); },
                value => closed = value);

            Assert.AreEqual(ScmCreateServiceAccess.Granted, result);
            Assert.AreEqual((uint)0x0002, requested);
            Assert.AreEqual(handle, closed);
        }

        [TestMethod]
        public void AccessDeniedAndOtherErrorsRemainDistinctWithoutClosingInvalidHandle()
        {
            int closes = 0;
            Func<uint, IntPtr> deniedOpen = access => IntPtr.Zero;
            Action<IntPtr> close = value => closes++;

            Assert.AreEqual(ScmCreateServiceAccess.Denied,
                ServicesInfoHelper.ProbeScmCreateServiceAccess(deniedOpen, () => 5, close));
            Assert.AreEqual(ScmCreateServiceAccess.Unknown,
                ServicesInfoHelper.ProbeScmCreateServiceAccess(deniedOpen, () => 1722, close));
            Assert.AreEqual(0, closes);
        }

        [TestMethod]
        public void NativeFailureCannotBecomeAGrantedCandidate()
        {
            Assert.AreEqual(ScmCreateServiceAccess.Unknown,
                ServicesInfoHelper.ProbeScmCreateServiceAccess(
                    access => { throw new EntryPointNotFoundException(); },
                    () => 0,
                    handle => Assert.Fail("No handle was opened.")));
        }
    }
}
