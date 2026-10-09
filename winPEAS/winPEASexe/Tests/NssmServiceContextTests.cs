using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class NssmServiceContextTests
    {
        private static Dictionary<string, string> Service(string path)
        {
            return new Dictionary<string, string>
            {
                ["Name"] = "HelperSvc",
                ["PathName"] = path,
                ["Account"] = "LocalSystem"
            };
        }

        [TestMethod]
        public void ReadsOnlyExactNssmParametersAndKeepsArgumentsHidden()
        {
            int reads = 0;
            NssmServiceContext context = ServicesInfoHelper.ReadNssmServiceContext(
                Service("\"C:\\Program Files\\NSSM\\nssm.exe\""), path =>
                {
                    reads++;
                    Assert.AreEqual(@"SYSTEM\CurrentControlSet\Services\HelperSvc\Parameters", path);
                    return new Dictionary<string, object>
                    {
                        ["Application"] = @"C:\Vendor.Name\Monitor\service.exe",
                        ["AppDirectory"] = @"C:\Vendor.Name\Monitor",
                        ["AppParameters"] = "-password secret-value"
                    };
                });
            Assert.AreEqual(1, reads);
            Assert.IsNotNull(context);
            Assert.AreEqual(@"C:\Vendor.Name\Monitor\service.exe", context.Application);
            Assert.AreEqual(@"C:\Vendor.Name\Monitor", context.Directory);
            Assert.AreEqual("LocalSystem", context.Account);
            Assert.IsFalse(context.Application.Contains("secret-value"));
            Assert.AreEqual(16, ServicesInfoHelper.MaxNssmServiceContexts);
        }

        [TestMethod]
        public void SkipsLookalikesAndUnsafeServiceNamesWithoutRegistryRead()
        {
            int reads = 0;
            Func<string, Dictionary<string, object>> read = path =>
            {
                reads++;
                return new Dictionary<string, object>();
            };
            Assert.IsNull(ServicesInfoHelper.ReadNssmServiceContext(Service(@"C:\Tools\almost-nssm.exe"), read));
            Assert.IsNull(ServicesInfoHelper.ReadNssmServiceContext(Service(@"\\server\share\nssm.exe"), read));
            Assert.IsNull(ServicesInfoHelper.ReadNssmServiceContext(Service(@"C:\Tools\nssm.exe -arg"), read));
            Assert.IsNull(ServicesInfoHelper.ReadNssmServiceContext(Service("C:\\Tools\\nssm.exe\n"), read));
            Assert.IsNull(ServicesInfoHelper.ReadNssmServiceContext(Service("C:\\" + new string('a', 520) + "\\nssm.exe"), read));
            var unsafeName = Service(@"C:\Tools\nssm.exe");
            unsafeName["Name"] = @"Bad\Child";
            Assert.IsNull(ServicesInfoHelper.ReadNssmServiceContext(unsafeName, read));
            Assert.AreEqual(0, reads);
        }

        [TestMethod]
        public void RejectsNonLocalOrAmbiguousChildPath()
        {
            foreach (string app in new[] {
                @"\\server\share\service.exe", @"C:\Safe\..\service.exe",
                @"%SystemRoot%\service.exe", @"C:\Safe\bad?.exe",
                "C:\\Safe\\bad\nname.exe", new string('x', 513)
            })
            {
                NssmServiceContext context = ServicesInfoHelper.ReadNssmServiceContext(
                    Service(@"C:\Tools\nssm.exe"), path => new Dictionary<string, object>
                    {
                        ["Application"] = app
                    });
                Assert.IsNull(context);
            }
        }
    }
}
