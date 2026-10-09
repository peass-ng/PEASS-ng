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
    }
}
