using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class UnconstrainedDelegationInventoryTests
    {
        [TestMethod]
        public void OnlyFlaggedNonDcComputersAreCandidates()
        {
            Assert.IsTrue(ActiveDirectoryInfo.IsNonDcUnconstrainedDelegation(0x80000 | 0x1000));
            Assert.IsFalse(ActiveDirectoryInfo.IsNonDcUnconstrainedDelegation(0x1000));
            Assert.IsFalse(ActiveDirectoryInfo.IsNonDcUnconstrainedDelegation(0x80000 | 0x2000));
            Assert.IsFalse(ActiveDirectoryInfo.IsNonDcUnconstrainedDelegation(null));

            var summary = ActiveDirectoryInfo.SummarizeDelegationInventory(new[]
            {
                new ActiveDirectoryInfo.DelegationComputer { Name = "WORKSTATION$", UserAccountControl = 0x80000 | 0x1000 },
                new ActiveDirectoryInfo.DelegationComputer { Name = "DC$", UserAccountControl = 0x80000 | 0x2000 },
                new ActiveDirectoryInfo.DelegationComputer { Name = "PLAIN$", UserAccountControl = 0x1000 },
                new ActiveDirectoryInfo.DelegationComputer { Name = "UNKNOWN$" }
            }, false);
            Assert.AreEqual(4, summary.Inspected);
            Assert.AreEqual(1, summary.Candidates);
            Assert.AreEqual(1, summary.UnknownFlags);
            CollectionAssert.AreEqual(new[] { "WORKSTATION$" }, summary.DisplayNames);
        }

        [TestMethod]
        public void SampleAndDisplayAreCapped()
        {
            var rows = new List<ActiveDirectoryInfo.DelegationComputer>();
            for (int i = 0; i < 121; i++)
                rows.Add(new ActiveDirectoryInfo.DelegationComputer
                {
                    Name = "COMPUTER" + i + "$",
                    UserAccountControl = 0x80000
                });

            var summary = ActiveDirectoryInfo.SummarizeDelegationInventory(rows, false);
            Assert.AreEqual(120, summary.Inspected);
            Assert.AreEqual(120, summary.Candidates);
            Assert.AreEqual(40, summary.DisplayNames.Count);
            Assert.IsTrue(summary.Truncated);
            Assert.IsFalse(summary.LdapError);
        }

        [TestMethod]
        public void LdapFailureMarksObservedRowsPartial()
        {
            var summary = ActiveDirectoryInfo.SummarizeDelegationInventory(new[]
            {
                new ActiveDirectoryInfo.DelegationComputer { Name = "OBSERVED$", UserAccountControl = 0x80000 }
            }, true);
            Assert.AreEqual(1, summary.Candidates);
            Assert.IsTrue(summary.LdapError);
            Assert.IsFalse(summary.Truncated);
        }
    }
}
