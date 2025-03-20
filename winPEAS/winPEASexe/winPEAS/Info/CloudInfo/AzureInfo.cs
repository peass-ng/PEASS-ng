using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace winPEAS.Info.CloudInfo
{
    internal class AzureInfo : CloudInfoBase
    {
        // The Name property now differentiates between an Azure VM and an Azure Container.
        public override string Name
        {
            get
            {
                if (IsContainer())
                    return "Azure Container"; // **Container environment detected**
                return "Azure VM"; // **VM environment detected**
            }
        }

        // IsCloud now returns true if either the Azure VM folder exists or container env vars are set.
        public override bool IsCloud => Directory.Exists(WINDOWS_AZURE_FOLDER) || IsContainer();

        private Dictionary<string, List<EndpointData>> _endpointData = null;

        const string WINDOWS_AZURE_FOLDER = "c:\\windowsazure";
        const string AZURE_BASE_URL = "http://169.254.169.254/metadata/";
        const string API_VERSION = "2021-12-13";
        const string CONTAINER_API_VERSION = "2019-08-01";

        public static bool DoesProcessExist(string processName)
        {
            // Return false if the process name is null or empty
            if (string.IsNullOrEmpty(processName))
            {
                return false;
            }

            // Retrieve all processes matching the specified name
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        // New helper method to detect if running inside an Azure container
        private bool IsContainer()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSI_ENDPOINT"));
        }

        public override Dictionary<string, List<EndpointData>> EndpointDataList()
        {
            if (_endpointData == null)
            {
                _endpointData = new Dictionary<string, List<EndpointData>>();
                List<EndpointData> _endpointDataList = new List<EndpointData>();

                try
                {
                    string result;
                    List<Tuple<string, string, bool>> endpoints;

                    if (IsContainer())
                    {
                        // **Running in Azure Container: use the container endpoint and add the "Secret" header if available**
                        string containerBaseUrl = Environment.GetEnvironmentVariable("MSI_ENDPOINT") ??
                                                  Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
                        endpoints = new List<Tuple<string, string, bool>>()
                        {
                            new Tuple<string, string, bool>("Management token", $"?api-version={CONTAINER_API_VERSION}&resource=https://management.azure.com/", true),
                            new Tuple<string, string, bool>("Graph token", $"?api-version={CONTAINER_API_VERSION}&resource=https://graph.microsoft.com/", true),
                            new Tuple<string, string, bool>("Vault token", $"?api-version={CONTAINER_API_VERSION}&resource=https://vault.azure.net/", true),
                            new Tuple<string, string, bool>("Storage token", $"?api-version={CONTAINER_API_VERSION}&resource=https://storage.azure.com/", true)
                        };

                        foreach (var tuple in endpoints)
                        {
                            // Ensure proper URL formatting.
                            string url = $"{containerBaseUrl}{(containerBaseUrl.EndsWith("/") ? "" : "/")}{tuple.Item2}";
                            var headers = new WebHeaderCollection();
                            string msiSecret = Environment.GetEnvironmentVariable("MSI_SECRET");
                            if (!string.IsNullOrEmpty(msiSecret))
                            {
                                headers.Add("Secret", msiSecret);
                            }
                            string identitySecret = Environment.GetEnvironmentVariable("IDENTITY_HEADER");
                            if (!string.IsNullOrEmpty(identitySecret))
                            {
                                headers.Add("X-IDENTITY-HEADER", identitySecret);
                            }
                            result = CreateMetadataAPIRequest(url, "GET", headers);
                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = tuple.Item1,
                                Data = result,
                                IsAttackVector = tuple.Item3
                            });
                        }
                    }
                    else if (Directory.Exists(WINDOWS_AZURE_FOLDER))
                    {
                        // **Running in Azure VM: use the standard metadata endpoint with "Metadata: true" header**
                        endpoints = new List<Tuple<string, string, bool>>()
                        {
                            new Tuple<string, string, bool>("Instance Details", $"instance?api-version={API_VERSION}", false),
                            new Tuple<string, string, bool>("Load Balancer details",  $"loadbalancer?api-version={API_VERSION}", false),
                            new Tuple<string, string, bool>("Management token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://management.azure.com/", true),
                            new Tuple<string, string, bool>("Graph token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://graph.microsoft.com/", true),
                            new Tuple<string, string, bool>("Vault token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://vault.azure.net/", true),
                            new Tuple<string, string, bool>("Storage token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://storage.azure.com/", true)
                        };

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
                        // If neither container nor VM, endpoints remain unset.
                        foreach (var endpoint in new List<Tuple<string, string, bool>>())
                        {
                            _endpointDataList.Add(new EndpointData()
                            {
                                EndpointName = endpoint.Item1,
                                Data = null,
                                IsAttackVector = false
                            });
                        }
                    }

                    string hwsRun = DoesProcessExist("HybridWorkerService") ? "Yes" : "No";
                    _endpointDataList.Add(new EndpointData()
                    {
                        EndpointName = "HybridWorkerService.exe Running",
                        Data = hwsRun,
                        IsAttackVector = true
                    });

                    string OSRun = DoesProcessExist("Orchestrator.Sandbox") ? "Yes" : "No";
                    _endpointDataList.Add(new EndpointData()
                    {
                        EndpointName = "Orchestrator.Sandbox.exe Running",
                        Data = OSRun,
                        IsAttackVector = true
                    });

                    _endpointData.Add("General", _endpointDataList);
                }
                catch (Exception ex)
                {
                    // **Exception handling (e.g., logging) can be added here**
                }
            }

            return _endpointData;
        }

        public override bool TestConnection()
        {
            if (IsContainer())
            {
                // **Test connection for Azure Container**
                string containerBaseUrl = Environment.GetEnvironmentVariable("MSI_ENDPOINT") ??
                                          Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
                if (string.IsNullOrEmpty(containerBaseUrl))
                    return false;
                var headers = new WebHeaderCollection();
                string msiSecret = Environment.GetEnvironmentVariable("MSI_SECRET");
                if (!string.IsNullOrEmpty(msiSecret))
                {
                    headers.Add("Secret", msiSecret);
                }
                string identitySecret = Environment.GetEnvironmentVariable("IDENTITY_HEADER");
                if (!string.IsNullOrEmpty(identitySecret))
                {
                    headers.Add("X-IDENTITY-HEADER", identitySecret);
                }
                return CreateMetadataAPIRequest(containerBaseUrl, "GET", headers) != null;
            }
            else
            {
                // **Test connection for Azure VM**
                return CreateMetadataAPIRequest(AZURE_BASE_URL, "GET", new WebHeaderCollection() { { "Metadata", "true" } }) != null;
            }
        }
    }
}
