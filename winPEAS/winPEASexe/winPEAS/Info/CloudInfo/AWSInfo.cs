using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using winPEAS.Helpers;

namespace winPEAS.Info.CloudInfo
{
    internal class AWSInfo : CloudInfoBase
    {
        /*
         * notes - possible identification:
         * 
         - "c:\Program Files\Amazon\EC2Launch" 
		 - "C:\Program Files\Amazon\EC2Launch\service\EC2LaunchService.exe"
		 - "c:\Program Files (x86)\AWS SDK for .NET" 
         - get EC2_TOKEN: PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600", it should start with "AQ"
         */

        const string AWS_FOLDER = "c:\\Program Files\\Amazon\\";
        const string AWS_BASE_URL = "http://169.254.169.254/latest/api/token";
        const string METADATA_URL_BASE = "http://169.254.169.254/latest/meta-data";


        public override string Name => "AWS EC2";

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        public override bool IsCloud => Directory.Exists(AWS_FOLDER);

        public override Dictionary<string, List<EndpointData>> EndpointDataList()
        {
            if (_endpointData == null)
            {
                _endpointData = new Dictionary<string, List<EndpointData>>();

                try
                {
                    if (IsAvailable)
                    {
                        string API_TOKEN = CreateMetadataAPIRequest(AWS_BASE_URL, "PUT", new WebHeaderCollection { { "X-aws-ec2-metadata-token-ttl-seconds", "21600" } });

                        _endpointData.Add("General Info", GetGeneralMetadataInfo(API_TOKEN));
                        _endpointData.Add("Account Info", GetAccountMetadataInfo(API_TOKEN));
                        _endpointData.Add("Network Info", GetNetworkMetadataInfo(API_TOKEN));
                        _endpointData.Add("IAM Role", GetIAMRoleMetadataInfo(API_TOKEN));
                        _endpointData.Add("User Data", GetUserDataMetadataInfo(API_TOKEN));
                        _endpointData.Add("EC2 Security Credentials", GetSecurityCredentialsMetadataInfo(API_TOKEN));

                        /*
                         * print_3title "SSM Runnig"
                           ps aux 2>/dev/null | grep "ssm-agent" | grep -v "grep" | sed "s,ssm-agent,${SED_RED},"
                         * 
                         */
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

        private List<EndpointData> GetSecurityCredentialsMetadataInfo(string apiToken)
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("ec2-instance", "identity-credentials/ec2/security-credentials/ec2-instance", false),
            };

            var result = GetMetadataInfo(metadataEndpoints, apiToken);

            return result;
        }

        private List<EndpointData> GetUserDataMetadataInfo(string apiToken)
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("user-data", "latest/user-data", false),
            };

            var result = GetMetadataInfo(metadataEndpoints, apiToken);

            return result;
        }

        private List<EndpointData> GetIAMRoleMetadataInfo(string apiToken)
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>
            {
                new Tuple<string, string, bool>("iam/info", "iam/info", false)
            };

            var url = $"{METADATA_URL_BASE}/iam/security-credentials/";
            var roles = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection() { { "X-aws-ec2-metadata-token", apiToken } });

            foreach (var role in roles.Split('\n'))
            {
                metadataEndpoints.Add(new Tuple<string, string, bool>(role, $"iam/security-credentials/{role}", false));
            }           
            
            var result = GetMetadataInfo(metadataEndpoints, apiToken);

            return result;
        }

        private List<EndpointData> GetNetworkMetadataInfo(string apiToken)
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>();           
         
            var url = $"{METADATA_URL_BASE}/network/interfaces/macs/";
            var macs = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection() { { "X-aws-ec2-metadata-token", apiToken } });
            var urlBase = "network/interfaces/macs";

            foreach (var mac in macs.Split('\n'))
            {
                metadataEndpoints.Add(new Tuple<string, string, bool>("Owner ID", $"{urlBase}/{mac}/owner-id", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Public Hostname", $"{urlBase}/{mac}/public-hostname", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Security Groups", $"{urlBase}/{mac}/security-groups", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Private IPv4s", $"{urlBase}/{mac}/ipv4-associations/", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Subnet IPv4", $"{urlBase}/{mac}/subnet-ipv4-cidr-block", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Private IPv6s", $"{urlBase}/{mac}/ipv6s", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Subnet IPv6", $"{urlBase}/{mac}/subnet-ipv6-cidr-blocks", false));
                metadataEndpoints.Add(new Tuple<string, string, bool>("Public IPv4s", $"{urlBase}/{mac}/public-ipv4s", false));
            }
            var result = GetMetadataInfo(metadataEndpoints, apiToken);

            return result;
        }

        private List<EndpointData> GetAccountMetadataInfo(string apiToken)
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                 new Tuple<string, string, bool>("account info", "identity-credentials/ec2/info", false),
            };

            var result = GetMetadataInfo(metadataEndpoints, apiToken);

            return result;
        }

        private List<EndpointData> GetGeneralMetadataInfo(string apiToken)
        {
            var metadataEndpoints = new List<Tuple<string, string, bool>>()
            {
                new Tuple<string, string, bool>("ami id", "ami-id", false),
                new Tuple<string, string, bool>("instance action","instance-action", false),
                new Tuple<string, string, bool>("instance id","instance-id", false),
                new Tuple<string, string, bool>("instance life-cycle","instance-life-cycle", false),
                new Tuple<string, string, bool>("instance type","instance-type", false),
                new Tuple<string, string, bool>("placement/region","placement/region", false),
            };

            var result = GetMetadataInfo(metadataEndpoints, apiToken);

            return result;
        }

        private List<EndpointData> GetMetadataInfo(List<Tuple<string, string, bool>> endpointData, string apiToken)
        {
            List<EndpointData> _endpointDataList = new List<EndpointData>();

            foreach (var tuple in endpointData)
            {
                string url = $"{METADATA_URL_BASE}/{tuple.Item2}";

                var result = CreateMetadataAPIRequest(url, "GET", new WebHeaderCollection() { { "X-aws-ec2-metadata-token", apiToken } });

                _endpointDataList.Add(new EndpointData()
                {
                    EndpointName = tuple.Item1,
                    Data = result,
                    IsAttackVector = tuple.Item3
                });
            }

            return _endpointDataList;
        }

        public override bool TestConnection()
        {
            return CreateMetadataAPIRequest(AWS_BASE_URL, "GET") != null;
        }
    }
}
