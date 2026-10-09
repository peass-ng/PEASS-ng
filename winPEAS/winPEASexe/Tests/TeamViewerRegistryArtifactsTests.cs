using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class TeamViewerRegistryArtifactsTests
    {
        [TestMethod]
        public void FindsOnlyKnownValueNamesAtFixedVendorKeys()
        {
            var visited = new List<string>();
            var artifacts = TeamViewerRegistryArtifacts.Scan((view, path) =>
            {
                visited.Add(view + ":" + path);
                if (view == RegistryView.Registry32 && path == @"SOFTWARE\TeamViewer\Version7")
                {
                    return new[] { "SecurityPasswordAES", "OtherSecret=do-not-print" };
                }
                return new string[0];
            });

            Assert.AreEqual(1, artifacts.Count);
            StringAssert.Contains(artifacts[0], @"Registry32: HKLM\SOFTWARE\TeamViewer\Version7 [SecurityPasswordAES]");
            Assert.IsFalse(artifacts[0].Contains("do-not-print"));
            Assert.AreEqual(32, visited.Count);
            CollectionAssert.Contains(visited, @"Registry32:SOFTWARE\TeamViewer\Version7");
            CollectionAssert.DoesNotContain(visited, @"Registry32:SOFTWARE\TeamViewer\Version16");
        }

        [TestMethod]
        public void MissingAndInaccessibleKeysDoNotProduceArtifacts()
        {
            var artifacts = TeamViewerRegistryArtifacts.Scan((view, path) =>
            {
                if (path.EndsWith("Version7", StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException();
                }
                return null;
            });
            Assert.AreEqual(0, artifacts.Count);
        }

        [TestMethod]
        public void OutputIsCappedEvenWhenEveryKnownNameIsPresent()
        {
            int visited = 0;
            var artifacts = TeamViewerRegistryArtifacts.Scan((view, path) =>
            {
                visited++;
                return new[] { "SecurityPasswordAES", "OptionsPasswordAES", "ServerPasswordAES", "ProxyPasswordAES", "SecurityPasswordExported" };
            });
            Assert.AreEqual(12, artifacts.Count);
            Assert.IsTrue(visited <= 3);
        }
    }
}
