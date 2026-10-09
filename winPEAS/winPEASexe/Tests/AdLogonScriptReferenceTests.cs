using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class AdLogonScriptReferenceTests
    {
        [TestMethod]
        public void ClassifiesReferencesWithoutOpeningPaths()
        {
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.Missing,
                ActiveDirectoryInfo.ClassifyLogonScriptPath(null));
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.Relative,
                ActiveDirectoryInfo.ClassifyLogonScriptPath(@"logon\startup.cmd"));
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.AbsoluteOrUnc,
                ActiveDirectoryInfo.ClassifyLogonScriptPath(@"\\domain.example\NETLOGON\login.cmd"));
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.AbsoluteOrUnc,
                ActiveDirectoryInfo.ClassifyLogonScriptPath(@"C:\scripts\login.cmd"));
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.UnsafeRelative,
                ActiveDirectoryInfo.ClassifyLogonScriptPath(@"C:login.cmd"));
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.UnsafeRelative,
                ActiveDirectoryInfo.ClassifyLogonScriptPath(@"..\login.cmd"));
            Assert.AreEqual(ActiveDirectoryInfo.LogonScriptPathKind.UnsafeRelative,
                ActiveDirectoryInfo.ClassifyLogonScriptPath("scripts//login.cmd"));
        }

        [TestMethod]
        public void DeduplicatesPathsAndSanitizesDisplayOnly()
        {
            var summary = ActiveDirectoryInfo.SummarizeLogonScriptReferences(new[]
            {
                new ActiveDirectoryInfo.LogonScriptReference { Account = "alice", Path = @"login.cmd" },
                new ActiveDirectoryInfo.LogonScriptReference { Account = "bob", Path = @"LOGIN.CMD" },
                new ActiveDirectoryInfo.LogonScriptReference { Account = "eve\nspoof", Path = @"\\other\share\go.cmd" },
                new ActiveDirectoryInfo.LogonScriptReference { Account = "plain" }
            }, false);

            Assert.AreEqual(4, summary.InspectedUsers);
            Assert.AreEqual(3, summary.ReferencedUsers);
            Assert.AreEqual(2, summary.UniquePaths);
            Assert.AreEqual(2, summary.DisplayLines.Count);
            Assert.IsTrue(summary.DisplayLines.Any(line => line.Contains("alice, bob") &&
                line.Contains("relative NETLOGON reference")));
            Assert.IsTrue(summary.DisplayLines.Any(line => line.Contains("eve?spoof") &&
                line.Contains("no path access attempted")));
            Assert.IsFalse(summary.Truncated);
        }

        [TestMethod]
        public void CapsSampleAndDisplayAndKeepsIncompleteState()
        {
            var rows = new List<ActiveDirectoryInfo.LogonScriptReference>();
            for (int i = 0; i < 121; i++)
                rows.Add(new ActiveDirectoryInfo.LogonScriptReference
                {
                    Account = "user" + i,
                    Path = "script" + i + ".cmd"
                });

            var summary = ActiveDirectoryInfo.SummarizeLogonScriptReferences(rows, false);
            Assert.AreEqual(120, summary.InspectedUsers);
            Assert.AreEqual(120, summary.UniquePaths);
            Assert.AreEqual(12, summary.DisplayLines.Count);
            Assert.IsTrue(summary.Truncated);
            Assert.IsTrue(ActiveDirectoryInfo.SummarizeLogonScriptReferences(
                new ActiveDirectoryInfo.LogonScriptReference[0], true).Truncated);
        }
    }
}
