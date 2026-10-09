using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class IisServedRootPermissionsTests
    {
        private const string UserSid = "S-1-5-21-111-222-333-1001";
        private const string GroupSid = "S-1-5-21-111-222-333-1002";
        private const string OtherSid = "S-1-5-21-111-222-333-1003";

        private static RawSecurityDescriptor Descriptor(params GenericAce[] aces)
        {
            var acl = new RawAcl(2, aces.Length);
            foreach (GenericAce ace in aces) acl.InsertAce(acl.Count, ace);
            var owner = new SecurityIdentifier(UserSid);
            return new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent, owner, owner, null, acl);
        }

        private static CommonAce Ace(string sid, AceQualifier qualifier, int mask, AceFlags flags = AceFlags.None)
        {
            return new CommonAce(flags, qualifier, mask, new SecurityIdentifier(sid), false, null);
        }

        private static IisCreateFileAcl Evaluate(RawSecurityDescriptor descriptor, params string[] enabled)
        {
            string trustee;
            var sids = new HashSet<string>(enabled, StringComparer.OrdinalIgnoreCase);
            return IisServedRootPermissions.EvaluateCreateFileAcl(descriptor, sids, sids, out trustee);
        }

        [TestMethod]
        public void UserAndEnabledGroupCanIndicateCreateFile()
        {
            Assert.AreEqual(IisCreateFileAcl.Indicated,
                Evaluate(Descriptor(Ace(UserSid, AceQualifier.AccessAllowed, 0x2)), UserSid));
            Assert.AreEqual(IisCreateFileAcl.Indicated,
                Evaluate(Descriptor(Ace(GroupSid, AceQualifier.AccessAllowed, 0x2, AceFlags.Inherited)), UserSid, GroupSid));
            Assert.AreEqual(IisCreateFileAcl.NoMatch,
                Evaluate(Descriptor(Ace(GroupSid, AceQualifier.AccessAllowed, 0x2)), UserSid));
        }

        [TestMethod]
        public void DenyOverridesAllowAndUnrelatedOrInheritOnlyAceDoesNotGrant()
        {
            Assert.AreEqual(IisCreateFileAcl.ManualReview,
                Evaluate(Descriptor(Ace(GroupSid, AceQualifier.AccessAllowed, 0x2),
                    Ace(UserSid, AceQualifier.AccessDenied, 0x2)), UserSid, GroupSid));
            Assert.AreEqual(IisCreateFileAcl.NoMatch,
                Evaluate(Descriptor(Ace(OtherSid, AceQualifier.AccessAllowed, 0x2),
                    Ace(UserSid, AceQualifier.AccessAllowed, 0x2, AceFlags.InheritOnly)), UserSid));
            string trustee;
            Assert.AreEqual(IisCreateFileAcl.ManualReview, IisServedRootPermissions.EvaluateCreateFileAcl(
                Descriptor(Ace(UserSid, AceQualifier.AccessAllowed, 0x2),
                    Ace(GroupSid, AceQualifier.AccessDenied, 0x2)),
                new HashSet<string> { UserSid }, new HashSet<string> { UserSid, GroupSid }, out trustee));
            Assert.AreEqual(IisCreateFileAcl.Denied,
                Evaluate(Descriptor(Ace(UserSid, AceQualifier.AccessDenied, 0x2)), UserSid));
        }

        [TestMethod]
        public void DirectoryCreationAndReadDoNotImplyFileCreation()
        {
            Assert.AreEqual(IisCreateFileAcl.NoMatch,
                Evaluate(Descriptor(Ace(UserSid, AceQualifier.AccessAllowed, 0x4 | 0x1)), UserSid));
            Assert.AreEqual(IisCreateFileAcl.Indicated,
                Evaluate(Descriptor(Ace(UserSid, AceQualifier.AccessAllowed, unchecked((int)0x40000000))), UserSid));
            Assert.AreEqual(IisCreateFileAcl.ManualReview, Evaluate(null, UserSid));
        }

        [TestMethod]
        public void MatchingCallbackAndObjectAcesRequireManualReview()
        {
            foreach (var qualifier in new[] { AceQualifier.AccessAllowed, AceQualifier.AccessDenied })
            {
                var callback = new CommonAce(AceFlags.None, qualifier, 0x2,
                    new SecurityIdentifier(UserSid), true, null);
                Assert.AreEqual(IisCreateFileAcl.ManualReview, Evaluate(Descriptor(callback), UserSid));
                var scoped = new ObjectAce(AceFlags.None, qualifier, 0x2,
                    new SecurityIdentifier(UserSid), ObjectAceFlags.ObjectAceTypePresent,
                    Guid.NewGuid(), Guid.Empty, false, null);
                Assert.AreEqual(IisCreateFileAcl.ManualReview, Evaluate(Descriptor(scoped), UserSid));
                Assert.AreEqual(IisCreateFileAcl.ManualReview,
                    Evaluate(Descriptor(Ace(UserSid, AceQualifier.AccessDenied, 0x2), callback), UserSid));
            }
        }

        [TestMethod]
        public void UnrelatedAndInheritOnlyCallbackAcesDoNotAffectAssessment()
        {
            var unrelated = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 0x2,
                new SecurityIdentifier(OtherSid), true, null);
            var inheritOnly = new CommonAce(AceFlags.InheritOnly, AceQualifier.AccessDenied, 0x2,
                new SecurityIdentifier(UserSid), true, null);
            Assert.AreEqual(IisCreateFileAcl.NoMatch, Evaluate(Descriptor(unrelated, inheritOnly), UserSid));
            Assert.AreEqual(IisCreateFileAcl.Indicated,
                Evaluate(Descriptor(Ace(UserSid, AceQualifier.AccessAllowed, 0x2), unrelated, inheritOnly), UserSid));
        }

        private static XDocument Config(int activeCount, bool handler, bool disabled = false)
        {
            var sites = new XElement("sites", new XElement("applicationDefaults", new XAttribute("applicationPool", "ExamplePool")));
            for (int i = 0; i < activeCount; i++)
                sites.Add(new XElement("site", new XAttribute("name", "Site" + i),
                    new XAttribute("serverAutoStart", disabled ? "false" : "true"),
                    new XElement("bindings", new XElement("binding", new XAttribute("protocol", "http"))),
                    new XElement("application", new XAttribute("path", "/"),
                        new XElement("virtualDirectory", new XAttribute("path", "/"),
                            new XAttribute("physicalPath", @"C:\sites\site" + i)))));
            var config = new XElement("configuration", new XElement("system.applicationHost", sites));
            if (handler)
                config.Add(new XElement("system.webServer", new XElement("handlers", new XAttribute("accessPolicy", "Read,Script"),
                    new XElement("add", new XAttribute("path", "*.aspx"),
                        new XAttribute("modules", "ManagedPipelineHandler"),
                        new XAttribute("type", "System.Web.UI.PageHandlerFactory")))));
            return new XDocument(config);
        }

        [TestMethod]
        public void UninspectedMappedRootsRemainUnknown()
        {
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, Config(1, false), Stopwatch.StartNew());
            Assert.AreEqual(1, report.Roots.Count);
            Assert.AreEqual(IisCreateFileAcl.ManualReview, report.Roots[0].CreateFileAcl);
            Assert.AreEqual("Physical root ACL not inspected", report.Roots[0].Reason);
        }

        [TestMethod]
        public void ActiveMappedRootsAndAspxEvidenceAreBounded()
        {
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, Config(40, true), Stopwatch.StartNew());
            Assert.AreEqual(IisServedRootPermissions.MaxRoots, report.Roots.Count);
            Assert.IsTrue(report.LimitReached);
            Assert.IsTrue(report.Roots.All(r => r.Configured && r.AspxHandlerConfigured && r.Pool == "ExamplePool"));
            Assert.AreEqual(0, report.Roots.Select(r => r.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() - report.Roots.Count);
        }

        [TestMethod]
        public void PoolIdentityUsesExplicitValueThenConfiguredDefault()
        {
            XDocument config = Config(1, true);
            XElement host = config.Root.Element("system.applicationHost");
            host.Add(new XElement("applicationPools",
                new XElement("add", new XAttribute("name", "ExamplePool"),
                    new XElement("processModel", new XAttribute("identityType", "NetworkService"))),
                new XElement("applicationPoolDefaults",
                    new XElement("processModel", new XAttribute("identityType", "SpecificUser")))));
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.NetworkService, report.Roots.Single().PoolIdentity);
            Assert.IsTrue(IisServedRootPermissions.DescribePoolIdentity(report.Roots.Single().PoolIdentity)
                .Contains("computer account"));

            host.Element("applicationPools").Element("add").Element("processModel").Remove();
            host.Element("applicationPools").Element("applicationPoolDefaults").Element("processModel")
                .SetAttributeValue("password", "fixture-secret");
            report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.SpecificUser, report.Roots.Single().PoolIdentity);
            string description = IisServedRootPermissions.DescribePoolIdentity(report.Roots.Single().PoolIdentity);
            Assert.IsTrue(description.Contains("credentials suppressed"));
            Assert.IsFalse(description.Contains("fixture-secret"));
        }

        [TestMethod]
        public void PoolIdentityRemainsUnknownWithoutDefinitionOrExplicitDefault()
        {
            XDocument config = Config(1, false);
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.Unknown, report.Roots.Single().PoolIdentity);

            XElement host = config.Root.Element("system.applicationHost");
            host.Add(new XElement("applicationPools", new XElement("add", new XAttribute("name", "OtherPool"),
                new XElement("processModel", new XAttribute("identityType", "ApplicationPoolIdentity")))));
            report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.Unknown, report.Roots.Single().PoolIdentity);
            Assert.IsTrue(IisServedRootPermissions.DescribePoolIdentity(report.Roots.Single().PoolIdentity)
                .Contains("no network principal inferred"));
        }

        [TestMethod]
        public void PoolIdentityVocabularyAndPoolCountAreBounded()
        {
            Assert.AreEqual(IisPoolIdentity.ApplicationPoolIdentity,
                IisServedRootPermissions.ParsePoolIdentity("4"));
            Assert.AreEqual(IisPoolIdentity.LocalSystem,
                IisServedRootPermissions.ParsePoolIdentity("LocalSystem"));
            Assert.AreEqual(IisPoolIdentity.LocalService,
                IisServedRootPermissions.ParsePoolIdentity("1"));
            Assert.AreEqual(IisPoolIdentity.Unknown,
                IisServedRootPermissions.ParsePoolIdentity("UnexpectedUserPassword"));

            XDocument config = Config(1, false);
            XElement host = config.Root.Element("system.applicationHost");
            XElement pools = new XElement("applicationPools");
            for (int i = 0; i < IisServedRootPermissions.MaxPoolEntries; i++)
                pools.Add(new XElement("add", new XAttribute("name", "Pool" + i),
                    new XElement("processModel", new XAttribute("identityType", "NetworkService"))));
            pools.Add(new XElement("add", new XAttribute("name", "ExamplePool"),
                new XElement("processModel", new XAttribute("identityType", "ApplicationPoolIdentity"))));
            host.Add(pools);
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.Unknown, report.Roots.Single().PoolIdentity);
        }

        [TestMethod]
        public void RemovedOrClearedPoolsDoNotRetainStaleIdentity()
        {
            XDocument config = Config(1, false);
            XElement host = config.Root.Element("system.applicationHost");
            XElement pools = new XElement("applicationPools",
                new XElement("add", new XAttribute("name", "ExamplePool"),
                    new XElement("processModel", new XAttribute("identityType", "NetworkService"))),
                new XElement("remove", new XAttribute("name", "ExamplePool")));
            host.Add(pools);
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.Unknown, report.Roots.Single().PoolIdentity);

            pools.Element("remove").Remove();
            pools.Add(new XElement("clear"));
            report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.Unknown, report.Roots.Single().PoolIdentity);

            pools.Add(new XElement("add", new XAttribute("name", "ExamplePool"),
                new XElement("processModel", new XAttribute("identityType", "ApplicationPoolIdentity"))));
            report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisPoolIdentity.ApplicationPoolIdentity, report.Roots.Single().PoolIdentity);
        }

        [TestMethod]
        public void ManualStartSiteRemainsVisibleAndMissingHandlerDoesNotClaimExecution()
        {
            var disabled = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(disabled, Config(1, true, true), Stopwatch.StartNew());
            Assert.AreEqual(1, disabled.Roots.Count);
            Assert.IsFalse(disabled.Roots[0].AutoStartConfigured);
            var noHandler = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(noHandler, Config(1, false), Stopwatch.StartNew());
            Assert.AreEqual(1, noHandler.Roots.Count);
            Assert.IsFalse(noHandler.Roots[0].AspxHandlerConfigured);
            var missing = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(missing, new XDocument(new XElement("configuration")), Stopwatch.StartNew());
            Assert.AreEqual(0, missing.Roots.Count);
            var overridden = Config(1, true);
            overridden.Root.Add(new XElement("location", new XAttribute("path", "Site0"),
                new XElement("system.webServer", new XElement("handlers", new XElement("clear")))));
            var overriddenReport = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(overriddenReport, overridden, Stopwatch.StartNew());
            Assert.IsFalse(overriddenReport.Roots[0].AspxHandlerConfigured);
        }

        [TestMethod]
        public void AutoStartSiteIsInspectedBeforeManualStartSitesAtTheCap()
        {
            XDocument config = Config(33, false, true);
            config.Descendants("site").Last().SetAttributeValue("serverAutoStart", "true");
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(IisServedRootPermissions.MaxRoots, report.Roots.Count);
            Assert.IsTrue(report.Roots.Any(r => r.Site == "Site32" && r.AutoStartConfigured));
            Assert.IsTrue(report.LimitReached);
        }

        [TestMethod]
        public void UnsafePhysicalPathsAreRejected()
        {
            Assert.IsNull(IisServedRootPermissions.NormalizeLocalPath(@"\\server\share\site"));
            Assert.IsNull(IisServedRootPermissions.NormalizeLocalPath(@"%UNRESOLVED_IIS_ROOT%\site"));
            Assert.IsNull(IisServedRootPermissions.NormalizeLocalPath(@"relative\site"));
        }

        [TestMethod]
        public void DefaultPathIsMappedOnlyWhenConfigurationNamesIt()
        {
            XDocument config = Config(1, false);
            config.Descendants("virtualDirectory").First().SetAttributeValue("physicalPath", @"C:\inetpub\wwwroot");
            var report = new IisServedRootReport();
            IisServedRootPermissions.AddConfiguredRoots(report, config, Stopwatch.StartNew());
            Assert.AreEqual(1, report.Roots.Count);
            Assert.IsTrue(report.Roots[0].Configured);
            Assert.AreEqual(@"C:\inetpub\wwwroot", report.Roots[0].Path);
        }

        private static XDocument CertSrvConfig(bool application = true)
        {
            XDocument config = Config(1, false);
            XElement site = config.Descendants("site").Single();
            if (application)
                site.Add(new XElement("application", new XAttribute("path", "/CertSrv"),
                    new XElement("virtualDirectory", new XAttribute("path", "/"),
                        new XAttribute("physicalPath", @"C:\Windows\System32\CertSrv"))));
            else
                site.Descendants("application").Single().Add(new XElement("virtualDirectory",
                    new XAttribute("path", "/CertSrv"),
                    new XAttribute("physicalPath", @"C:\Windows\System32\CertSrv")));
            return config;
        }

        private static void AddCertSrvLocation(XDocument config, string path, string enabled,
            string epa, string ssl)
        {
            var windowsAuth = new XElement("windowsAuthentication", new XAttribute("enabled", enabled));
            if (epa != null) windowsAuth.Add(new XElement("extendedProtection", new XAttribute("tokenChecking", epa)));
            var security = new XElement("security", new XElement("authentication", windowsAuth));
            if (ssl != null) security.Add(new XElement("access", new XAttribute("sslFlags", ssl)));
            config.Root.Add(new XElement("location", new XAttribute("path", path),
                new XElement("system.webServer", security)));
        }

        private static IisServedRootReport AssessCertSrv(XDocument config)
        {
            var report = new IisServedRootReport();
            IisAdcsWebEnrollment.Assess(report, config, Stopwatch.StartNew());
            return report;
        }

        [TestMethod]
        public void CertSrvHttpNtlmWithoutRequiredProtectionIsOnlyAConfigurationCandidate()
        {
            XDocument config = CertSrvConfig();
            AddCertSrvLocation(config, "Site0/CertSrv", "true", "None", "None");
            var report = AssessCertSrv(config);
            Assert.AreEqual(1, report.AdcsWebEnrollment.Count);
            var finding = report.AdcsWebEnrollment.Single();
            Assert.IsTrue(finding.Candidate);
            Assert.AreEqual("Site0", finding.Site);
            Assert.IsTrue(finding.HttpBinding);
            Assert.IsFalse(report.LimitReached);
        }

        [TestMethod]
        public void RequireSslOrEpaRemovesTheHttpEsc8ConfigurationLead()
        {
            XDocument config = CertSrvConfig(false);
            AddCertSrvLocation(config, "Site0", "true", "Allow", "None");
            AddCertSrvLocation(config, "Site0/CertSrv", "true", "Require", "Ssl");
            var finding = AssessCertSrv(config).AdcsWebEnrollment.Single();
            Assert.IsFalse(finding.Candidate);
            Assert.AreEqual("Require", finding.EpaTokenChecking);
            Assert.AreEqual(true, finding.RequireSsl);
            Assert.AreEqual(true, finding.WindowsAuthentication);
        }

        [TestMethod]
        public void CertificateRequestFileOverrideKeepsEffectivePostureUnknown()
        {
            XDocument config = CertSrvConfig();
            AddCertSrvLocation(config, "Site0/CertSrv", "true", "Allow", "None");
            AddCertSrvLocation(config, "Site0/CertSrv/certfnsh.asp", "false", null, "Ssl");
            var finding = AssessCertSrv(config).AdcsWebEnrollment.Single();
            Assert.IsTrue(finding.FileLevelOverride);
            Assert.IsFalse(finding.Candidate);
        }

        [TestMethod]
        public void GlobalLocationProtectionIsInheritedByCertSrv()
        {
            XDocument config = CertSrvConfig();
            AddCertSrvLocation(config, "", "true", "Require", "Ssl");
            var finding = AssessCertSrv(config).AdcsWebEnrollment.Single();
            Assert.AreEqual("Require", finding.EpaTokenChecking);
            Assert.AreEqual(true, finding.RequireSsl);
            Assert.IsFalse(finding.Candidate);
        }

        [TestMethod]
        public void NoNtlmProviderOrRequiredEpaIsNotAnEsc8Candidate()
        {
            XDocument config = CertSrvConfig();
            config.Descendants("binding").Single().SetAttributeValue("protocol", "https");
            AddCertSrvLocation(config, "Site0/CertSrv", "true", "Require", "Ssl");
            Assert.IsFalse(AssessCertSrv(config).AdcsWebEnrollment.Single().Candidate);
            config.Descendants("binding").Single().SetAttributeValue("protocol", "http");
            config.Descendants("extendedProtection").Single().SetAttributeValue("tokenChecking", "None");
            config.Descendants("access").Single().SetAttributeValue("sslFlags", "None");
            XElement auth = config.Descendants("windowsAuthentication").Single();
            auth.Add(new XElement("providers", new XElement("clear"),
                new XElement("add", new XAttribute("value", "Negotiate:Kerberos"))));
            var finding = AssessCertSrv(config).AdcsWebEnrollment.Single();
            Assert.AreEqual(false, finding.NtlmProvider);
            Assert.IsFalse(finding.Candidate);
        }

        [TestMethod]
        public void HttpsWithoutRequiredEpaIsStillAnEsc8ConfigurationCandidate()
        {
            XDocument config = CertSrvConfig();
            config.Descendants("binding").Single().SetAttributeValue("protocol", "https");
            AddCertSrvLocation(config, "Site0/CertSrv", "true", "Allow", "Ssl");
            var finding = AssessCertSrv(config).AdcsWebEnrollment.Single();
            Assert.IsFalse(finding.HttpBinding);
            Assert.IsTrue(finding.HttpsBinding);
            Assert.IsTrue(finding.Candidate);
        }

        [TestMethod]
        public void NegotiateProviderCanStillAcceptNtlm()
        {
            XDocument config = CertSrvConfig();
            AddCertSrvLocation(config, "Site0/CertSrv", "true", "None", "None");
            config.Descendants("windowsAuthentication").Single().Add(new XElement("providers",
                new XElement("clear"), new XElement("add", new XAttribute("value", "Negotiate"))));
            var finding = AssessCertSrv(config).AdcsWebEnrollment.Single();
            Assert.AreEqual(true, finding.NtlmProvider);
            Assert.IsTrue(finding.Candidate);
        }

        [TestMethod]
        public void UnrelatedSitesAndBoundedSiteInventoryDoNotClaimCertSrvExposure()
        {
            Assert.AreEqual(0, AssessCertSrv(Config(1, false)).AdcsWebEnrollment.Count);
            XDocument manySites = Config(IisAdcsWebEnrollment.MaxSites + 1, false);
            var report = AssessCertSrv(manySites);
            Assert.IsTrue(report.LimitReached);
            Assert.AreEqual(0, report.AdcsWebEnrollment.Count);
        }
    }
}
