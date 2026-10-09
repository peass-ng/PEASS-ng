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
    }
}
