using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
                    "systeminfo", "servicesinfo", "processinfo", "applicationsinfo", "browserinfo", "debug" 
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
    }
}

