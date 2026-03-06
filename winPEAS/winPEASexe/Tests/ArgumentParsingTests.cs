using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Resets all public static Checks fields that arg parsing can mutate, then
        /// invokes Program.Main with the supplied args followed by "--help" so execution
        /// returns immediately after parsing without running any actual system checks.
        /// </summary>
        private static void ParseOnly(params string[] args)
        {
            // Reset every field that Checks.Run() can modify during arg parsing.
            winPEAS.Checks.Checks.IsDomainEnumeration = false;
            winPEAS.Checks.Checks.IsNoColor           = false;
            winPEAS.Checks.Checks.DontCheckHostname   = false;
            winPEAS.Checks.Checks.Banner              = true;
            winPEAS.Checks.Checks.IsDebug             = false;
            winPEAS.Checks.Checks.IsLinpeas           = false;
            winPEAS.Checks.Checks.IsLolbas            = false;
            winPEAS.Checks.Checks.IsNetworkScan       = false;
            winPEAS.Checks.Checks.SearchProgramFiles  = false;
            winPEAS.Checks.Checks.NetworkScanOptions  = string.Empty;
            winPEAS.Checks.Checks.PortScannerPorts    = null;
            winPEAS.Checks.Checks.LinpeasUrl          = "https://github.com/carlospolop/PEASS-ng/releases/latest/download/linpeas.sh";
            winPEAS.Checks.Checks.MaxRegexFileSize    = 1000000;

            var argsWithHelp = args.Concat(new[] { "--help" }).ToArray();
            Program.Main(argsWithHelp);
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

        // -- Space-separated argument normalisation tests --

        [TestMethod]
        public void NetworkFlag_SpaceSeparated_Netmask_SetsIsNetworkScan()
        {
            ParseOnly("-network", "10.0.0.0/24");
            Assert.IsTrue(winPEAS.Checks.Checks.IsNetworkScan,
                "-network 10.0.0.0/24 (space-separated) should set IsNetworkScan");
            Assert.AreEqual("10.0.0.0/24", winPEAS.Checks.Checks.NetworkScanOptions);
        }

        [TestMethod]
        public void NetworkFlag_SpaceSeparated_Auto_SetsIsNetworkScan()
        {
            ParseOnly("-network", "auto");
            Assert.IsTrue(winPEAS.Checks.Checks.IsNetworkScan,
                "-network auto (space-separated) should set IsNetworkScan");
            Assert.AreEqual("auto", winPEAS.Checks.Checks.NetworkScanOptions,
                StringComparer.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void NetworkFlag_EqualsSeparated_Netmask_SetsIsNetworkScan()
        {
            ParseOnly("-network=10.0.0.0/24");
            Assert.IsTrue(winPEAS.Checks.Checks.IsNetworkScan,
                "-network=10.0.0.0/24 (equals-separated) should set IsNetworkScan");
            Assert.AreEqual("10.0.0.0/24", winPEAS.Checks.Checks.NetworkScanOptions);
        }

        [TestMethod]
        public void NetworkAndPortsFlags_SpaceSeparated_BothParsedCorrectly()
        {
            ParseOnly("-network", "auto", "-ports", "80,443");
            Assert.IsTrue(winPEAS.Checks.Checks.IsNetworkScan,
                "-network auto -ports 80,443 should set IsNetworkScan");
            var ports = winPEAS.Checks.Checks.PortScannerPorts?.ToList();
            Assert.IsNotNull(ports, "PortScannerPorts should not be null");
            CollectionAssert.AreEquivalent(new List<int> { 80, 443 }, ports);
        }
    }
}
