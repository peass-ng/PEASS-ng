using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

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
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                string[] args = new string[] {
                    "help",
                };
                Program.Main(args);

                string output = sw.ToString();
                Assert.IsTrue(output.Contains("WinPEAS is a binary"),
                    "Help output did not contain expected text.");
            }
            catch (Exception e)
            {
                Assert.Fail($"Exception thrown: {e.Message}");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}

