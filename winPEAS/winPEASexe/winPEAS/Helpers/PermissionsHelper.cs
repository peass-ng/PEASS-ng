using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace winPEAS.Helpers
{
    internal enum PermissionType
    {
        DEFAULT,
        READABLE_OR_WRITABLE,
        WRITEABLE_OR_EQUIVALENT,
        WRITEABLE_OR_EQUIVALENT_REG,
        WRITEABLE_OR_EQUIVALENT_SVC,
    }


    ///////////////////////////////////
    //////// Check Permissions ////////
    ///////////////////////////////////
    /// Get interesting permissions from Files, Folders and Registry
    internal static class PermissionsHelper
    {
        public static List<string> GetPermissionsFile(string path, Dictionary<string, string> SIDs, PermissionType permissionType = PermissionType.DEFAULT)
        {
            /*Permisos especiales para carpetas 
             *https://docs.microsoft.com/en-us/windows/win32/secauthz/access-mask-format?redirectedfrom=MSDN
             *https://docs.microsoft.com/en-us/windows/win32/fileio/file-security-and-access-rights?redirectedfrom=MSDN
             */

            List<string> results = new List<string>();
            path = path.Trim();
            if (path == null || path == "")
                return results;

            Match reg_path = Regex.Match(path.ToString(), @"\W*([a-z]:\\[^.]+(\.[a-zA-Z0-9_-]+)?)\W*", RegexOptions.IgnoreCase);
            string binaryPath = reg_path.Groups[1].ToString();
            path = binaryPath;
            if (path == null || path == "")
                return results;

            try
            {
                FileSecurity fSecurity = File.GetAccessControl(path);
                results = GetMyPermissionsF(fSecurity, SIDs, permissionType);
            }
            catch
            {
                //By some reason some times it cannot find a file or cannot get permissions (normally with some binaries inside system32)
            }
            return results;
        }

        public static List<string> GetPermissionsFolder(string path, Dictionary<string, string> SIDs, PermissionType permissionType = PermissionType.DEFAULT)
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
                results = GetMyPermissionsF(fSecurity, SIDs, permissionType);
            }
            catch
            {
                //Te exceptions here use to be "Not access to a file", nothing interesting
            }
            return results;
        }

        public static List<string> GetMyPermissionsF(FileSecurity fSecurity, Dictionary<string, string> SIDs, PermissionType permissionType = PermissionType.DEFAULT)
        {
            // Get interesting permissions in fSecurity (Only files and folders)
            List<string> results = new List<string>();
            Dictionary<string, string> container = new Dictionary<string, string>();

            foreach (FileSystemAccessRule rule in fSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                //First, check if the rule to check is interesting
                int current_perm = (int)rule.FileSystemRights;
                string current_perm_str = PermInt2Str(current_perm, permissionType);
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
                    string current_perm_str = PermInt2Str(current_perm, PermissionType.WRITEABLE_OR_EQUIVALENT_REG);
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

        public static string PermInt2Str(int current_perm, PermissionType permissionType = PermissionType.DEFAULT)
        {
            Dictionary<string, int> interesting_perms = new Dictionary<string, int>();

            if (permissionType == PermissionType.DEFAULT)
            {
                interesting_perms = new Dictionary<string, int>()
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
            }

            else if (permissionType == PermissionType.READABLE_OR_WRITABLE)
            {
                interesting_perms = new Dictionary<string, int>()
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

                    { "Read", (int)FileSystemRights.Read },
                    { "ReadData", (int)FileSystemRights.ReadData },

                    { "ChangePermissions", (int)FileSystemRights.ChangePermissions },

                    { "Delete", (int)FileSystemRights.Delete },
                    { "DeleteSubdirectoriesAndFiles", (int)FileSystemRights.DeleteSubdirectoriesAndFiles },
                    { "AppendData/CreateDirectories", (int)FileSystemRights.AppendData },
                    { "WriteAttributes", (int)FileSystemRights.WriteAttributes },
                    { "WriteExtendedAttributes", (int)FileSystemRights.WriteExtendedAttributes },
                };
            }

            else if (permissionType == PermissionType.WRITEABLE_OR_EQUIVALENT)
            {
                interesting_perms = new Dictionary<string, int>()
                {
                    { "AllAccess", 0xf01ff},
                    { "GenericAll", 0x10000000},
                    { "FullControl", (int)FileSystemRights.FullControl }, //0x1f01ff - 2032127
                    { "TakeOwnership", (int)FileSystemRights.TakeOwnership }, //0x80000 - 524288
                    { "GenericWrite", 0x40000000 },
                    { "WriteData/CreateFiles", (int)FileSystemRights.WriteData }, //0x2
                    { "Modify", (int)FileSystemRights.Modify }, //0x301bf - 197055
                    { "Write", (int)FileSystemRights.Write }, //0x116 - 278
                    { "ChangePermissions", (int)FileSystemRights.ChangePermissions }, //0x40000 - 262144
                    { "AppendData/CreateDirectories", (int)FileSystemRights.AppendData }, //4
                };
            }

            else if (permissionType == PermissionType.WRITEABLE_OR_EQUIVALENT_REG)
            {
                interesting_perms = new Dictionary<string, int>()
                {
                    { "AllAccess", 0xf01ff},
                    { "GenericAll", 0x10000000},
                    { "FullControl", (int)RegistryRights.FullControl }, //983103
                    { "TakeOwnership", (int)RegistryRights.TakeOwnership }, //524288
                    { "GenericWrite", 0x40000000 },
                    { "WriteKey", (int)RegistryRights.WriteKey }, //131078
                    { "SetValue", (int)RegistryRights.SetValue }, //2
                    { "ChangePermissions", (int)RegistryRights.ChangePermissions }, //262144
                    { "CreateSubKey", (int)RegistryRights.CreateSubKey }, //4
                };
            }

            else if (permissionType == PermissionType.WRITEABLE_OR_EQUIVALENT_SVC)
            {
                // docs:
                // https://docs.microsoft.com/en-us/windows/win32/services/service-security-and-access-rights
                // https://docs.microsoft.com/en-us/troubleshoot/windows-server/windows-security/grant-users-rights-manage-services

                interesting_perms = new Dictionary<string, int>()
                {
                    { "AllAccess", 0xf01ff}, // full control
                    //{"QueryConfig" , 1},  //Grants permission to query the service's configuration.
                    {"ChangeConfig" , 2}, //Grants permission to change the service's permission.
                    //{"QueryStatus" , 4},  //Grants permission to query the service's status.
                    //{"EnumerateDependents" , 8}, //Grants permissionto enumerate the service's dependent services.
                    //{"PauseContinue" , 64}, //Grants permission to pause/continue the service.
                    //{"Interrogate" , 128},  //Grants permission to interrogate the service (i.e. ask it to report its status immediately).
                    //{"UserDefinedControl" , 256}, //Grants permission to run the service's user-defined control.
                    //{"Delete" , 65536},  //Grants permission to delete the service.
                    //{"ReadControl" , 131072}, //Grants permission to query the service's security descriptor.
                    {"WriteDac" , 0x40000},  //Grants permission to set the service's discretionary access list.
                    {"WriteOwner" , 0x80000},  //Grants permission to modify the group and owner of a service.
                    //{"Synchronize" , 1048576},
                    {"AccessSystemSecurity" , 16777216}, //The right to get or set the SACL in the object security descriptor.
                    {"GenericAll" , 0x1000_0000},
                    //{"GenericWrite" , 0x4000_0000},
                    //{"GenericExecute" , 0x2000_0000},
                    {"GenericWrite (ChangeConfig)" , 0x2_0002},
                    {"GenericExecute (Start/Stop)" , 0x2_01F0},
                    {"Start" , 0x0010}, //Grants permission to start the service.
                    {"Stop" , 0x0020},  //Grants permission to stop the service.
                    //{"GenericRead" , 2147483648}
                };
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
                    if (Directory.Exists(path))
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
            }
            catch
            {
                //Access denied to a path
            }
            return results;
        }
    }
}
