using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;

namespace winPEAS
{
    static class InterestingFiles
    {       
        public static List<string> GetUnattendedInstallFiles()
        { 
            //From SharpUP
            var results = new List<string>();

            try
            {
                var winDir = System.Environment.GetEnvironmentVariable("windir");
                string[] searchLocations =
                {
                   $"{winDir}\\sysprep\\sysprep.xml",
                   $"{winDir}\\sysprep\\sysprep.inf",
                   $"{winDir}\\sysprep.inf",
                   $"{winDir}\\Panther\\Unattended.xml",
                   $"{winDir}\\Panther\\Unattend.xml",
                   $"{winDir}\\Panther\\Unattend\\Unattend.xml",
                   $"{winDir}\\Panther\\Unattend\\Unattended.xml",
                   $"{winDir}\\System32\\Sysprep\\unattend.xml",
                   $"{winDir}\\System32\\Sysprep\\Panther\\unattend.xml",
                   $"{winDir}\\..\\unattend.xml",
                   $"{winDir}\\..\\unattend.inf",
                };

                results.AddRange(searchLocations.Where(System.IO.File.Exists));
            }
            catch (Exception ex)
            {                
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static List<string> ExtractUnattenededPwd(string path)
        {
            List<string> results = new List<string>();

            try
            { 
                string text = File.ReadAllText(path);
                text = text.Replace("\n", "");
                text = text.Replace("\r", "");
                Regex regex = new Regex(@"<Password>.*</Password>");
                
                foreach (Match match in regex.Matches(text))
                {
                    results.Add(match.Value);
                }                
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return results;
        }

        public static List<string> GetSAMBackups()
        { 
            //From SharpUP
            var results = new List<string>();

            try
            {
                string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                string[] searchLocations =
                {
                    $@"{systemRoot}\repair\SAM",
                    $@"{systemRoot}\System32\config\RegBack\SAM",
                    //$@"{0}\System32\config\SAM"
                    $@"{systemRoot}\repair\SYSTEM",
                    //$@"{0}\System32\config\SYSTEM", systemRoot),
                    $@"{systemRoot}\System32\config\RegBack\SYSTEM",
                };

                results.AddRange(searchLocations.Where(searchLocation => System.IO.File.Exists(searchLocation)));
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static List<string> GetMcAfeeSitelistFiles()
        { 
            //From SharpUP
            List<string> results = new List<string>();

            try
            {
                string drive = System.Environment.GetEnvironmentVariable("SystemDrive");

                string[] searchLocations =
                {
                    $"{drive}\\Program Files\\",
                    $"{drive}\\Program Files (x86)\\",
                    $"{drive}\\Documents and Settings\\",
                    $"{drive}\\Users\\",
                };

                results.AddRange(
                    searchLocations.SelectMany(
                        searchLocation => MyUtils.FindFiles(searchLocation, "SiteList.xml")));
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
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

                List<string> files = MyUtils.FindFiles(allUsers, "*.xml");

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
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }


        public static string DecryptGPP(string cPassword)
        {  
            //From SharpUP
            int mod = cPassword.Length % 4;

            switch (mod)
            {
                case 1:
                    cPassword = cPassword.Substring(0, cPassword.Length - 1);
                    break;
                case 2:
                    cPassword += "".PadLeft(4 - mod, '=');
                    break;
                case 3:
                    cPassword += "".PadLeft(4 - mod, '=');
                    break;
            }

            byte[] base64decoded = Convert.FromBase64String(cPassword);

            AesCryptoServiceProvider aesObject = new AesCryptoServiceProvider();

            byte[] aesKey = { 0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f,
                0xfe, 0xe8, 0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b };
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
                    string searchPath = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";

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

        private static object InvokeMemberMethod(object target, string name, object[] args = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            object result = InvokeMember(target, name, BindingFlags.InvokeMethod, args);

            return result;
        }

        private static object InvokeMemberProperty(object target, string name, object[] args = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            object result = InvokeMember(target, name, BindingFlags.GetProperty, args);

            return result;
        }

        private static object InvokeMember(object target, string name, BindingFlags invokeAttr, object[] args = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            object result = target.GetType().InvokeMember(name, invokeAttr, null, target, args);

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
                object shellObj = Activator.CreateInstance(shell);

                // namespace for recycle bin == 10 - https://msdn.microsoft.com/en-us/library/windows/desktop/bb762494(v=vs.85).aspx
                object recycle = InvokeMemberMethod(shellObj, "Namespace", new object[] { 10 });
                // grab all the deletes items
                object items = InvokeMemberMethod(recycle, "Items");
                // grab the number of deleted items
                object count = InvokeMemberProperty(items, "Count");
                int deletedCount = Int32.Parse(count.ToString());

                // iterate through each item
                for (int i = 0; i < deletedCount; i++)
                {
                    // grab the specific deleted item
                    object item = InvokeMemberMethod(items, "Item", new object[] { i });
                    object dateDeleted = InvokeMemberMethod(item, "ExtendedProperty", new object[] { "System.Recycle.DateDeleted" });
                    DateTime modifiedDate = DateTime.Parse(dateDeleted.ToString());
                    if (modifiedDate > startTime)
                    {
                        // additional extended properties from https://blogs.msdn.microsoft.com/oldnewthing/20140421-00/?p=1183
                        object name = InvokeMemberProperty(item, "Name");
                        object path = InvokeMemberProperty(item, "Path");
                        object size = InvokeMemberProperty(item, "Size");
                        object deletedFrom = InvokeMemberMethod(item, "ExtendedProperty", new object[] { "System.Recycle.DeletedFrom" });
                        results.Add(new Dictionary<string, string>()
                    {
                        { "Name", name.ToString() },
                        { "Path", path.ToString() },
                        { "Size", size.ToString() },
                        { "Deleted from", deletedFrom.ToString() },
                        { "Date Deleted", dateDeleted.ToString() }
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
