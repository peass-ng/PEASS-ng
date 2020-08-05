using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;

namespace winPEAS
{
    class InterestingFiles
    {
        private InterestingFiles() {}
        public static List<string> GetUnattendedInstallFiles()
        { //From SharpUP
            List<string> results = new List<string>();

            try
            {
                string windir = System.Environment.GetEnvironmentVariable("windir");
                string[] SearchLocations =
                {
                    String.Format("{0}\\sysprep\\sysprep.xml", windir),
                    String.Format("{0}\\sysprep\\sysprep.inf", windir),
                    String.Format("{0}\\sysprep.inf", windir),
                    String.Format("{0}\\Panther\\Unattended.xml", windir),
                    String.Format("{0}\\Panther\\Unattend.xml", windir),
                    String.Format("{0}\\Panther\\Unattend\\Unattend.xml", windir),
                    String.Format("{0}\\Panther\\Unattend\\Unattended.xml", windir),
                    String.Format("{0}\\System32\\Sysprep\\unattend.xml", windir),
                    String.Format("{0}\\System32\\Sysprep\\Panther\\unattend.xml", windir),
                    String.Format("{0}\\..\\unattend.xml", windir),
                    String.Format("{0}\\..\\unattend.inf", windir),
                };

                foreach (string SearchLocation in SearchLocations)
                {
                    if (System.IO.File.Exists(SearchLocation))
                        results.Add(SearchLocation);
                    
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<string> ExtractUnattenededPwd(string path)
        {
            List<string> results = new List<string>();
            try { 
                string text = File.ReadAllText(path);
                text = text.Replace("\n", "");
                text = text.Replace("\r", "");
                Regex regex = new Regex(@"<Password>.*</Password>");
                foreach (Match match in regex.Matches(text))
                    results.Add(match.Value);
                
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<string> GetSAMBackups()
        { //From SharpUP
            List<string> results = new List<string>();

            try
            {
                string systemRoot = System.Environment.GetEnvironmentVariable("SystemRoot");
                string[] SearchLocations =
                {
                    String.Format(@"{0}\repair\SAM", systemRoot),
                    String.Format(@"{0}\System32\config\RegBack\SAM", systemRoot),
                    //String.Format(@"{0}\System32\config\SAM", systemRoot),
                    String.Format(@"{0}\repair\SYSTEM", systemRoot),
                    //String.Format(@"{0}\System32\config\SYSTEM", systemRoot),
                    String.Format(@"{0}\System32\config\RegBack\SYSTEM", systemRoot),
                };

                foreach (string SearchLocation in SearchLocations)
                {
                    if (System.IO.File.Exists(SearchLocation))
                        results.Add(SearchLocation);

                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<string> GetMcAfeeSitelistFiles()
        { //From SharpUP
            List<string> results = new List<string>();

            try
            {
                string drive = System.Environment.GetEnvironmentVariable("SystemDrive");

                string[] SearchLocations =
                {
                    String.Format("{0}\\Program Files\\", drive),
                    String.Format("{0}\\Program Files (x86)\\", drive),
                    String.Format("{0}\\Documents and Settings\\", drive),
                    String.Format("{0}\\Users\\", drive)
                };

                foreach (string SearchLocation in SearchLocations)
                {
                    List<string> files = MyUtils.FindFiles(SearchLocation, "SiteList.xml");

                    foreach (string file in files)
                        results.Add(file);
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static Dictionary<string, Dictionary<string, string>> GetCachedGPPPassword()
        {  //From SharpUP
            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                string allUsers = System.Environment.GetEnvironmentVariable("ALLUSERSPROFILE");

                if (!allUsers.Contains("ProgramData"))
                {
                    // Before Windows Vista, the default value of AllUsersProfile was "C:\Documents and Settings\All Users"
                    // And after, "C:\ProgramData"
                    allUsers += "\\Application Data";
                }
                allUsers += "\\Microsoft\\Group Policy\\History"; // look only in the GPO cache folder

                List<String> files = MyUtils.FindFiles(allUsers, "*.xml");

                // files will contain all XML files
                foreach (string file in files)
                {
                    if (!(file.Contains("Groups.xml") || file.Contains("Services.xml")
                        || file.Contains("Scheduledtasks.xml") || file.Contains("DataSources.xml")
                        || file.Contains("Printers.xml") || file.Contains("Drives.xml")))
                    {
                        continue; // uninteresting XML files, move to next
                    }

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);

                    if (!xmlDoc.InnerXml.Contains("cpassword"))
                    {
                        continue; // no "cpassword" => no interesting content, move to next
                    }

                    Console.WriteLine("\r\n{0}", file);

                    string cPassword = "";
                    string UserName = "";
                    string NewName = "";
                    string Changed = "";
                    if (file.Contains("Groups.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/Groups/User/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/Groups/User");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("userName"))
                            {
                                UserName = attr.Value;
                            }
                            if (attr.Name.Equals("newName"))
                            {
                                NewName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }
                        //Console.WriteLine("\r\nA{0}", a.Attributes[0].Value);
                    }
                    else if (file.Contains("Services.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/NTServices/NTService/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/NTServices/NTService");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("accountName"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }

                    }
                    else if (file.Contains("Scheduledtasks.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/ScheduledTasks/Task/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/ScheduledTasks/Task");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("runAs"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }

                    }
                    else if (file.Contains("DataSources.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/DataSources/DataSource/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/DataSources/DataSource");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("username"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }
                    }
                    else if (file.Contains("Printers.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/Printers/SharedPrinter/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/Printers/SharedPrinter");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("username"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }
                    }
                    else
                    {
                        // Drives.xml
                        XmlNode a = xmlDoc.SelectSingleNode("/Drives/Drive/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/Drives/Drive");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("username"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }

                    }

                    if (UserName.Equals(""))
                    {
                        UserName = "[BLANK]";
                    }

                    if (NewName.Equals(""))
                    {
                        NewName = "[BLANK]";
                    }


                    if (cPassword.Equals(""))
                    {
                        cPassword = "[BLANK]";
                    }
                    else
                    {
                        cPassword = DecryptGPP(cPassword);
                    }

                    if (Changed.Equals(""))
                    {
                        Changed = "[BLANK]";
                    }

                    results[file] = new Dictionary<string, string>();
                    results[file]["UserName"] = UserName;
                    results[file]["NewName"] = NewName;
                    results[file]["cPassword"] = cPassword;
                    results[file]["Changed"] = Changed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }


        public static string DecryptGPP(string cpassword)
        {  //From SharpUP
            int mod = cpassword.Length % 4;

            switch (mod)
            {
                case 1:
                    cpassword = cpassword.Substring(0, cpassword.Length - 1);
                    break;
                case 2:
                    cpassword += "".PadLeft(4 - mod, '=');
                    break;
                case 3:
                    cpassword += "".PadLeft(4 - mod, '=');
                    break;
                default:
                    break;
            }

            byte[] base64decoded = Convert.FromBase64String(cpassword);

            AesCryptoServiceProvider aesObject = new AesCryptoServiceProvider();

            byte[] aesKey = { 0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f, 0xfe, 0xe8, 0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b };
            byte[] aesIV = new byte[aesObject.IV.Length];

            aesObject.IV = aesIV;
            aesObject.Key = aesKey;

            ICryptoTransform aesDecryptor = aesObject.CreateDecryptor();
            byte[] outBlock = aesDecryptor.TransformFinalBlock(base64decoded, 0, base64decoded.Length);

            return System.Text.UnicodeEncoding.Unicode.GetString(outBlock);
        }

        public static List<string> ListUsersDocs()
        {
            List<string> results = new List<string>();
            try
            {
                // returns files (w/ modification dates) that match the given pattern below
                string patterns = "*diagram*;*.pdf;*.vsd;*.doc;*docx;*.xls;*.xlsx";

                if (MyUtils.IsHighIntegrity())
                {
                    string searchPath = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));

                    List<string> files = MyUtils.FindFiles(searchPath, patterns);

                    foreach (string file in files)
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                        results.Add(file);
                    }
                }

                else
                {
                    string searchPath = Environment.GetEnvironmentVariable("USERPROFILE");

                    List<string> files = MyUtils.FindFiles(searchPath, patterns);

                    foreach (string file in files)
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                        results.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

        public static string GetConsoleHostHistory()
        {
            string result = "";
            try
            {
                
                string searchLocation = String.Format("{0}\\AppData\\Roaming\\Microsoft\\Windows\\PowerShell\\PSReadline\\ConsoleHost_history.txt", Environment.GetEnvironmentVariable("USERPROFILE"));
                if (System.IO.File.Exists(searchLocation))
                    result = searchLocation;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return result;
        }

        public static List<Dictionary<string, string>> GetRecycleBin()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                // lists recently deleted files (needs to be run from a user context!)

                // Reference: https://stackoverflow.com/questions/18071412/list-filenames-in-the-recyclebin-with-c-sharp-without-using-any-external-files
                int lastDays = 30;

                var startTime = System.DateTime.Now.AddDays(-lastDays);

                // Shell COM object GUID
                Type shell = Type.GetTypeFromCLSID(new Guid("13709620-C279-11CE-A49E-444553540000"));
                Object shellObj = Activator.CreateInstance(shell);

                // namespace for recycle bin == 10 - https://msdn.microsoft.com/en-us/library/windows/desktop/bb762494(v=vs.85).aspx
                Object recycle = shellObj.GetType().InvokeMember("Namespace", BindingFlags.InvokeMethod, null, shellObj, new object[] { 10 });
                // grab all the deletes items
                Object items = recycle.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, recycle, null);
                // grab the number of deleted items
                Object count = items.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, items, null);
                int deletedCount = Int32.Parse(count.ToString());

                // iterate through each item
                for (int i = 0; i < deletedCount; i++)
                {
                    // grab the specific deleted item
                    Object item = items.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, items, new object[] { i });
                    Object DateDeleted = item.GetType().InvokeMember("ExtendedProperty", BindingFlags.InvokeMethod, null, item, new object[] { "System.Recycle.DateDeleted" });
                    DateTime modifiedDate = DateTime.Parse(DateDeleted.ToString());
                    if (modifiedDate > startTime)
                    {
                        // additional extended properties from https://blogs.msdn.microsoft.com/oldnewthing/20140421-00/?p=1183
                        Object Name = item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null);
                        Object Path = item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null);
                        Object Size = item.GetType().InvokeMember("Size", BindingFlags.GetProperty, null, item, null);
                        Object DeletedFrom = item.GetType().InvokeMember("ExtendedProperty", BindingFlags.InvokeMethod, null, item, new object[] { "System.Recycle.DeletedFrom" });
                        results.Add(new Dictionary<string, string>()
                    {
                        { "Name", String.Format("{0}", Name) },
                        { "Path",  String.Format("{0}", Path) },
                        { "Size",  String.Format("{0}", Size) },
                        { "Deleted from",  String.Format("{0}", DeletedFrom) },
                        { "Date Deleted",  String.Format("{0}", DateDeleted) }
                    });
                    }
                    Marshal.ReleaseComObject(item);
                    item = null;
                }
                Marshal.ReleaseComObject(recycle);
                recycle = null;
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }
    }
}
