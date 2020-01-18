using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace winPEAS
{
    class InterestingFiles
    {
        public static List<string> ListUsersDocs()
        {
            List<string> results = new List<string>();
            try
            {
                // returns files (w/ modification dates) that match the given pattern below
                string patterns = "*diagram*;*.pdf;*.vsd;*.doc;*docx;*.xls;*.xlsx;";

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
