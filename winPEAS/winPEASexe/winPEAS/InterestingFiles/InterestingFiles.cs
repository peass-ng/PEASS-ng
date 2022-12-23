using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using winPEAS.Helpers;
using winPEAS.Helpers.Search;

namespace winPEAS.InterestingFiles
{
    internal static class InterestingFiles
    {
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

                results.AddRange(searchLocations.Where(searchLocation => File.Exists(searchLocation)));
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static List<string> GetLinuxShells()
        {
            var results = new List<string>();

            try
            {
                string drive = Environment.GetEnvironmentVariable("SystemDrive");

                string[] searchDirs =
                {
                    $@"{drive}\Windows\SysNative\",
                    $@"{drive}\Windows\System32\",
                };

                string[] fileNames =
                {
                    "wsl.exe",
                    "bash.exe",
                };

                var searchLocations = (from searchDir in searchDirs
                                       from fileName in fileNames
                                       select Path.Combine(searchDir, fileName)).ToList();

                results.AddRange(searchLocations.Where(File.Exists));
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }


        public static List<string> ListUsersDocs()
        {
            List<string> results = new List<string>();
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    results = SearchHelper.SearchUsersDocs();
                }
                else
                {
                    results = SearchHelper.SearchCurrentUserDocs();
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetRecycleBin()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                // lists recently deleted files (needs to be run from a user context!)

                // Reference: https://stackoverflow.com/questions/18071412/list-filenames-in-the-recyclebin-with-c-sharp-without-using-any-external-files
                int lastDays = 30;

                var startTime = DateTime.Now.AddDays(-lastDays);

                // Shell COM object GUID
                Type shell = Type.GetTypeFromCLSID(new Guid("13709620-C279-11CE-A49E-444553540000"));
                object shellObj = Activator.CreateInstance(shell);

                // namespace for recycle bin == 10 - https://msdn.microsoft.com/en-us/library/windows/desktop/bb762494(v=vs.85).aspx
                object recycle = ReflectionHelper.InvokeMemberMethod(shellObj, "Namespace", new object[] { 10 });
                // grab all the deletes items
                object items = ReflectionHelper.InvokeMemberMethod(recycle, "Items");
                // grab the number of deleted items
                object count = ReflectionHelper.InvokeMemberProperty(items, "Count");
                int deletedCount = Int32.Parse(count.ToString());

                // iterate through each item
                for (int i = 0; i < deletedCount; i++)
                {
                    // grab the specific deleted item
                    object item = ReflectionHelper.InvokeMemberMethod(items, "Item", new object[] { i });
                    object dateDeleted = ReflectionHelper.InvokeMemberMethod(item, "ExtendedProperty", new object[] { "System.Recycle.DateDeleted" });
                    DateTime modifiedDate = DateTime.Parse(dateDeleted.ToString());
                    if (modifiedDate > startTime)
                    {
                        // additional extended properties from https://blogs.msdn.microsoft.com/oldnewthing/20140421-00/?p=1183
                        object name = ReflectionHelper.InvokeMemberProperty(item, "Name");
                        object path = ReflectionHelper.InvokeMemberProperty(item, "Path");
                        object size = ReflectionHelper.InvokeMemberProperty(item, "Size");
                        object deletedFrom = ReflectionHelper.InvokeMemberMethod(item, "ExtendedProperty", new object[] { "System.Recycle.DeletedFrom" });
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
