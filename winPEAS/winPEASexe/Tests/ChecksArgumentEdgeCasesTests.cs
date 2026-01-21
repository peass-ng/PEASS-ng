using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace winPEAS.Tests
{
    [TestClass]
    public class ChecksArgumentEdgeCasesTests
    {
        [TestMethod]
        public void ShouldNotThrowOnEmptyLogFileArg()
        {
            // Should return early with a user-friendly error, not crash.
            Program.Main(new[] { "log=" });
        }

        [TestMethod]
        public void ShouldNotThrowOnPortsWithoutNetwork()
        {
            // Should warn and return early because -network was not provided.
            Program.Main(new[] { "-ports=80,443" });
        }

        [TestMethod]
        public void ShouldNotThrowOnInvalidNetworkArgument()
        {
            // Should warn and return early because the IP is invalid.
            Program.Main(new[] { "-network=10.10.10.999" });
        }

        [TestMethod]
        public void ShouldNotThrowOnEmptyNetworkArgument()
        {
            // Should warn and return early because the value is empty.
            Program.Main(new[] { "-network=" });
        }
    }
}
