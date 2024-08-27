using System.Collections.Generic;
using System.IO;
using System.Net;
using System;

namespace winPEAS.Info.CloudInfo
{
    internal class AzureInfo : CloudInfoBase
    {
        public override string Name => "Azure VM";
        public override bool IsCloud => Directory.Exists(WINDOWS_AZURE_FOLDER);
      
        private Dictionary<string, List<EndpointData>> _endpointData = null;

        const string WINDOWS_AZURE_FOLDER = "c:\\windowsazure";
        const string AZURE_BASE_URL = "http://169.254.169.254/metadata/";
        const string API_VERSION = "2021-12-13";

        public override Dictionary<string, List<EndpointData>> EndpointDataList()
        {
            if (_endpointData == null)
            {
                _endpointData = new Dictionary<string, List<EndpointData>>();
                List<EndpointData> _endpointDataList = new List<EndpointData>();

                try
                {
                    string result;                   

                    List<Tuple<string, string, bool>> endpoints = new List<Tuple<string, string, bool>>()
                    {
                        new Tuple<string, string, bool>("Instance Details", $"instance?api-version={API_VERSION}", false),
                        new Tuple<string, string, bool>("Load Balancer details",  $"loadbalancer?api-version={API_VERSION}", false),
                        new Tuple<string, string, bool>("Management token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://management.azure.com/", true),
                        new Tuple<string, string, bool>("Graph token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://graph.microsoft.com/", true),
                        new Tuple<string, string, bool>("Vault token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://vault.azure.net/", true),
                        new Tuple<string, string, bool>("Storage token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://storage.azure.com/", true)
                    };
                   
                    if (IsAvailable)
                    {
                        
        
                        foreach (var tuple in endpoints)
                        {
                            string url = $"{AZURE_BASE_URL}{tuple.Item2}";

                            result = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection() { { "Metadata", "true" } });

                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = tuple.Item1,
                                Data = result,
                                IsAttackVector = tuple.Item3
                            });
                        }

                        
                    }
                    else
                    {
                        foreach (var endpoint in endpoints)
                        {
                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = endpoint.Item1,
                                Data = null,
                                IsAttackVector = false
                            });
                        }
                    }

                    _endpointData.Add("General", _endpointDataList);
                }
                catch (Exception ex)
                {
                }
            }

            return _endpointData;
        }

        public override bool TestConnection()
        {
            return CreateMetadataAPIRequest(AZURE_BASE_URL, "GET") != null;
        }       
    }
}
