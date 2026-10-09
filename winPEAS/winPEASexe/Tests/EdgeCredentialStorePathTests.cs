using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class EdgeCredentialStorePathTests
    {
        [TestMethod]
        public void ExactCurrentDefaultProfilePairIsReportedAsPathsOnly()
        {
            string localAppData = Path.Combine(Path.GetTempPath(), "edge-candidate-" + Guid.NewGuid().ToString("N"));
            string userData = Path.Combine(localAppData, "Microsoft", "Edge", "User Data");
            string profile = Path.Combine(userData, "Default");
            Directory.CreateDirectory(profile);
            try
            {
                string loginData = Path.Combine(profile, "Login Data");
                string localState = Path.Combine(userData, "Local State");
                File.WriteAllText(loginData, "SENSITIVE_FIXTURE_VALUE_DO_NOT_PRINT");
                File.WriteAllText(localState, "SENSITIVE_FIXTURE_VALUE_DO_NOT_PRINT");
                Directory.CreateDirectory(Path.Combine(userData, "Profile 1"));
                File.WriteAllText(Path.Combine(userData, "Profile 1", "Login Data"), "other");

                string[] result = BrowserInfo.GetEdgeCredentialStorePaths(localAppData);
                Assert.AreEqual(2, result.Length);
                Assert.AreEqual(loginData, result[0]);
                Assert.AreEqual(localState, result[1]);
                Assert.IsFalse(string.Join(" ", result).Contains("SENSITIVE_FIXTURE_VALUE_DO_NOT_PRINT"));
                Assert.IsFalse(string.Join(" ", result).Contains("Profile 1"));
            }
            finally { Directory.Delete(localAppData, true); }
        }

        [TestMethod]
        public void MissingLoginOrUnsafeRootCannotClaimSavedLogins()
        {
            string localAppData = Path.Combine(Path.GetTempPath(), "edge-candidate-" + Guid.NewGuid().ToString("N"));
            string userData = Path.Combine(localAppData, "Microsoft", "Edge", "User Data");
            Directory.CreateDirectory(Path.Combine(userData, "Default"));
            try
            {
                File.WriteAllText(Path.Combine(userData, "Local State"), "fixture");
                string[] result = BrowserInfo.GetEdgeCredentialStorePaths(localAppData);
                Assert.IsNull(result[0]);
                Assert.AreEqual(Path.Combine(userData, "Local State"), result[1]);
            }
            finally { Directory.Delete(localAppData, true); }
            Assert.IsNull(BrowserInfo.GetEdgeCredentialStorePaths(@"relative\AppData")[0]);
            Assert.IsNull(BrowserInfo.GetEdgeCredentialStorePaths(@"\\server\share")[0]);
        }
    }
}
