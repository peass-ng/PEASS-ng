using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

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
        const string ARM_VM_API_VERSION = "2024-07-01";

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
                            result = CreateMetadataAPIRequest(url, "GET", CreateContainerHeaders());
                            AddEndpointData(_endpointDataList, tuple.Item1, result, tuple.Item3);
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
                            result = CreateMetadataAPIRequest(url, "GET", CreateAzureVmHeaders());
                            AddEndpointData(_endpointDataList, tuple.Item1, result, tuple.Item3);
                        }

                        AddAzureVmUserAssignedIdentityTokens(_endpointDataList);
                    }
                    else
                    {
                        // If neither container nor VM, endpoints remain unset.
                        foreach (var endpoint in new List<Tuple<string, string, bool>>())
                        {
                            AddEndpointData(_endpointDataList, endpoint.Item1, null, false);
                        }
                    }

                    string hwsRun = DoesProcessExist("HybridWorkerService") ? "Yes" : "No";
                    AddEndpointData(_endpointDataList, "HybridWorkerService.exe Running", hwsRun, true);

                    string OSRun = DoesProcessExist("Orchestrator.Sandbox") ? "Yes" : "No";
                    AddEndpointData(_endpointDataList, "Orchestrator.Sandbox.exe Running", OSRun, true);

                    _endpointData.Add("General", _endpointDataList);
                }
                catch (Exception ex)
                {
                    // **Exception handling (e.g., logging) can be added here**
                }
            }

            return _endpointData;
        }

        private void AddAzureVmUserAssignedIdentityTokens(List<EndpointData> endpointDataList)
        {
            AddEndpointData(
                endpointDataList,
                "Managed identity discovery note",
                "winPEAS can request default managed identity tokens directly from IMDS. To discover every attached user-assigned identity, it tries to read the VM ARM identity block with the default Management token. If that token lacks Microsoft.Compute/virtualMachines/read, IMDS can still issue tokens for known client_id/object_id/msi_res_id values, but the full attached identity list cannot be discovered from IMDS alone.",
                false);

            string instanceJson = CreateMetadataAPIRequest(
                $"{AZURE_BASE_URL}instance?api-version={API_VERSION}",
                "GET",
                CreateAzureVmHeaders());
            string vmResourceId = GetJsonString(instanceJson, "compute", "resourceId");

            string managementTokenJson = CreateMetadataAPIRequest(
                $"{AZURE_BASE_URL}identity/oauth2/token?api-version={API_VERSION}&resource=https://management.azure.com/",
                "GET",
                CreateAzureVmHeaders());
            string managementToken = GetJsonString(managementTokenJson, "access_token");

            if (string.IsNullOrEmpty(vmResourceId) || string.IsNullOrEmpty(managementToken))
            {
                AddEndpointData(
                    endpointDataList,
                    "Attached user-assigned managed identities",
                    "Could not obtain the VM resource ID or default Management token needed for ARM identity discovery.",
                    false);
                AddAzureVmWireServerIdentityTokens(endpointDataList);
                return;
            }

            string armUrl = $"https://management.azure.com{vmResourceId}?api-version={ARM_VM_API_VERSION}";
            string vmJson = CreateMetadataAPIRequest(
                armUrl,
                "GET",
                new WebHeaderCollection() { { "Authorization", $"Bearer {managementToken}" } });

            if (string.IsNullOrEmpty(vmJson))
            {
                AddEndpointData(
                    endpointDataList,
                    "Attached user-assigned managed identities",
                    "Could not read the VM identity block from ARM with the default managed identity token.",
                    false);
                AddAzureVmWireServerIdentityTokens(endpointDataList);
                return;
            }

            JsonNode root;
            try
            {
                root = JsonNode.Parse(vmJson);
            }
            catch
            {
                AddEndpointData(endpointDataList, "Attached user-assigned managed identities", vmJson, false);
                AddAzureVmWireServerIdentityTokens(endpointDataList);
                return;
            }

            JsonNode identityNode = root?["identity"];
            JsonObject userAssignedIdentities = identityNode?["userAssignedIdentities"] as JsonObject;

            AddEndpointData(
                endpointDataList,
                "VM ARM identity block",
                identityNode?.ToJsonString() ?? "No identity block found in ARM VM response.",
                false);

            if (userAssignedIdentities == null || userAssignedIdentities.Count == 0)
            {
                AddAzureVmWireServerIdentityTokens(endpointDataList);
                return;
            }

            foreach (var identity in userAssignedIdentities)
            {
                string identityResourceId = identity.Key;
                string clientId = identity.Value?["clientId"]?.GetValue<string>();
                string principalId = identity.Value?["principalId"]?.GetValue<string>();

                if (string.IsNullOrEmpty(clientId))
                {
                    continue;
                }

                AddEndpointData(endpointDataList, $"User-assigned MI {clientId}", $"ResourceId: {identityResourceId}\nPrincipalId: {principalId}", false);
                AddAzureVmTokenResponses(endpointDataList, $"&client_id={Uri.EscapeDataString(clientId)}", $"for UAI {clientId}");
            }
        }

        private void AddAzureVmWireServerIdentityTokens(List<EndpointData> endpointDataList)
        {
            AddEndpointData(
                endpointDataList,
                "WireServer/HostGAPlugin managed identity fallback note",
                "ARM identity discovery failed or returned no user-assigned identities. Trying WireServer GoalState and HostGAPlugin /vmSettings for identity-looking selectors. These endpoints are environment-dependent and may expose no managed identity data.",
                false);

            string wireData = "";
            wireData += CreateMetadataAPIRequest(
                "http://168.63.129.16/machine?comp=goalstate",
                "GET",
                new WebHeaderCollection() { { "x-ms-version", "2012-11-30" } });
            wireData += "\n";
            wireData += CreateMetadataAPIRequest(
                "http://168.63.129.16/machine/?comp=goalstate",
                "GET",
                new WebHeaderCollection() { { "x-ms-version", "2012-11-30" } });
            wireData += "\n";
            wireData += CreateMetadataAPIRequest(
                "http://168.63.129.16:32526/vmSettings",
                "GET");

            if (string.IsNullOrEmpty(wireData))
            {
                AddEndpointData(
                    endpointDataList,
                    "WireServer/HostGAPlugin managed identity fallback",
                    "WireServer/HostGAPlugin did not return data from this context.",
                    false);
                return;
            }

            HashSet<string> clientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> resourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CollectWireServerIdentitySelectors(wireData, clientIds, resourceIds);

            AddEndpointData(endpointDataList, "WireServer/HostGAPlugin identity-looking hints", GetWireServerIdentityHints(wireData), false);

            foreach (string clientId in clientIds)
            {
                AddEndpointData(endpointDataList, $"WireServer-discovered client_id {clientId}", "Trying IMDS tokens for this client_id.", false);
                AddAzureVmTokenResponses(endpointDataList, $"&client_id={Uri.EscapeDataString(clientId)}", $"for WireServer client_id {clientId}");
            }

            foreach (string resourceId in resourceIds)
            {
                AddEndpointData(endpointDataList, $"WireServer-discovered msi_res_id {resourceId}", "Trying IMDS tokens for this msi_res_id.", false);
                AddAzureVmTokenResponses(endpointDataList, $"&msi_res_id={Uri.EscapeDataString(resourceId)}", "for WireServer msi_res_id");
            }
        }

        private static WebHeaderCollection CreateContainerHeaders()
        {
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

            return headers;
        }

        private static WebHeaderCollection CreateAzureVmHeaders()
        {
            return new WebHeaderCollection() { { "Metadata", "true" } };
        }

        private static void AddEndpointData(List<EndpointData> endpointDataList, string endpointName, string data, bool isAttackVector)
        {
            endpointDataList.Add(new EndpointData()
            {
                EndpointName = endpointName,
                Data = data,
                IsAttackVector = isAttackVector
            });
        }

        private void AddAzureVmTokenResponses(List<EndpointData> endpointDataList, string selectorSuffix, string endpointNameSuffix)
        {
            foreach (var tokenEndpoint in GetAzureVmTokenEndpoints(selectorSuffix))
            {
                string result = CreateMetadataAPIRequest(
                    $"{AZURE_BASE_URL}{tokenEndpoint.Item2}",
                    "GET",
                    CreateAzureVmHeaders());

                AddEndpointData(endpointDataList, $"{tokenEndpoint.Item1} {endpointNameSuffix}", result, true);
            }
        }

        private static void CollectWireServerIdentitySelectors(string wireData, HashSet<string> clientIds, HashSet<string> resourceIds)
        {
            TryCollectWireServerJsonSelectors(wireData, clientIds, resourceIds);

            foreach (Match match in Regex.Matches(wireData, @"(?i)(clientId|IdentityClientId|client_id)[^0-9a-fA-F]{0,80}([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})"))
            {
                clientIds.Add(match.Groups[2].Value);
            }

            foreach (Match match in Regex.Matches(wireData, @"(?i)/subscriptions/[^""<>\s]+/resourceGroups/[^""<>\s]+/providers/Microsoft\.ManagedIdentity/userAssignedIdentities/[^""<>\s]+"))
            {
                resourceIds.Add(match.Value);
            }
        }

        private static void TryCollectWireServerJsonSelectors(string wireData, HashSet<string> clientIds, HashSet<string> resourceIds)
        {
            try
            {
                JsonNode root = JsonNode.Parse(wireData);
                CollectJsonIdentitySelectors(root, clientIds, resourceIds);
            }
            catch
            {
            }
        }

        private static void CollectJsonIdentitySelectors(JsonNode node, HashSet<string> clientIds, HashSet<string> resourceIds)
        {
            if (node == null)
            {
                return;
            }

            if (node is JsonObject obj)
            {
                foreach (var prop in obj)
                {
                    string value = null;
                    try
                    {
                        value = prop.Value?.GetValue<string>();
                    }
                    catch
                    {
                    }
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Regex.IsMatch(prop.Key, @"(?i)(clientId|IdentityClientId)$") && Regex.IsMatch(value, @"^[0-9a-fA-F-]{36}$"))
                        {
                            clientIds.Add(value);
                        }
                        if (Regex.IsMatch(value, @"(?i)/subscriptions/.+/providers/Microsoft\.ManagedIdentity/userAssignedIdentities/"))
                        {
                            resourceIds.Add(value);
                        }
                    }
                    CollectJsonIdentitySelectors(prop.Value, clientIds, resourceIds);
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (JsonNode child in arr)
                {
                    CollectJsonIdentitySelectors(child, clientIds, resourceIds);
                }
            }
        }

        private static string GetWireServerIdentityHints(string wireData)
        {
            List<string> hints = new List<string>();
            foreach (Match match in Regex.Matches(wireData, @"(?i)([A-Za-z0-9_./:-]*Identity[A-Za-z0-9_./:-]*|Microsoft\.ManagedIdentity/userAssignedIdentities/[^""<>\s]+|clientId["":=\s]+[0-9a-fA-F-]{36}|IdentityClientId[^0-9a-fA-F]*[0-9a-fA-F-]{36})"))
            {
                if (!hints.Contains(match.Value))
                {
                    hints.Add(match.Value);
                }
                if (hints.Count >= 80)
                {
                    break;
                }
            }
            return hints.Count > 0 ? string.Join("\n", hints) : "No identity-looking strings found in WireServer/HostGAPlugin responses.";
        }

        private static List<Tuple<string, string, bool>> GetAzureVmTokenEndpoints(string selectorSuffix = "")
        {
            return new List<Tuple<string, string, bool>>()
            {
                new Tuple<string, string, bool>("Management token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://management.azure.com/{selectorSuffix}", true),
                new Tuple<string, string, bool>("Graph token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://graph.microsoft.com/{selectorSuffix}", true),
                new Tuple<string, string, bool>("Vault token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://vault.azure.net/{selectorSuffix}", true),
                new Tuple<string, string, bool>("Storage token", $"identity/oauth2/token?api-version={API_VERSION}&resource=https://storage.azure.com/{selectorSuffix}", true)
            };
        }

        private static string GetJsonString(string json, params string[] path)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                JsonNode current = JsonNode.Parse(json);
                foreach (string key in path)
                {
                    current = current?[key];
                    if (current == null)
                    {
                        return null;
                    }
                }
                return current.GetValue<string>();
            }
            catch
            {
                return null;
            }
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
                return CreateMetadataAPIRequest(containerBaseUrl, "GET", CreateContainerHeaders()) != null;
            }
            else
            {
                // **Test connection for Azure VM**
                return CreateMetadataAPIRequest(AZURE_BASE_URL, "GET", CreateAzureVmHeaders()) != null;
            }
        }
    }
}
