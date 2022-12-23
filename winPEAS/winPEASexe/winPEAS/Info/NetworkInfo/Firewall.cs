using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using winPEAS.Helpers;

namespace winPEAS.Info.NetworkInfo
{
    internal static class Firewall
    {
        // From Seatbelt
        [Flags]
        public enum FirewallProfiles
        {
            DOMAIN = 1,
            PRIVATE = 2,
            PUBLIC = 4,
            ALL = 2147483647
        }
        public static string GetFirewallProfiles()
        {
            string result = "";
            try
            {
                Type firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                object firewallObj = Activator.CreateInstance(firewall);
                object types = ReflectionHelper.InvokeMemberProperty(firewallObj, "CurrentProfileTypes");
                result = $"{(FirewallProfiles)int.Parse(types.ToString())}";
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return result;
        }
        public static Dictionary<string, string> GetFirewallBooleans()
        {
            var results = new Dictionary<string, string>();
            try
            {
                Type firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                object firewallObj = Activator.CreateInstance(firewall);
                object enabledDomain = ReflectionHelper.InvokeMemberProperty(firewallObj, "FirewallEnabled", new object[] { 1 });
                object enabledPrivate = ReflectionHelper.InvokeMemberProperty(firewallObj, "FirewallEnabled", new object[] { 2 });
                object enabledPublic = ReflectionHelper.InvokeMemberProperty(firewallObj, "FirewallEnabled", new object[] { 4 });

                results = new Dictionary<string, string>
                {
                    { "FirewallEnabled (Domain)", $"{enabledDomain}"},
                    { "FirewallEnabled (Private)", $"{enabledPrivate}"},
                    { "FirewallEnabled (Public)", $"{enabledPublic}"},
                };
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
        public static List<Dictionary<string, string>> GetFirewallRules()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                //Filtrado por DENY como Seatbelt??
                // GUID for HNetCfg.FwPolicy2 COM object
                Type firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                object firewallObj = Activator.CreateInstance(firewall);

                // now grab all the rules
                object rules = ReflectionHelper.InvokeMemberProperty(firewallObj, "Rules");

                // manually get the enumerator() method
                var enumerator = (System.Collections.IEnumerator)ReflectionHelper.InvokeMemberMethod(rules, "GetEnumerator");

                // move to the first item
                enumerator.MoveNext();
                object currentItem = enumerator.Current;

                while (currentItem != null)
                {
                    // only display enabled rules
                    object enabled = ReflectionHelper.InvokeMemberProperty(currentItem, "Enabled");
                    if (enabled.ToString() == "True")
                    {
                        object action = ReflectionHelper.InvokeMemberProperty(currentItem, "Action");
                        if (action.ToString() == "0") //Only DENY rules
                        {
                            // extract all of our fields
                            object name = ReflectionHelper.InvokeMemberProperty(currentItem, "Name");
                            object description = ReflectionHelper.InvokeMemberProperty(currentItem, "Description");
                            object protocol = ReflectionHelper.InvokeMemberProperty(currentItem, "Protocol");
                            object applicationName = ReflectionHelper.InvokeMemberProperty(currentItem, "ApplicationName");
                            object localAddresses = ReflectionHelper.InvokeMemberProperty(currentItem, "LocalAddresses");
                            object localPorts = ReflectionHelper.InvokeMemberProperty(currentItem, "LocalPorts");
                            object remoteAddresses = ReflectionHelper.InvokeMemberProperty(currentItem, "RemoteAddresses");
                            object remotePorts = ReflectionHelper.InvokeMemberProperty(currentItem, "RemotePorts");
                            object direction = ReflectionHelper.InvokeMemberProperty(currentItem, "Direction");
                            object profiles = ReflectionHelper.InvokeMemberProperty(currentItem, "Profiles");

                            string ruleAction = "ALLOW";
                            if (action.ToString() != "1")
                            {
                                ruleAction = "DENY";
                            }

                            string ruleDirection = "IN";
                            if (direction.ToString() != "1")
                            {
                                ruleDirection = "OUT";
                            }

                            string ruleProtocol = "TCP";
                            if (protocol.ToString() != "6")
                            {
                                ruleProtocol = "UDP";
                            }

                            var rule = new Dictionary<string, string>
                            {
                                ["Name"] = name.ToString(),
                                ["Description"] = description.ToString(),
                                ["AppName"] = applicationName.ToString(),
                                ["Protocol"] = ruleProtocol,
                                ["Action"] = ruleAction,
                                ["Direction"] = ruleDirection,
                                ["Profiles"] = int.Parse(profiles.ToString()).ToString(),
                                ["Local"] = $"{localAddresses}:{localPorts}",
                                ["Remote"] = $"{remoteAddresses}:{remotePorts}"
                            };

                            results.Add(rule);
                        }
                    }
                    // manually move the enumerator
                    enumerator.MoveNext();
                    currentItem = enumerator.Current;
                }
                Marshal.ReleaseComObject(firewallObj);
                firewallObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
