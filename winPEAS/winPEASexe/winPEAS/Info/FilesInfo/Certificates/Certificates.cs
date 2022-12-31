using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace winPEAS.Info.FilesInfo.Certificates
{
    internal class Certificates
    {
        public static IEnumerable<CertificateInfo> GetCertificateInfos()
        {
            foreach (var storeLocation in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {
                var store = new X509Store(StoreName.My, storeLocation);
                store.Open(OpenFlags.ReadOnly);

                foreach (var certificate in store.Certificates)
                {
                    var template = "";
                    var enhancedKeyUsages = new List<string>();
                    bool? keyExportable = false;

                    try
                    {
                        certificate.PrivateKey.ToXmlString(true);
                        keyExportable = true;
                    }
                    catch (Exception e)
                    {
                        keyExportable = !e.Message.Contains("not valid for use in specified state");
                    }

                    foreach (var ext in certificate.Extensions)
                    {
                        switch (ext.Oid.FriendlyName)
                        {
                            case "Enhanced Key Usage":
                                {
                                    var extUsages = ((X509EnhancedKeyUsageExtension)ext).EnhancedKeyUsages;

                                    if (extUsages.Count == 0)
                                        continue;

                                    foreach (var extUsage in extUsages)
                                    {
                                        enhancedKeyUsages.Add(extUsage.FriendlyName);
                                    }

                                    break;
                                }
                            case "Certificate Template Name":
                            case "Certificate Template Information":
                                template = ext.Format(false);
                                break;
                        }
                    }

                    yield return new CertificateInfo
                    {
                        StoreLocation = $"{storeLocation}",
                        Issuer = certificate.Issuer,
                        Subject = certificate.Subject,
                        ValidDate = certificate.NotBefore,
                        ExpiryDate = certificate.NotAfter,
                        HasPrivateKey = certificate.HasPrivateKey,
                        KeyExportable = keyExportable,
                        Template = template,
                        Thumbprint = certificate.Thumbprint,
                        EnhancedKeyUsages = enhancedKeyUsages
                    };
                }

                store.Close();
            }
        }
    }
}
