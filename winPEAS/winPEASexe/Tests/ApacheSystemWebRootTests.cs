using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace Tests
{
    [TestClass]
    public class ApacheSystemWebRootTests
    {
        [TestMethod]
        public void RequiresSystemIdentityAndExactApacheImage()
        {
            const string image = @"C:\xampp\apache\bin\httpd.exe";
            Assert.IsTrue(ApacheSystemWebRoot.IsSystemApacheService("LocalSystem",
                "\"C:\\xampp\\apache\\bin\\httpd.exe\" -k runservice", image));
            Assert.IsFalse(ApacheSystemWebRoot.IsSystemApacheService("LocalService",
                "\"C:\\xampp\\apache\\bin\\httpd.exe\" -k runservice", image));
            Assert.IsFalse(ApacheSystemWebRoot.IsSystemApacheService("LocalSystem",
                "\"C:\\other\\httpd.exe\" -k runservice", image));
            Assert.IsFalse(ApacheSystemWebRoot.IsSystemApacheService("LocalSystem",
                "\"C:\\xampp\\apache\\bin\\httpd.exe\" -f C:\\other\\httpd.conf", image));
        }

        [TestMethod]
        public void UsesOnlyExplicitDefaultDocumentRoot()
        {
            string config = "# DocumentRoot \"C:/wrong\"\n" +
                "<VirtualHost *:80>\nDocumentRoot \"C:/vhost\"\n</VirtualHost>\n" +
                "DocumentRoot \"C:/xampp/htdocs\"\n";
            Assert.AreEqual(@"C:\xampp\htdocs", ApacheSystemWebRoot.ParseDefaultDocumentRoot(config));
            Assert.IsNull(ApacheSystemWebRoot.ParseDefaultDocumentRoot(
                "DocumentRoot \"%UNKNOWN_APACHE_ROOT%/htdocs\"\n"));
            Assert.IsNull(ApacheSystemWebRoot.ParseDefaultDocumentRoot(
                "DocumentRoot \"C:/xampp/htdocs\"\nDocumentRoot \"C:/other\"\n"));
        }

        [TestMethod]
        public void RejectsOversizedConfiguration()
        {
            Assert.IsNull(ApacheSystemWebRoot.ParseDefaultDocumentRoot(
                new string('a', ApacheSystemWebRoot.MaxConfigBytes + 1)));
        }

        [TestMethod]
        public void WampImageMustMatchConventionalVersionedServicePath()
        {
            Assert.AreEqual(@"C:\wamp64\bin\apache\apache2.4.27\bin\httpd.exe",
                ApacheSystemWebRoot.ConventionalWampImage(
                    "\"C:\\wamp64\\bin\\apache\\apache2.4.27\\bin\\httpd.exe\" -k runservice", "C:"));
            Assert.AreEqual(@"C:\wamp\bin\apache\apache2.4.9\bin\httpd.exe",
                ApacheSystemWebRoot.ConventionalWampImage(
                    "\"C:\\wamp\\bin\\apache\\apache2.4.9\\bin\\httpd.exe\" -k runservice", "C:", "wamp"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"C:\\wamp64\\bin\\apache\\apache2.4.27\\bin\\other.exe\" -k runservice", "C:"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"C:\\wamp64\\bin\\apache\\staging\\bin\\httpd.exe\" -k runservice", "C:"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"C:\\wamp64\\bin\\apache\\apache...\\bin\\httpd.exe\" -k runservice", "C:"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"C:\\wamp64\\bin\\apache\\apache2..4\\bin\\httpd.exe\" -k runservice", "C:"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"C:\\wamp64\\bin\\apache\\apache2..4\\bin\\httpd.exe\" -k runservice", "C:"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"\\\\server\\share\\httpd.exe\" -k runservice", "C:"));
            Assert.IsNull(ApacheSystemWebRoot.ConventionalWampImage(
                "\"C:\\wamp\\bin\\apache\\apache2.4.9\\bin\\httpd.exe\" -k runservice", "C:", "wamp64"));
        }

        [TestMethod]
        public void WampDocumentRootRequiresMatchingInstallDefine()
        {
            string config = "Define INSTALL_DIR \"C:/wamp64\"\n" +
                "DocumentRoot \"${INSTALL_DIR}/www\"\n";
            Assert.AreEqual(@"C:\wamp64\www",
                ApacheSystemWebRoot.ParseWampDocumentRoot(config, @"C:\wamp64"));
            Assert.IsNull(ApacheSystemWebRoot.ParseWampDocumentRoot(
                config.Replace("C:/wamp64", "C:/other"), @"C:\wamp64"));
            Assert.IsNull(ApacheSystemWebRoot.ParseWampDocumentRoot(
                "DocumentRoot \"${INSTALL_DIR}/www\"\n", @"C:\wamp64"));
            Assert.IsNull(ApacheSystemWebRoot.ParseWampDocumentRoot(
                config.Replace("${INSTALL_DIR}", "${install_dir}"), @"C:\wamp64"));
            Assert.IsNull(ApacheSystemWebRoot.ParseWampDocumentRoot(
                config + "Define INSTALL_DIR \"C:/wamp64\"\n", @"C:\wamp64"));
            Assert.IsNull(ApacheSystemWebRoot.ParseWampDocumentRoot(
                "Define INSTALL_DIR \"C:/wamp64\"\nDocumentRoot \"C:/other\"\n", @"C:\wamp64"));
        }
    }
}
