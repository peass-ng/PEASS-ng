using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class SqlSetupConfigurationIndicatorTests
    {
        [TestMethod]
        public void RecognizesOnlyExpectedSetupNamesAndLocalDriveRoots()
        {
            Assert.IsTrue(SqlSetupConfigurationIndicator.IsSetupConfigName("sql-Configuration.INI"));
            Assert.IsTrue(SqlSetupConfigurationIndicator.IsSetupConfigName("CONFIGURATIONFILE.INI"));
            Assert.IsFalse(SqlSetupConfigurationIndicator.IsSetupConfigName("database.ini"));
            Assert.IsTrue(SqlSetupConfigurationIndicator.IsFixedDriveRoot(@"C:\"));
            Assert.IsFalse(SqlSetupConfigurationIndicator.IsFixedDriveRoot(@"\\server\share\"));
            Assert.IsFalse(SqlSetupConfigurationIndicator.IsFixedDriveRoot(@"C:\SQL2019\"));
        }

        [TestMethod]
        public void PopulatedFieldsAreReportedByNameOnly()
        {
            const string fixture = "[OPTIONS]\nSQLSVCACCOUNT=DOMAIN\\svc\n" +
                "SQLSVCPASSWORD=\"example-service-secret\"\n" +
                "SAPWD='example-sa-secret'\nAGTSVCPASSWORD=\"*****\"\n" +
                "; ISSVCPASSWORD=commented\nRSSVCPASSWORD=<password>\n";
            var fields = SqlSetupConfigurationIndicator.FindPopulatedCredentialKeys(fixture);
            CollectionAssert.AreEquivalent(new[] { "SQLSVCPASSWORD", "SAPWD" }, fields);
            Assert.IsFalse(string.Join(";", fields).Contains("example-"));
        }

        [TestMethod]
        public void EmptyMaskedAndPlaceholderFieldsAreNotCandidates()
        {
            string fixture = "SAPWD=\"\" ; generated\nSQLSVCPASSWORD=\"********\"\n" +
                "AGTSVCPASSWORD=REDACTED\nASSVCPASSWORD=\"<service password>\"\n";
            Assert.AreEqual(0, SqlSetupConfigurationIndicator.FindPopulatedCredentialKeys(fixture).Count);
            Assert.AreEqual(0, SqlSetupConfigurationIndicator.FindPopulatedCredentialKeys(
                "SQLSVCPASSWORD=" + new string('x', 5000)).Count);
        }

        [TestMethod]
        public void TextParsingIsLineCappedAndIgnoresLongLines()
        {
            string fixture = string.Concat(Enumerable.Repeat("IGNORED=x\n", 1000)) +
                "SAPWD=too-late\n" +
                "SQLSVCPASSWORD=" + new string('x', 5000);
            Assert.AreEqual(0, SqlSetupConfigurationIndicator.FindPopulatedCredentialKeys(fixture).Count);
            Assert.IsTrue(SqlSetupConfigurationIndicator.MaxReadBytes <= 65536);
            Assert.IsTrue(SqlSetupConfigurationIndicator.MaxRootDirectories <= 12);
            Assert.IsTrue(SqlSetupConfigurationIndicator.MaxChildDirectories <= 24);
        }
    }
}
