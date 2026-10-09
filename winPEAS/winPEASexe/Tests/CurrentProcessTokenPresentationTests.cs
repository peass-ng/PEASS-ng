using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.UserInfo.Token;

namespace winPEAS.Tests
{
    [TestClass]
    public class CurrentProcessTokenPresentationTests
    {
        private static CurrentProcessTokenSnapshot Snapshot(
            TokenIntegrityState integrity, TokenElevationState elevation,
            TokenGroupState administrators, TokenGroupState localAdministrator,
            bool? enableLua)
        {
            return new CurrentProcessTokenSnapshot
            {
                Integrity = integrity,
                Elevation = elevation,
                Administrators = administrators,
                LocalAdministratorAccount = localAdministrator,
                EnableLUA = enableLua
            };
        }

        [TestMethod]
        public void MediumLimitedLocalAdministratorIsTheOnlyHighlightedState()
        {
            var filtered = Snapshot(TokenIntegrityState.Medium, TokenElevationState.Limited,
                TokenGroupState.DenyOnly, TokenGroupState.DenyOnly, true);
            Assert.IsTrue(filtered.IsFilteredLocalAdminCandidate);
            StringAssert.Contains(filtered.Summary, "Administrators SID: DenyOnly");
            StringAssert.Contains(filtered.Summary, "EnableLUA: enabled");

            var elevated = Snapshot(TokenIntegrityState.High, TokenElevationState.Full,
                TokenGroupState.Enabled, TokenGroupState.Enabled, true);
            Assert.IsFalse(elevated.IsFilteredLocalAdminCandidate);
            StringAssert.Contains(elevated.Summary, "elevation type: Full");

            var standard = Snapshot(TokenIntegrityState.Medium, TokenElevationState.Default,
                TokenGroupState.Absent, TokenGroupState.Absent, true);
            Assert.IsFalse(standard.IsFilteredLocalAdminCandidate);

            // A domain account in local Administrators need not have S-1-5-114.
            var domainGroupMember = Snapshot(TokenIntegrityState.Medium, TokenElevationState.Limited,
                TokenGroupState.DenyOnly, TokenGroupState.Absent, true);
            Assert.IsTrue(domainGroupMember.IsFilteredLocalAdminCandidate);

            var noAdministratorsSid = Snapshot(TokenIntegrityState.Medium, TokenElevationState.Limited,
                TokenGroupState.Absent, TokenGroupState.Absent, true);
            Assert.IsFalse(noAdministratorsSid.IsFilteredLocalAdminCandidate);
        }

        [TestMethod]
        public void DisabledOrUnknownPolicyAndFailedTokenQueriesDoNotHighlight()
        {
            var filtered = Snapshot(TokenIntegrityState.Medium, TokenElevationState.Limited,
                TokenGroupState.DenyOnly, TokenGroupState.DenyOnly, false);
            Assert.IsFalse(filtered.IsFilteredLocalAdminCandidate);
            StringAssert.Contains(filtered.Summary, "EnableLUA: disabled");

            filtered.EnableLUA = null;
            Assert.IsFalse(filtered.IsFilteredLocalAdminCandidate);
            StringAssert.Contains(filtered.Summary, "EnableLUA: unknown");
            StringAssert.Contains(filtered.Summary, "ConsentPromptBehaviorAdmin: unknown");

            filtered.EnableLUA = true;
            filtered.Integrity = TokenIntegrityState.Unknown;
            Assert.IsFalse(filtered.IsFilteredLocalAdminCandidate);
            filtered.Integrity = TokenIntegrityState.Medium;
            filtered.Elevation = TokenElevationState.Unknown;
            Assert.IsFalse(filtered.IsFilteredLocalAdminCandidate);
            filtered.Elevation = TokenElevationState.Limited;
            filtered.Administrators = TokenGroupState.Unknown;
            Assert.IsFalse(filtered.IsFilteredLocalAdminCandidate);
            filtered.Administrators = TokenGroupState.DenyOnly;
            filtered.LocalAdministratorAccount = TokenGroupState.Unknown;
            Assert.IsTrue(filtered.IsFilteredLocalAdminCandidate);
        }

        [TestMethod]
        public void PresentationDoesNotDependOnProcessArchitecture()
        {
            var filtered = Snapshot(TokenIntegrityState.Medium, TokenElevationState.Limited,
                TokenGroupState.DenyOnly, TokenGroupState.Enabled, true);
            filtered.ConsentPromptBehaviorAdmin = 5;
            Assert.IsTrue(filtered.IsFilteredLocalAdminCandidate);
            StringAssert.Contains(filtered.Summary, "ConsentPromptBehaviorAdmin: 5");
        }
    }
}
