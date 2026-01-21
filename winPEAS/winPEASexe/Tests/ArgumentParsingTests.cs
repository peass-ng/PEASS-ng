using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace winPEAS.Tests
{
    [TestClass]
    public class ArgumentParsingTests
    {
        private static bool InvokeIsNetworkTypeValid(string arg)
        {
            var method = typeof(winPEAS.Checks.Checks).GetMethod("IsNetworkTypeValid", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "IsNetworkTypeValid method not found.");
            return (bool)method.Invoke(null, new object[] { arg });
        }

        [TestMethod]
        public void ShouldAcceptValidNetworkTypes()
        {
            Assert.IsTrue(InvokeIsNetworkTypeValid("-network=auto"));
            Assert.IsTrue(InvokeIsNetworkTypeValid("-network=10.10.10.10"));
            Assert.IsTrue(InvokeIsNetworkTypeValid("-network=10.10.10.10/24"));
            Assert.IsTrue(InvokeIsNetworkTypeValid("-network=10.10.10.10,10.10.10.20"));
        }

        [TestMethod]
        public void ShouldRejectInvalidNetworkTypes()
        {
            Assert.IsFalse(InvokeIsNetworkTypeValid("-network="));
            Assert.IsFalse(InvokeIsNetworkTypeValid("-network=10.10.10.999"));
            Assert.IsFalse(InvokeIsNetworkTypeValid("-network=10.10.10.10/64"));
            Assert.IsFalse(InvokeIsNetworkTypeValid("-network=999.999.999.999/24"));
            Assert.IsFalse(InvokeIsNetworkTypeValid("-network=not-an-ip"));
        }
    }
}
