using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using winPEAS.Helpers;
using System.Data.SQLite;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Modes;
using System.Linq;
using Microsoft.Win32;
using System.Web.Script.Serialization;


namespace winPEAS.Info.CloudInfo
{
    internal class GCDSInfo : CloudInfoBase
    {
        public override string Name => "Google Cloud Directory Sync";

        public override bool IsCloud => CheckIfGCDSInstalled();

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        public static bool CheckIfGCDSInstalled()
        {
            string[] check = Helpers.Registry.RegistryHelper.GetRegSubkeys("HKCU", @"SOFTWARE\JavaSoft\Prefs\com\google\usersyncapp\util");
            bool regExists = check != null && check.Length > 0;
            bool result = regExists || File.Exists(@"C:\Program Files\Google Cloud Directory Sync\config-manager.exe");
            return result;
        }

        private List<EndpointData> GetGCDSRegValues()
        {
            Dictionary<string, string> GCDSRegValues = new Dictionary<string, string>();
            GCDSRegValues.Add("V2.configured", Helpers.Registry.RegistryHelper.GetRegValue("HKCU", @"SOFTWARE\JavaSoft\Prefs\com\google\usersyncapp\util", @"/Encryption/Policy/V2.configured"));
            GCDSRegValues.Add("V2.iv", Helpers.Registry.RegistryHelper.GetRegValue("HKCU", @"SOFTWARE\JavaSoft\Prefs\com\google\usersyncapp\util", @"/Encryption/Policy/V2.iv").Replace("/", "").Replace("\\","/"));
            GCDSRegValues.Add("V2.key", Helpers.Registry.RegistryHelper.GetRegValue("HKCU", @"SOFTWARE\JavaSoft\Prefs\com\google\usersyncapp\util", @"/Encryption/Policy/V2.key").Replace("/", "").Replace("\\", "/"));
            string openRecent = Helpers.Registry.RegistryHelper.GetRegValue("HKCU", @"SOFTWARE\JavaSoft\Prefs\com\google\usersyncapp\ui", @"open.recent");
            GCDSRegValues.Add("Open recent confs", Helpers.Registry.RegistryHelper.GetRegValue("HKCU", @"SOFTWARE\JavaSoft\Prefs\com\google\usersyncapp\ui", @"open.recent"));

            List<string> filePaths = new List<string>(openRecent.Split(new string[] { "/u000a" }, StringSplitOptions.None));

            foreach (var filePath in filePaths)
            {
                // Normalize the path by replacing triple slashes and double slashes with single slashes
                string normalizedPath = filePath.Replace("///", "/").Replace("//", "/");

                // Remove any leading slashes that shouldn't be there
                if (normalizedPath.StartsWith("/"))
                {
                    normalizedPath = normalizedPath.Substring(1);
                }

                // Check if file exists
                if (File.Exists(normalizedPath))
                {
                    try
                    {
                        // Read and print the file content
                        string fileContent = File.ReadAllText(normalizedPath);
                        List<EndpointData> _endpointDataList_cust = new List<EndpointData>();
                        _endpointDataList_cust.Add(new EndpointData()
                        {
                            EndpointName = @"Content",
                            Data = fileContent,
                            IsAttackVector = false
                        });
                        _endpointData.Add(normalizedPath, _endpointDataList_cust);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException($"Could not open file {normalizedPath}: {ex.Message}");
                    }
                }
                else
                {
                    Beaprint.PrintException($"File {normalizedPath} does not exist.");
                }
            }

            // Format the info in expected CloudInfo format
            List<EndpointData> _endpointDataList = new List<EndpointData>();

            foreach (var kvp in GCDSRegValues)
            {
                _endpointDataList.Add(new EndpointData()
                {
                    EndpointName = kvp.Key,
                    Data = kvp.Value?.Trim(),
                    IsAttackVector = false
                });
            }

            return _endpointDataList;
        }
        

        public override Dictionary<string, List<EndpointData>> EndpointDataList()
        {
            if (_endpointData == null)
            {
                _endpointData = new Dictionary<string, List<EndpointData>>();

                try
                {
                    if (IsAvailable)
                    {
                        _endpointData.Add("Local Info", GetGCDSRegValues());
                    }
                    else
                    {
                        _endpointData.Add("General Info", new List<EndpointData>()
                        {
                            new EndpointData()
                            {
                                EndpointName = "",
                                Data = null,
                                IsAttackVector = false
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
            }

            return _endpointData;
        }

        public override bool TestConnection()
        {
            return true;
        }
    }
}
