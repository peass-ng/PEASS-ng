using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace winPEAS.Helpers
{
    ///////////////////////////////////
    //////// Check Permissions ////////
    ///////////////////////////////////
    /// Get interesting permissions from Files, Folders and Registry
    internal static class PermissionsHelper
    {
        public static List<string> GetPermissionsFile(string path, Dictionary<string, string> SIDs, bool isOnlyWriteOrEquivalentCheck = false)
        {
            /*Permisos especiales para carpetas 
             *https://docs.microsoft.com/en-us/windows/win32/secauthz/access-mask-format?redirectedfrom=MSDN
             *https://docs.microsoft.com/en-us/windows/win32/fileio/file-security-and-access-rights?redirectedfrom=MSDN
             */

            List<string> results = new List<string>();
            path = path.Trim();
            if (path == null || path == "")
                return results;

            Match reg_path = Regex.Match(path.ToString(), @"\W*([a-z]:\\.+?(\.[a-zA-Z0-9_-]+))\W*", RegexOptions.IgnoreCase);
            string binaryPath = reg_path.Groups[1].ToString();
            path = binaryPath;
            if (path == null || path == "")
                return results;

            try
            {
                FileSecurity fSecurity = File.GetAccessControl(path);
                results = GetMyPermissionsF(fSecurity, SIDs, isOnlyWriteOrEquivalentCheck);
            }
            catch
            {
                //By some reason some times it cannot find a file or cannot get permissions (normally with some binaries inside system32)
            }
            return results;
        }

        public static List<string> GetPermissionsFolder(string path, Dictionary<string, string> SIDs, bool isOnlyWriteOrEquivalentCheck = false)
        {
            List<string> results = new List<string>();

            try
            {
                path = path.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    return results;
                }

                path = GetFolderFromString(path);

                if (string.IsNullOrEmpty(path))
                {
                    return results;
                }

                FileSecurity fSecurity = File.GetAccessControl(path);
                results = GetMyPermissionsF(fSecurity, SIDs, isOnlyWriteOrEquivalentCheck);
            }
            catch
            {
                //Te exceptions here use to be "Not access to a file", nothing interesting
            }
            return results;
        }

        public static List<string> GetMyPermissionsF(FileSecurity fSecurity, Dictionary<string, string> SIDs, bool isOnlyWriteOrEquivalentCheck = false)
        {
            // Get interesting permissions in fSecurity (Only files and folders)
            List<string> results = new List<string>();
            Dictionary<string, string> container = new Dictionary<string, string>();

            foreach (FileSystemAccessRule rule in fSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                //First, check if the rule to check is interesting
                int current_perm = (int)rule.FileSystemRights;
                string current_perm_str = PermInt2Str(current_perm, isOnlyWriteOrEquivalentCheck);
                if (current_perm_str == "")
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> mySID in SIDs)
                {
                    // If the rule is interesting, check if any of my SIDs is in the rule
                    if (rule.IdentityReference.Value.ToLower() == mySID.Key.ToLower())
                    {
                        string SID_name = string.IsNullOrEmpty(mySID.Value) ? mySID.Key : mySID.Value;

                        if (container.ContainsKey(SID_name))
                        {
                            if (!container[SID_name].Contains(current_perm_str))
                            {
                                container[SID_name] += " " + current_perm_str;
                            }
                        }
                        else
                            container[SID_name] = current_perm_str;

                        string to_add = string.Format("{0} [{1}]", SID_name, current_perm_str);
                    }
                }
            }
            foreach (KeyValuePair<string, string> SID_input in container)
            {
                string to_add = string.Format("{0} [{1}]", SID_input.Key, SID_input.Value);
                results.Add(to_add);
            }
            return results;
        }

        public static List<string> GetMyPermissionsR(RegistryKey key, Dictionary<string, string> SIDs)
        {
            // Get interesting permissions in rSecurity (Only Registry)
            List<string> results = new List<string>();
            Dictionary<string, string> container = new Dictionary<string, string>();

            try
            {
                var rSecurity = key.GetAccessControl();

                //Go through the rules returned from the DirectorySecurity
                foreach (RegistryAccessRule rule in rSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    int current_perm = (int)rule.RegistryRights;
                    string current_perm_str = PermInt2Str(current_perm, true);
                    if (current_perm_str == "")
                        continue;

                    foreach (KeyValuePair<string, string> mySID in SIDs)
                    {
                        // If the rule is interesting, check if any of my SIDs is in the rule
                        if (rule.IdentityReference.Value.ToLower() == mySID.Key.ToLower())
                        {
                            string SID_name = string.IsNullOrEmpty(mySID.Value) ? mySID.Key : mySID.Value;

                            if (container.ContainsKey(SID_name))
                            {
                                if (!container[SID_name].Contains(current_perm_str))
                                    container[SID_name] += " " + current_perm_str;
                            }
                            else
                                container[SID_name] = current_perm_str;

                            string to_add = string.Format("{0} [{1}]", SID_name, current_perm_str);
                        }
                    }
                }
                foreach (KeyValuePair<string, string> SID_input in container)
                {
                    string to_add = string.Format("{0} [{1}]", SID_input.Key, SID_input.Value);
                    results.Add(to_add);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static string PermInt2Str(int current_perm, bool only_write_or_equivalent = false, bool is_service = false)
        {
            Dictionary<string, int> interesting_perms = new Dictionary<string, int>()
                {
                    // This isn't an exhaustive list of possible permissions. Just the interesting ones.
                    { "AllAccess", 0xf01ff},
                    { "GenericAll", 0x10000000},
                    { "FullControl", (int)FileSystemRights.FullControl },
                    { "TakeOwnership", (int)FileSystemRights.TakeOwnership },
                    { "GenericWrite", 0x40000000 },
                    { "WriteData/CreateFiles", (int)FileSystemRights.WriteData },
                    { "Modify", (int)FileSystemRights.Modify },
                    { "Write", (int)FileSystemRights.Write },
                    { "ChangePermissions", (int)FileSystemRights.ChangePermissions },
                    { "Delete", (int)FileSystemRights.Delete },
                    { "DeleteSubdirectoriesAndFiles", (int)FileSystemRights.DeleteSubdirectoriesAndFiles },
                    { "AppendData/CreateDirectories", (int)FileSystemRights.AppendData },
                    { "WriteAttributes", (int)FileSystemRights.WriteAttributes },
                    { "WriteExtendedAttributes", (int)FileSystemRights.WriteExtendedAttributes },
                };

            if (only_write_or_equivalent)
            {
                interesting_perms = new Dictionary<string, int>()
                {
                    { "AllAccess", 0xf01ff},
                    { "GenericAll", 0x10000000},
                    { "FullControl", (int)FileSystemRights.FullControl }, //0x1f01ff
                    { "TakeOwnership", (int)FileSystemRights.TakeOwnership }, //0x80000
                    { "GenericWrite", 0x40000000 },
                    { "WriteData/CreateFiles", (int)FileSystemRights.WriteData }, //0x2
                    { "Modify", (int)FileSystemRights.Modify }, //0x301bf
                    { "Write", (int)FileSystemRights.Write }, //0x116
                    { "ChangePermissions", (int)FileSystemRights.ChangePermissions }, //0x40000
                    { "AppendData/CreateDirectories", (int)FileSystemRights.AppendData },
                };
            }

            if (is_service)
            {
                interesting_perms["Start"] = 0x00000010;
                interesting_perms["Stop"] = 0x00000020;
            }

            try
            {
                foreach (KeyValuePair<string, int> entry in interesting_perms)
                {
                    if ((entry.Value & current_perm) == entry.Value)
                    { 
                        return entry.Key; 
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error in PermInt2Str: " + ex);
            }
            return "";
        }

        public static string GetFolderFromString(string path)
        {
            string fpath = path;
            if (!Directory.Exists(path))
            {
                Match reg_path = Regex.Match(path.ToString(), @"\W*([a-z]:\\.+?(\.[a-zA-Z0-9_-]+))\W*", RegexOptions.IgnoreCase);
                string binaryPath = reg_path.Groups[1].ToString();
                if (File.Exists(binaryPath))
                    fpath = Path.GetDirectoryName(binaryPath);
                else
                    fpath = "";
            }
            return fpath;
        }

        //From https://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
        public static Dictionary<string, string> GetRecursivePrivs(string path, int cont = 0)
        {
            /*string root = @Path.GetPathRoot(Environment.SystemDirectory) + path;
            var dirs = from dir in Directory.EnumerateDirectories(root) select dir;
            return dirs.ToList();*/
            Dictionary<string, string> results = new Dictionary<string, string>();
            int max_dir_recurse = 130;
            if (cont > max_dir_recurse)
            {
                return results; //"Limit" for apps with hundreds of thousands of folders
            }

            results[path] = ""; //If you cant open, then there are no privileges for you (and the try will explode)
            try
            {
                results[path] = String.Join(", ", GetPermissionsFolder(path, Checks.Checks.CurrentUserSiDs));
                if (string.IsNullOrEmpty(results[path]))
                {
                    foreach (string d in Directory.EnumerateDirectories(path))
                    {
                        foreach (string f in Directory.EnumerateFiles(d))
                        {
                            results[f] = String.Join(", ", GetPermissionsFile(f, Checks.Checks.CurrentUserSiDs));
                        }
                        cont += 1;
                        results.Concat(GetRecursivePrivs(d, cont)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                }
            }
            catch
            {
                //Access denied to a path
            }
            return results;
        }
    }
}
