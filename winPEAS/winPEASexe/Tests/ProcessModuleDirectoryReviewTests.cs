using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ProcessInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class ProcessModuleDirectoryReviewTests
    {
        private static Dictionary<string, string> Process(string path, string owner = "other") =>
            new Dictionary<string, string> {
                ["Name"] = "worker", ["Owner"] = owner, ["ExecutablePath"] = path
            };

        [TestMethod]
        public void RequiresDifferentOwnerAndCreateFileAllowOnExistingModuleDirectory()
        {
            string root = Path.Combine(Path.GetTempPath(), "peas-module-review-" + Guid.NewGuid().ToString("N"));
            string modules = Path.Combine(root, "Libraries");
            Directory.CreateDirectory(modules);
            try
            {
                string exe = Path.Combine(root, "worker.exe");
                var processes = new List<Dictionary<string, string>>();
                for (int i = 0; i < 24; i++)
                    processes.Add(Process(Path.Combine(root, "stale" + i, "worker.exe")));
                processes.Add(Process(exe));
                processes.Add(Process(exe, "current"));
                int calls = 0;
                var review = ProcessModuleDirectoryReview.FindLeads(processes, "current", path => {
                    calls++;
                    Assert.AreEqual(modules, path);
                    return new[] { "Users [Allow: WriteData/CreateFiles]" };
                });
                Assert.AreEqual(1, calls);
                Assert.AreEqual(1, review.Leads.Count);
                Assert.AreEqual(modules, review.Leads[0].Directory);
                Assert.IsFalse(review.Truncated);

                var sameOwner = ProcessModuleDirectoryReview.FindLeads(
                    new[] { Process(exe, "DOMAIN\\current") }, "current",
                    _ => throw new AssertFailedException("Same-principal path should not be checked"));
                Assert.AreEqual(0, sameOwner.Leads.Count);
            }
            finally { Directory.Delete(root, true); }
        }

        [TestMethod]
        public void RejectsNonCreatingOrDeniedAces()
        {
            Assert.IsTrue(ProcessModuleDirectoryReview.IsEligibleModuleDirectoryAttributes(FileAttributes.Directory));
            Assert.IsFalse(ProcessModuleDirectoryReview.IsEligibleModuleDirectoryAttributes(
                FileAttributes.Directory | FileAttributes.ReparsePoint));
            Assert.IsFalse(ProcessModuleDirectoryReview.IsEligibleModuleDirectoryAttributes(FileAttributes.Normal));
            Assert.IsTrue(ProcessModuleDirectoryReview.HasCreateFileAllowWithoutDeny(
                "Users [Allow: Modify WriteData/CreateFiles]"));
            Assert.IsFalse(ProcessModuleDirectoryReview.HasCreateFileAllowWithoutDeny(
                "Users [Allow: WriteAttributes]"));
            Assert.IsFalse(ProcessModuleDirectoryReview.HasCreateFileAllowWithoutDeny(
                "Users [Allow: Read]"));
            Assert.IsFalse(ProcessModuleDirectoryReview.HasCreateFileAllowWithoutDeny(
                "Users [Allow: WriteData/CreateFiles] [Deny: WriteData/CreateFiles]"));
        }

        [TestMethod]
        public void RejectsAmbiguousPathsAndCapsInputInventory()
        {
            foreach (string path in new[] {
                @"\\server\share\Libraries", @"C:\App\..\Other\Libraries",
                @"C:\App\.\Libraries", @"C:\App\\Libraries",
                @"C:\App\Libraries:stream", @"C:\" + new string('a', 510)
            }) Assert.IsFalse(ProcessModuleDirectoryReview.IsFixedUnreparsedDirectory(path));

            var processes = new List<Dictionary<string, string>>();
            for (int i = 0; i < 513; i++) processes.Add(Process(@"C:\Missing\worker.exe"));
            var review = ProcessModuleDirectoryReview.FindLeads(processes, "current",
                _ => throw new AssertFailedException("Missing paths must not be inspected"));
            Assert.IsTrue(review.Truncated);
            Assert.AreEqual(0, review.Leads.Count);
        }
    }
}
