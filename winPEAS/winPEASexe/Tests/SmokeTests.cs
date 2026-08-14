using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using winPEAS.Info.ServicesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class SmokeTests
    {
        [TestMethod]
        public void ShouldRunWinPeass()
        {
            try
            {
                string[] args = new string[] {
                    "systeminfo", "userinfo", "servicesinfo", "browserinfo", "eventsinfo", "cloud", "debug"
                };
                Program.Main(args);
            }
            catch (Exception e)
            {
                Assert.Fail($"Exception thrown: {e.Message}");
            }
        }

        [TestMethod]
        public void ShouldDisplayHelp()
        {
            try
            {
                string[] args = new string[] {
                    "help",
                };
                Program.Main(args);
            }
            catch (Exception e)
            {
                Assert.Fail($"Exception thrown: {e.Message}");
            }
        }

        [TestMethod]
        public void ShouldNormalizeServiceDllPathsAndSystemAccount()
        {
            Assert.AreEqual(
                @"C:\Windows\System32\example.dll",
                ServicesInfoHelper.NormalizeServiceDllPath(@" ""%SystemRoot%\System32\example.dll"" ", @"C:\Windows"));
            Assert.AreEqual(
                @"C:\Windows\System32\example.dll",
                ServicesInfoHelper.NormalizeServiceDllPath(@"\SystemRoot\System32\example.dll", @"C:\Windows"));
            Assert.AreEqual(
                @"C:\Windows\Sysnative\example.dll",
                ServicesInfoHelper.GetFileSystemAccessPath(
                    @"C:\Windows\System32\example.dll", @"C:\Windows", true, false));
            Assert.AreEqual(
                @"C:\Windows\System32\example.dll",
                ServicesInfoHelper.GetFileSystemAccessPath(
                    @"C:\Windows\System32\example.dll", @"C:\Windows", true, true));
            Assert.IsTrue(ServicesInfoHelper.IsLocalSystemAccount(null));
            Assert.IsTrue(ServicesInfoHelper.IsLocalSystemAccount(@"NT AUTHORITY\SYSTEM"));
            Assert.IsFalse(ServicesInfoHelper.IsLocalSystemAccount(@"NT AUTHORITY\LocalService"));
        }
    }
}
