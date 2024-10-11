using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace winPEAS.Helpers.Registry
{
    static class RegistryHelper
    {
        ///////////////////////////////////////////
        /// Interf. for Keys and Values in Reg. ///
        ///////////////////////////////////////////
        /// Functions related to obtain keys and values from the registry
        /// Some parts adapted from Seatbelt
        public static Microsoft.Win32.RegistryKey GetReg(string hive, string path)
        {
            if (hive == "HKCU")
                return Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);

            else if (hive == "HKU")
                return Microsoft.Win32.Registry.Users.OpenSubKey(path);

            else
                return Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
        }

        public static bool WriteRegValue(string hive, string path, string keyName, string value)
        {
            try
            {
                RegistryKey regKey;
                if (hive == "HKCU")
                {
                    regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
                }
                else if (hive == "HKU")
                {
                    regKey = Microsoft.Win32.Registry.Users.OpenSubKey(path);

                }
                else
                {
                    regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                }

                if (regKey == null)
                {
                    return false;
                }

                regKey.SetValue(keyName, value, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public static string GetRegValue(string hive, string path, string value)
        {
            // returns a single registry value under the specified path in the specified hive (HKLM/HKCU)
            string regKeyValue = "";
            if (hive == "HKCU")
            {
                var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = string.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Microsoft.Win32.Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = string.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = string.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
        }

        public static Dictionary<string, object> GetRegValues(string hive, string path)
        {
            // returns all registry values under the specified path in the specified hive (HKLM/HKCU)
            Dictionary<string, object> keyValuePairs = null;
            try
            {
                if (hive == "HKCU")
                {
                    using (var regKeyValues = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                else if (hive == "HKU")
                {
                    using (var regKeyValues = Microsoft.Win32.Registry.Users.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                else
                {
                    using (var regKeyValues = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                return keyValuePairs;
            }
            catch
            {
                return null;
            }
        }

        public static string[] ListRegValues(string hive, string path)
        {
            string[] keys = null;
            try
            {
                if (hive == "HKCU")
                {
                    using (var regKeyValues = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            keys = regKeyValues.GetValueNames();
                        }
                    }
                }
                else if (hive == "HKU")
                {
                    using (var regKeyValues = Microsoft.Win32.Registry.Users.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            keys = regKeyValues.GetValueNames();
                        }
                    }
                }
                else
                {
                    using (var regKeyValues = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            keys = regKeyValues.GetValueNames();
                        }
                    }
                }
                return keys;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] GetRegValueBytes(string hive, string path, string value)
        {
            // returns a byte array of single registry value under the specified path in the specified hive (HKLM/HKCU)
            byte[] regKeyValue = null;
            if (hive == "HKCU")
            {
                var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Microsoft.Win32.Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
        }

        public static string[] GetRegSubkeys(string hive, string path)
        {
            // returns an array of the subkeys names under the specified path in the specified hive (HKLM/HKCU/HKU)
            try
            {
                RegistryKey myKey = null;
                if (hive == "HKLM")
                {
                    myKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                }
                else if (hive == "HKU")
                {
                    myKey = Microsoft.Win32.Registry.Users.OpenSubKey(path);
                }
                else
                {
                    myKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
                }

                if (myKey == null)
                {
                    return new string[0];
                }

                String[] subkeyNames = myKey.GetSubKeyNames();
                return myKey.GetSubKeyNames();
            }
            catch
            {
                return new string[0];
            }
        }

        public static string[] GetUserSIDs()
        {
            return Microsoft.Win32.Registry.Users.GetSubKeyNames() ?? new string[] { };
        }

        internal static uint? GetDwordValue(string hive, string key, string val)
        {
            string strValue = GetRegValue(hive, key, val);

            if (uint.TryParse(strValue, out uint res))
            {
                return res;
            }

            return null;
        }

        public static string CheckIfExists(string path)
        {
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                if (key != null)
                    return "HKLM";

                key = Microsoft.Win32.Registry.Users.OpenSubKey(path);
                if (key != null)
                    return "HKU";

                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
                if (key != null)
                    return "HKCU";

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
