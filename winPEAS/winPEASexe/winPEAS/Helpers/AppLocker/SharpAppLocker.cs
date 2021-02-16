using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace winPEAS.Helpers.AppLocker
{
    // https://github.com/Flangvik/SharpAppLocker

    public class SharpAppLocker
    {
        public enum PolicyType
        {
            Local,
            Domain,
            Effective
        }

        public static T DeserializeToObject<T>(string xmlData) where T : class
        {
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (StreamReader sr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(xmlData))))
            {
                return (T)ser.Deserialize(sr);
            }
        }


        public static AppLockerPolicy GetAppLockerPolicy(PolicyType policyType, string[] appLockerRuleTypes, string ldapPath = "", bool allowOnly = false, bool denyOnly = false)
        {
            // Create IAppIdPolicyHandler COM interface
            IAppIdPolicyHandler IAppHandler = new AppIdPolicyHandlerClass();
            string policies;

            switch (policyType)
            {
                case PolicyType.Local:
                case PolicyType.Domain:
                    policies = IAppHandler.GetPolicy(ldapPath);
                    break;

                case PolicyType.Effective:
                    policies = IAppHandler.GetEffectivePolicy();
                    break;

                default:
                    throw new InvalidOperationException();
            };

            var objectHolder = DeserializeToObject<AppLockerPolicy>(policies);
            AppLockerPolicy appLockerPolicyFiltered = DeserializeToObject<AppLockerPolicy>(policies);

            if (objectHolder?.RuleCollection?.Count() > 0)
            {
                //Null them all out to empty lists
                for (int i = 0; i < appLockerPolicyFiltered.RuleCollection.Length; i++)
                {
                    appLockerPolicyFiltered.RuleCollection[i].FileHashRule = new List<AppLockerPolicyRuleCollectionFileHashRule>() { };
                    appLockerPolicyFiltered.RuleCollection[i].FilePathRule = new List<AppLockerPolicyRuleCollectionFilePathRule>() { };
                    appLockerPolicyFiltered.RuleCollection[i].FilePublisherRule = new List<AppLockerPolicyRuleCollectionFilePublisherRule>() { };
                }

                for (int i = 0; i < objectHolder?.RuleCollection.Count(); i++)
                {
                    if (objectHolder?.RuleCollection[i].FilePathRule != null)
                    {
                        if (appLockerRuleTypes.Contains("All", StringComparer.InvariantCultureIgnoreCase) ||
                            appLockerRuleTypes.Contains("FilePathRule", StringComparer.InvariantCultureIgnoreCase))
                        {
                            foreach (var pathRule in objectHolder?.RuleCollection[i].FilePathRule)
                            {
                                if (allowOnly || denyOnly)
                                {
                                    if (pathRule.Action.Equals(allowOnly ? "Allow" : "Deny"))
                                    {
                                        appLockerPolicyFiltered.RuleCollection[i].FilePathRule.Add(pathRule);
                                    }
                                }
                                else
                                {
                                    appLockerPolicyFiltered.RuleCollection[i].FilePathRule.Add(pathRule);
                                }
                            }
                        }
                    }
                    if (objectHolder?.RuleCollection[i].FileHashRule != null)
                    {
                        if (appLockerRuleTypes.Contains("All", StringComparer.InvariantCultureIgnoreCase) || appLockerRuleTypes.Contains("FileHashRule", StringComparer.InvariantCultureIgnoreCase))
                        {
                            foreach (var hashRule in objectHolder?.RuleCollection[i].FileHashRule)
                            {
                                if (allowOnly || denyOnly)
                                {
                                    if (hashRule.Action.Equals(allowOnly ? "Allow" : "Deny"))
                                    {
                                        appLockerPolicyFiltered.RuleCollection[i].FileHashRule.Add(hashRule);
                                    }
                                }
                                else
                                {
                                    appLockerPolicyFiltered.RuleCollection[i].FileHashRule.Add(hashRule);
                                }
                            }
                        }
                    }
                    if (objectHolder?.RuleCollection[i].FilePublisherRule != null)
                    {
                        if (appLockerRuleTypes.Contains("All", StringComparer.InvariantCultureIgnoreCase) || appLockerRuleTypes.Contains("FilePublisherRule", StringComparer.InvariantCultureIgnoreCase))
                        {
                            foreach (var pubRile in objectHolder?.RuleCollection[i].FilePublisherRule.ToArray())
                            {
                                if (allowOnly || denyOnly)
                                {
                                    if (pubRile.Action.Equals(allowOnly ? "Allow" : "Deny"))
                                    {
                                        appLockerPolicyFiltered.RuleCollection[i].FilePublisherRule.Add(pubRile);
                                    }
                                }
                                else
                                {
                                    appLockerPolicyFiltered.RuleCollection[i].FilePublisherRule.Add(pubRile);
                                }
                            }
                        }
                    }
                }

                //Remove all the empty stuff
                appLockerPolicyFiltered.RuleCollection = appLockerPolicyFiltered.RuleCollection.Where(x =>

                    x.FilePublisherRule.Any() ||
                    x.FilePathRule.Any() ||
                    x.FileHashRule.Any()

                ).ToArray();
            }

            return appLockerPolicyFiltered;
        }
    }
}
