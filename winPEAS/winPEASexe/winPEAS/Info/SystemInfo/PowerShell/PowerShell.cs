using System.Collections.Generic;
using System.Security.AccessControl;
using System.Xml;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo.PowerShell
{
    internal class PowerShell
    {
        public static IEnumerable<PowerShellSessionSettingsInfo> GetPowerShellSessionSettingsInfos()
        {
            var plugins = new[] { "Microsoft.PowerShell", "Microsoft.PowerShell.Workflow", "Microsoft.PowerShell32" };

            foreach (var plugin in plugins)
            {
                var config = RegistryHelper.GetRegValue("HKLM", $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WSMAN\\Plugin\\{plugin}", "ConfigXML");

                if (config == null) continue;

                var access = new List<PluginAccessInfo>();

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(config);
                var security = xmlDoc.GetElementsByTagName("Security");

                if (security.Count <= 0)
                    continue;

                foreach (XmlAttribute attr in security[0].Attributes)
                {
                    if (attr.Name != "Sddl")
                    {
                        continue;
                    }

                    var desc = new RawSecurityDescriptor(attr.Value);
                    foreach (QualifiedAce ace in desc.DiscretionaryAcl)
                    {
                        var principal = ace.SecurityIdentifier.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                        var accessStr = ace.AceQualifier.ToString();

                        access.Add(new PluginAccessInfo(
                            principal,
                            ace.SecurityIdentifier.ToString(),
                            accessStr
                        ));
                    }
                }

                yield return new PowerShellSessionSettingsInfo(plugin, access);
            }
        }
    }
}
