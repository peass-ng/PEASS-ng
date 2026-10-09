using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ApplicationInfo;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class HMailAndInstalledAppsTests
    {
        [TestMethod]
        public void IncludesX86ProgramFilesAndReadsTheWowUninstallKey()
        {
            string nativeRoot = @"C:\Program Files";
            string x86Root = @"C:\Program Files (x86)";
            string x86App = x86Root + @"\hMailServer";
            string wowApp = @"D:\Applications\Example";
            var requested = new List<string>();
            var result = InstalledApps.GetInstalledAppsPermsCore(
                new[] { nativeRoot, x86Root },
                root =>
                {
                    var found = new SortedDictionary<string, Dictionary<string, string>>();
                    if (root == x86Root) found[x86App] = new Dictionary<string, string> { { x86App, "" } };
                    return found;
                },
                path => path.Contains("WOW6432Node") ? new[] { "Example" } : new string[0],
                path =>
                {
                    requested.Add(path);
                    return path.Contains("WOW6432Node") ? wowApp : "";
                },
                path => path == wowApp,
                path => new Dictionary<string, string> { { path, "read" } });

            Assert.IsTrue(result.ContainsKey(x86App));
            Assert.IsTrue(result.ContainsKey(wowApp));
            CollectionAssert.AreEqual(new[]
            {
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Example"
            }, requested);
        }

        [TestMethod]
        public void ParsesOnlyRelevantConfigurationKeysAndNeverRetainsPasswordValue()
        {
            string secret = "fixture-secret-should-not-be-retained";
            var settings = HMailServerExposure.ParseDatabaseSection(
                "[Other]\nPassword=wrong\n[database]\ntYpE=MSSQLCE\nInternal=1\n" +
                "PasswordEncryption=1\nPassword=" + secret + "\n[Directories]\nDatabaseFolder=Database\n" +
                "[Security]\nAdministratorPassword=" + secret + "\n[Other]\nType=ignored");
            Assert.AreEqual("MSSQLCE", settings.Type);
            Assert.AreEqual("Database", settings.DatabaseFolder);
            Assert.IsTrue(settings.PasswordPresent);
            Assert.IsTrue(settings.AdministratorPasswordPresent);
            Assert.IsFalse(string.Join("|", settings.Type, settings.Internal,
                settings.PasswordEncryption, settings.DatabaseFolder).Contains(secret));
        }

        [TestMethod]
        public void ProgramDataLayoutReportsAdministratorHashWithoutSqlCeDatabase()
        {
            string parent = Path.Combine(Path.GetTempPath(), "hmail-data-fixture-" + Guid.NewGuid().ToString("N"));
            string install = Path.Combine(parent, "hMailServer");
            try
            {
                Directory.CreateDirectory(install);
                string ini = Path.Combine(install, "hMailServer.ini");
                File.WriteAllText(ini, "[Security]\nAdministratorPassword=fixture-hash\n[Database]\nType=MYSQL\n");
                var result = HMailServerExposure.ProbeInstall(install, true);
                Assert.AreEqual(ini, result.IniPath);
                Assert.AreEqual(HMailReadState.Accessible, result.IniState);
                Assert.IsTrue(result.AdministratorPasswordPresent);
                Assert.IsFalse(result.IsSqlCe);
                Assert.AreEqual(HMailReadState.Unknown, result.DatabaseState);

                File.WriteAllText(ini, "[Other]\nAdministratorPassword=ignored\n[Security]\nAdministratorPassword=\n");
                result = HMailServerExposure.ProbeInstall(install, true);
                Assert.IsFalse(result.AdministratorPasswordPresent);
            }
            finally
            {
                if (Directory.Exists(parent)) Directory.Delete(parent, true);
            }
        }

        [TestMethod]
        public void DatabaseFolderComesOnlyFromDirectoriesSection()
        {
            var settings = HMailServerExposure.ParseDatabaseSection(
                "[Directories]\nDatabaseFolder=CustomDatabase\nPassword=ignored\n" +
                "[Database]\nType=MSSQLCE\nDatabaseFolder=wrong\n");
            Assert.AreEqual("CustomDatabase", settings.DatabaseFolder);
            Assert.AreEqual("MSSQLCE", settings.Type);
            Assert.IsFalse(settings.PasswordPresent);
        }

        [TestMethod]
        public void ReportsIniAndDatabaseReadabilityIndependently()
        {
            string parent = Path.Combine(Path.GetTempPath(), "hmail-fixture-" + Guid.NewGuid().ToString("N"));
            string install = Path.Combine(parent, "hMailServer");
            string bin = Path.Combine(install, "Bin");
            string database = Path.Combine(install, "Database");
            try
            {
                Directory.CreateDirectory(bin);
                File.WriteAllText(Path.Combine(bin, "hMailServer.ini"),
                    "[Database]\nType=MSSQLCE\nPasswordEncryption=1\nPassword=fixture\n");
                var iniOnly = HMailServerExposure.ProbeInstall(install);
                Assert.AreEqual(HMailReadState.Accessible, iniOnly.IniState);
                Assert.AreEqual(HMailReadState.Unknown, iniOnly.DatabaseState);

                Directory.CreateDirectory(database);
                File.WriteAllBytes(Path.Combine(database, "hMailServer.sdf"), new byte[] { 1 });
                var both = HMailServerExposure.ProbeInstall(install);
                Assert.AreEqual(HMailReadState.Accessible, both.IniState);
                Assert.AreEqual(HMailReadState.Accessible, both.DatabaseState);
                Assert.IsTrue(both.EncryptedPasswordPresent);

                string customDatabase = Path.Combine(install, "CustomDatabase");
                Directory.CreateDirectory(customDatabase);
                string customFile = Path.Combine(customDatabase, "hMailServer.sdf");
                File.WriteAllBytes(customFile, new byte[] { 1 });
                File.WriteAllText(Path.Combine(bin, "hMailServer.ini"),
                    "[Database]\nType=MSSQLCE\n[Directories]\nDatabaseFolder=" + customDatabase + "\n");
                var relocated = HMailServerExposure.ProbeInstall(install);
                Assert.AreEqual(customFile, relocated.DatabasePath);
                Assert.AreEqual(HMailReadState.Accessible, relocated.DatabaseState);

                File.WriteAllText(Path.Combine(bin, "hMailServer.ini"),
                    "[Database]\nType=MSSQLCE\n[Directories]\nDatabaseFolder=../outside\n");
                var rejected = HMailServerExposure.ProbeInstall(install);
                Assert.AreEqual(HMailReadState.Accessible, rejected.IniState);
                Assert.AreEqual(HMailReadState.Unknown, rejected.DatabaseState);
                Assert.IsNull(rejected.DatabasePath);
            }
            finally
            {
                if (Directory.Exists(parent)) Directory.Delete(parent, true);
            }
        }

        [TestMethod]
        public void OversizeIniIsUnknownAndDoesNotReadDatabase()
        {
            string parent = Path.Combine(Path.GetTempPath(), "hmail-fixture-" + Guid.NewGuid().ToString("N"));
            string install = Path.Combine(parent, "hMailServer");
            try
            {
                Directory.CreateDirectory(Path.Combine(install, "Bin"));
                File.WriteAllText(Path.Combine(install, "Bin", "hMailServer.ini"),
                    "[Database]\nType=MSSQLCE\n" + new string('X', 17000));
                var result = HMailServerExposure.ProbeInstall(install);
                Assert.AreEqual(HMailReadState.Unknown, result.IniState);
                Assert.AreEqual(HMailReadState.Unknown, result.DatabaseState);
            }
            finally
            {
                if (Directory.Exists(parent)) Directory.Delete(parent, true);
            }
        }
    }
}
