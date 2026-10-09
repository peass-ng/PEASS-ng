using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Info.SystemInfo.Ntlm;

namespace winPEAS.Tests
{
    [TestClass]
    public class DcLdapPolicyTests
    {
        private static DcLdapPolicyInfo Policy(
            LocalDomainRole role, DomainControllerGeneration generation,
            LdapPolicyReadState signingState, uint? signing,
            LdapPolicyReadState bindingState, uint? binding)
        {
            return new DcLdapPolicyInfo
            {
                Role = role,
                Generation = generation,
                SigningReadState = signingState,
                SigningValue = signing,
                ChannelBindingReadState = bindingState,
                ChannelBindingValue = binding
            };
        }

        [TestMethod]
        public void ExplicitSigningAndBindingValuesKeepProtocolMeaning()
        {
            var none = Policy(LocalDomainRole.DomainController, DomainControllerGeneration.Before2025,
                LdapPolicyReadState.Present, 1, LdapPolicyReadState.Present, 2);
            Assert.AreEqual(LdapSigningStatus.None, none.SigningStatus);
            Assert.AreEqual(LdapsChannelBindingStatus.Always, none.ChannelBindingStatus);
            Assert.IsTrue(none.HasObservedRelayCondition);

            var partial = Policy(LocalDomainRole.DomainController, DomainControllerGeneration.Before2025,
                LdapPolicyReadState.Present, 2, LdapPolicyReadState.Present, 1);
            Assert.AreEqual(LdapSigningStatus.RequireSigning, partial.SigningStatus);
            Assert.AreEqual(LdapsChannelBindingStatus.WhenSupported, partial.ChannelBindingStatus);
            Assert.IsFalse(partial.HasObservedRelayCondition);

            partial.ChannelBindingValue = 2;
            Assert.IsFalse(partial.HasObservedRelayCondition);

            partial.ChannelBindingValue = 0;
            Assert.AreEqual(LdapsChannelBindingStatus.Never, partial.ChannelBindingStatus);
            Assert.IsTrue(partial.HasObservedRelayCondition);
        }

        [TestMethod]
        public void MissingSigningOnlyUsesOlderDcDefault()
        {
            var policy = Policy(LocalDomainRole.DomainController, DomainControllerGeneration.Before2025,
                LdapPolicyReadState.Missing, null, LdapPolicyReadState.Missing, null);
            Assert.AreEqual(LdapSigningStatus.LegacyDefault, policy.SigningStatus);
            Assert.IsTrue(policy.HasObservedRelayCondition);

            policy.Generation = DomainControllerGeneration.Server2025OrLater;
            Assert.AreEqual(LdapSigningStatus.Unknown, policy.SigningStatus);
            Assert.IsFalse(policy.HasObservedRelayCondition);

            var clientOnly = new NtlmSettingsInfo { LdapSigning = 2, DcLdapPolicy = policy };
            Assert.IsFalse(clientOnly.DcLdapPolicy.HasObservedRelayCondition);

            policy.Generation = DomainControllerGeneration.Unknown;
            Assert.AreEqual(LdapSigningStatus.Unknown, policy.SigningStatus);
            policy.Generation = DomainControllerGeneration.Before2025;
            policy.SigningReadState = LdapPolicyReadState.Error;
            Assert.AreEqual(LdapSigningStatus.Unknown, policy.SigningStatus);
            Assert.IsFalse(policy.HasObservedRelayCondition);
        }

        [TestMethod]
        public void MemberAndUnexpectedValuesStayUnknown()
        {
            var policy = Policy(LocalDomainRole.Member, DomainControllerGeneration.Before2025,
                LdapPolicyReadState.Present, 1, LdapPolicyReadState.Present, 0);
            Assert.AreEqual(LdapSigningStatus.Unknown, policy.SigningStatus);
            Assert.AreEqual(LdapsChannelBindingStatus.Unknown, policy.ChannelBindingStatus);
            Assert.IsFalse(policy.HasObservedRelayCondition);

            policy.Role = LocalDomainRole.DomainController;
            policy.SigningValue = 0;
            policy.ChannelBindingValue = 3;
            Assert.AreEqual(LdapSigningStatus.Unknown, policy.SigningStatus);
            Assert.AreEqual(LdapsChannelBindingStatus.Unknown, policy.ChannelBindingStatus);
            Assert.IsFalse(policy.HasObservedRelayCondition);
        }

        [TestMethod]
        public void QuotaClassificationDoesNotClaimCallerCanCreateAccounts()
        {
            Assert.AreEqual(ActiveDirectoryInfo.MachineAccountQuotaStatus.Zero,
                ActiveDirectoryInfo.AssessMachineAccountQuota(0));
            Assert.AreEqual(ActiveDirectoryInfo.MachineAccountQuotaStatus.Positive,
                ActiveDirectoryInfo.AssessMachineAccountQuota(10));
            Assert.AreEqual(ActiveDirectoryInfo.MachineAccountQuotaStatus.Unavailable,
                ActiveDirectoryInfo.AssessMachineAccountQuota(null));
            Assert.AreEqual(ActiveDirectoryInfo.MachineAccountQuotaStatus.Unavailable,
                ActiveDirectoryInfo.AssessMachineAccountQuota(-1));
        }

        [TestMethod]
        public void StagedComputerFlagsAreCandidatesOnlyWhenEnabledAndWorkstationTrust()
        {
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Candidate,
                ActiveDirectoryInfo.AssessStagedComputer(0x1020));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Candidate,
                ActiveDirectoryInfo.AssessStagedComputer(0x1020 | 0x10000));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Excluded,
                ActiveDirectoryInfo.AssessStagedComputer(0x1022));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Excluded,
                ActiveDirectoryInfo.AssessStagedComputer(0x2020));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Excluded,
                ActiveDirectoryInfo.AssessStagedComputer(0x1000));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Excluded,
                ActiveDirectoryInfo.AssessStagedComputer(0x20));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Unknown,
                ActiveDirectoryInfo.AssessStagedComputer(null));
            Assert.AreEqual(ActiveDirectoryInfo.StagedComputerStatus.Unknown,
                ActiveDirectoryInfo.AssessStagedComputer(-1));
        }
    }
}
