using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using winPEAS.Helpers;

namespace winPEAS.Info.CloudInfo
{
    internal class GCPInfo : CloudInfoBase
    {
        public override string Name => "Google Cloud Platform";

        const string GCP_BASE_URL = "http://{URL_BASE}/";
        const string GCP_FOLDER = "C:\\Program Files\\Google\\Compute Engine\\";
        
        /*
             C:\Program Files\Google\Compute Engine\agent\GCEWindowsAgent.exe"
             C:\Program Files\Google\OSConfig\google_osconfig_agent.exe"
             c:\Program Files (x86)\Google\Cloud SDK" 
             http://metadata.google.internal         
         */

        public override bool IsCloud => Directory.Exists(GCP_FOLDER);

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        const string METADATA_URL_BASE = "http://metadata.google.internal/computeMetadata/v1";


        public override Dictionary<string, List<EndpointData>> EndpointDataList()
        {
            if (_endpointData == null)
            {
                _endpointData = new Dictionary<string, List<EndpointData>>();

                try
                {
                    if (IsAvailable)
                    {
                        _endpointData.Add("GC Project Info", GetGCProjectMetadataInfo());
                        _endpointData.Add("OSLogin Info", GetOSLoginMetadataInfo());
                        _endpointData.Add("Instance Info", GetInstanceMetadataInfo());
                        _endpointData.Add("Interfaces", GetInterfacesMetadataInfo());
                        _endpointData.Add("User Data", GetUserMetadataInfo());
                        _endpointData.Add("Service Accounts", GetServiceAccountsMetadataInfo());
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

        private List<EndpointData> GetServiceAccountsMetadataInfo()
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>();

            var serviceAccountsEndpointUrlBase = "instance/service-accounts";
            var url = $"{METADATA_URL_BASE}/{serviceAccountsEndpointUrlBase}";
            var serviceAccounts = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection { { "X-Google-Metadata-Request", "True" } });

            // TODO
            //  echo "  Name: $sa"  - ignored for now

            foreach (var serviceAccount in serviceAccounts.Trim().Split('\n'))
            {
                metadataEndpoints.Add(new Tuple<string, string, bool>("Email", $"{serviceAccountsEndpointUrlBase}/{serviceAccount}email", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Aliases", $"{serviceAccountsEndpointUrlBase}/{serviceAccount}aliases", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Identity", $"{serviceAccountsEndpointUrlBase}/{serviceAccount}identity", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Scopes", $"{serviceAccountsEndpointUrlBase}/{serviceAccount}scopes", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Token", $"{serviceAccountsEndpointUrlBase}/{serviceAccount}token", false));
            }

            var result = GetMetadataInfo(metadataEndpoints);

            return result;
        }

        private List<EndpointData> GetUserMetadataInfo()
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("startup-script", "instance/attributes/startup-script", false),
            };

            var result = GetMetadataInfo(metadataEndpoints);

            return result;
        }

        private List<EndpointData> GetInterfacesMetadataInfo()
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>();

            var networkEndpointUrlBase = "instance/network-interfaces";
            var url = $"{METADATA_URL_BASE}/{networkEndpointUrlBase}";
            var ifaces = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection { { "X-Google-Metadata-Request", "True" } });            

            foreach (var iface in ifaces.Trim().Split('\n'))
            {
                metadataEndpoints.Add(new Tuple<string, string, bool>("IP", $"{networkEndpointUrlBase}/{iface}ip", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Subnetmask", $"{networkEndpointUrlBase}/{iface}subnetmask", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Gateway", $"{networkEndpointUrlBase}/{iface}gateway", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("DNS", $"{networkEndpointUrlBase}/{iface}dns-servers", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Network", $"{networkEndpointUrlBase}/{iface}network", false));
            }

            var result = GetMetadataInfo(metadataEndpoints);

            return result;
        }

        private List<EndpointData> GetInstanceMetadataInfo()
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("Instance Description", "instance/description", false),
                 new Tuple<string, string, bool>("Hostname", "instance/hostname", false),
                 new Tuple<string, string, bool>("Instance ID", "instance/id", false),
                 new Tuple<string, string, bool>("Instance Image", "instance/image", false),
                 new Tuple<string, string, bool>("Machine Type", "instance/machine-type", false),
                 new Tuple<string, string, bool>("Instance Name", "instance/name", false),
                 new Tuple<string, string, bool>("Instance tags", "instance/scheduling/tags", false),
                 new Tuple<string, string, bool>("Zone", "instance/zone", false),
                 new Tuple<string, string, bool>("K8s Cluster Location", "instance/attributes/cluster-location", false),
                 new Tuple<string, string, bool>("K8s Cluster name", "instance/attributes/cluster-name", false),
                 new Tuple<string, string, bool>("K8s OSLoging enabled", "instance/attributes/enable-oslogin", false),
                 new Tuple<string, string, bool>("K8s Kube-labels", "instance/attributes/kube-labels", false),
                 new Tuple<string, string, bool>("K8s Kubeconfig", "instance/attributes/kubeconfig", false),
                 new Tuple<string, string, bool>("K8s Kube-env", "instance/attributes/kube-env", false),
            };

            var result = GetMetadataInfo(metadataEndpoints);

            return result;

        }
        private List<EndpointData> GetOSLoginMetadataInfo()
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("OSLogin users", "oslogin/users", false),
                 new Tuple<string, string, bool>("OSLogin Groups", "oslogin/groups", false),
                 new Tuple<string, string, bool>("OSLogin Security Keys", "oslogin/security-keys", false),
                 new Tuple<string, string, bool>("OSLogin Authorize", "oslogin/authorize", false),
            };

            var result = GetMetadataInfo(metadataEndpoints);

            return result;
        }

        private List<EndpointData> GetGCProjectMetadataInfo()
        {            
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("Project-ID", "project/project-id", false),
                 new Tuple<string, string, bool>("Project Number", "project/numeric-project-id", false),
                 new Tuple<string, string, bool>("Project SSH-Keys", "project/attributes/ssh-keys", false),
                 new Tuple<string, string, bool>("All Project Attributes", "project/attributes/?recursive=true", false),
            };

            var result = GetMetadataInfo(metadataEndpoints);

            return result;
        }

        private List<EndpointData> GetMetadataInfo(List<Tuple<string, string, bool>> endpointData)
        {
            List<EndpointData> _endpointDataList = new List<EndpointData>();

            foreach (var tuple in endpointData)
            {
                string url = $"{METADATA_URL_BASE}/{tuple.Item2}";
                var result = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection { { "X-Google-Metadata-Request", "True" } });

                _endpointDataList.Add(new EndpointData()
                {
                    EndpointName = tuple.Item1,
                    Data = result?.Trim(),
                    IsAttackVector = tuple.Item3
                });
            }

            return _endpointDataList;
        }

        public override bool TestConnection()
        {
            return CreateMetadataAPIRequest(GCP_BASE_URL, "GET") != null;
        }
    }
}
