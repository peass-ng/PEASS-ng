using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.KnownFileCreds
{
    static class RemoteDesktop
    {
        public static List<Dictionary<string, string>> GetSavedRDPConnections()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            //shows saved RDP connections, including username hints (if present)
            if (MyUtils.IsHighIntegrity())
            {
                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                    {
                        string[] subkeys = RegistryHelper.GetRegSubkeys("HKU", string.Format("{0}\\Software\\Microsoft\\Terminal Server Client\\Servers", SID));
                        if (subkeys != null)
                        {
                            //Console.WriteLine("\r\n\r\n=== Saved RDP Connection Information ({0}) ===", SID);
                            foreach (string host in subkeys)
                            {
                                string usernameHint = RegistryHelper.GetRegValue("HKCU", string.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
                                Dictionary<string, string> rdp_info = new Dictionary<string, string>() {
                                    { "SID", SID },
                                    { "Host", host },
                                    { "Username Hint", usernameHint },
                                };
                                results.Add(rdp_info);
                            }
                        }
                    }
                }
            }
            else
            {
                string[] subkeys = RegistryHelper.GetRegSubkeys("HKCU", "Software\\Microsoft\\Terminal Server Client\\Servers");
                if (subkeys != null)
                {
                    foreach (string host in subkeys)
                    {
                        string usernameHint = RegistryHelper.GetRegValue("HKCU", string.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
                        Dictionary<string, string> rdp_info = new Dictionary<string, string>() {
                            { "SID", "" },
                            { "Host", host },
                            { "Username Hint", usernameHint },
                        };
                        results.Add(rdp_info);
                    }
                }
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetRDCManFiles()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // lists any found files in Local\Microsoft\Credentials\*
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = string.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    var dirs = Directory.EnumerateDirectories(userFolder);

                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userRDManFile = string.Format("{0}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings", dir);
                            if (File.Exists(userRDManFile))
                            {
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.Load(userRDManFile);

                                // grab the recent RDG files
                                XmlNodeList filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                                XmlNodeList items = filesToOpen[0].ChildNodes;
                                XmlNode node = items[0];

                                DateTime lastAccessed = File.GetLastAccessTime(userRDManFile);
                                DateTime lastModified = File.GetLastWriteTime(userRDManFile);
                                Dictionary<string, string> rdg = new Dictionary<string, string>(){
                                    { "RDCManFile", userRDManFile },
                                    { "Accessed", string.Format("{0}", lastAccessed) },
                                    { "Modified", string.Format("{0}", lastModified) },
                                    { ".RDG Files", "" },
                                };

                                foreach (XmlNode rdgFile in items)
                                    rdg[".RDG Files"] += rdgFile.InnerText;

                                results.Add(rdg);
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    string userRDManFile = string.Format("{0}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings", Environment.GetEnvironmentVariable("USERPROFILE"));

                    if (File.Exists(userRDManFile))
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(userRDManFile);

                        // grab the recent RDG files
                        XmlNodeList filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                        XmlNodeList items = filesToOpen[0].ChildNodes;
                        XmlNode node = items[0];

                        DateTime lastAccessed = File.GetLastAccessTime(userRDManFile);
                        DateTime lastModified = File.GetLastWriteTime(userRDManFile);
                        Dictionary<string, string> rdg = new Dictionary<string, string>(){
                                    { "RDCManFile", userRDManFile },
                                    { "Accessed", string.Format("{0}", lastAccessed) },
                                    { "Modified", string.Format("{0}", lastModified) },
                                    { ".RDG Files", "" },
                                };

                        foreach (XmlNode rdgFile in items)
                            rdg[".RDG Files"] += rdgFile.InnerText;
                        results.Add(rdg);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
