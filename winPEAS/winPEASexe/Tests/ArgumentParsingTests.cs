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

        private static bool InvokePassesMitreFilter(string[] checkIds)
        {
            // Build a minimal ISystemCheck stub whose MitreAttackIds returns checkIds.
            var stub = new MitreCheckStub(checkIds);
            var method = typeof(winPEAS.Checks.Checks).GetMethod("PassesMitreFilter", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "PassesMitreFilter method not found.");
            return (bool)method.Invoke(null, new object[] { stub });
        }

        /// <summary>Minimal ISystemCheck stub for PassesMitreFilter reflection tests.</summary>
        private sealed class MitreCheckStub : winPEAS.Checks.ISystemCheck
        {
            public MitreCheckStub(string[] ids) { MitreAttackIds = ids; }
            public string[] MitreAttackIds { get; }
            public void PrintInfo(bool isDebug) { }
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
            winPEAS.Checks.Checks.MitreFilter.Clear();

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

        [TestMethod]
        public void MitreFlag_SingleTechnique_ParsedIntoFilter()
        {
            ParseOnly("mitre=T1082");
            Assert.AreEqual(1, winPEAS.Checks.Checks.MitreFilter.Count,
                "mitre=T1082 should add exactly one technique to MitreFilter");
            Assert.IsTrue(winPEAS.Checks.Checks.MitreFilter.Contains("T1082"),
                "MitreFilter should contain T1082");
        }

        [TestMethod]
        public void MitreFlag_MultipleIds_AllParsedIntoFilter()
        {
            ParseOnly("mitre=T1082,T1548.002,T1057");
            Assert.AreEqual(3, winPEAS.Checks.Checks.MitreFilter.Count,
                "mitre=T1082,T1548.002,T1057 should add three techniques to MitreFilter");
            Assert.IsTrue(winPEAS.Checks.Checks.MitreFilter.Contains("T1082"));
            Assert.IsTrue(winPEAS.Checks.Checks.MitreFilter.Contains("T1548.002"));
            Assert.IsTrue(winPEAS.Checks.Checks.MitreFilter.Contains("T1057"));
        }

        [TestMethod]
        public void MitreFlag_CaseInsensitive_IsRecognised()
        {
            ParseOnly("MITRE=t1082");
            Assert.AreEqual(1, winPEAS.Checks.Checks.MitreFilter.Count,
                "MITRE= (upper-case) should be accepted case-insensitively");
            // HashSet uses OrdinalIgnoreCase so both casing variants should be found
            Assert.IsTrue(winPEAS.Checks.Checks.MitreFilter.Contains("T1082") ||
                          winPEAS.Checks.Checks.MitreFilter.Contains("t1082"));
        }

        [TestMethod]
        public void PassesMitreFilter_EmptyFilter_AllChecksPass()
        {
            winPEAS.Checks.Checks.MitreFilter.Clear();
            Assert.IsTrue(InvokePassesMitreFilter(new[] { "T1082" }),
                "An empty MitreFilter should pass every check.");
            Assert.IsTrue(InvokePassesMitreFilter(new string[0]),
                "An empty MitreFilter should pass a check with no IDs.");
        }

        [TestMethod]
        public void PassesMitreFilter_ExactMatch_Passes()
        {
            winPEAS.Checks.Checks.MitreFilter.Clear();
            winPEAS.Checks.Checks.MitreFilter.Add("T1082");
            Assert.IsTrue(InvokePassesMitreFilter(new[] { "T1082" }),
                "A check tagged T1082 should pass when filter contains T1082.");
        }

        [TestMethod]
        public void PassesMitreFilter_NoMatch_Fails()
        {
            winPEAS.Checks.Checks.MitreFilter.Clear();
            winPEAS.Checks.Checks.MitreFilter.Add("T1082");
            Assert.IsFalse(InvokePassesMitreFilter(new[] { "T1057" }),
                "A check tagged T1057 should not pass when filter only contains T1082.");
        }

        [TestMethod]
        public void PassesMitreFilter_PrefixMatch_Passes()
        {
            // Filter on base technique T1552 should match sub-technique T1552.001
            winPEAS.Checks.Checks.MitreFilter.Clear();
            winPEAS.Checks.Checks.MitreFilter.Add("T1552");
            Assert.IsTrue(InvokePassesMitreFilter(new[] { "T1552.001" }),
                "Filter on T1552 should match a check tagged T1552.001 (prefix match).");
            Assert.IsTrue(InvokePassesMitreFilter(new[] { "T1552.005" }),
                "Filter on T1552 should match a check tagged T1552.005 (prefix match).");
        }

        [TestMethod]
        public void PassesMitreFilter_SubtechniqueDoesNotMatchDifferentBase_Fails()
        {
            winPEAS.Checks.Checks.MitreFilter.Clear();
            winPEAS.Checks.Checks.MitreFilter.Add("T1548");
            Assert.IsFalse(InvokePassesMitreFilter(new[] { "T1552.001" }),
                "Filter on T1548 must not match T1552.001.");
        }
    }
}
