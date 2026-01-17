using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using winPEAS.Helpers;

namespace winPEAS.Info.ServicesInfo
{
    internal static class OemSoftwareHelper
    {
        internal static List<OemSoftwareFinding> GetPotentiallyVulnerableComponents(Dictionary<string, string> currentUserSids)
        {
            var findings = new List<OemSoftwareFinding>();
            var services = GetServiceSnapshot();
            var processes = GetProcessSnapshot();

            foreach (var definition in GetDefinitions())
            {
                var finding = new OemSoftwareFinding
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    Cves = definition.Cves,
                };

                AppendServiceEvidence(definition, services, finding);
                AppendProcessEvidence(definition, processes, finding);
                AppendPathEvidence(definition, currentUserSids, finding);
                AppendPipeEvidence(definition, finding);

                if (finding.Evidence.Count > 0)
                {
                    findings.Add(finding);
                }
            }

            return findings;
        }

        private static void AppendServiceEvidence(OemComponentDefinition definition, List<ServiceSnapshot> services, OemSoftwareFinding finding)
        {
            if (definition.ServiceHints == null || definition.ServiceHints.Length == 0)
            {
                return;
            }

            foreach (var serviceHint in definition.ServiceHints)
            {
                foreach (var service in services)
                {
                    if (ContainsIgnoreCase(service.Name, serviceHint) ||
                        ContainsIgnoreCase(service.DisplayName, serviceHint))
                    {
                        finding.Evidence.Add(new OemEvidence
                        {
                            EvidenceType = "service",
                            Highlight = true,
                            Message = $"Service '{service.Name}' (Display: {service.DisplayName}) matches indicator '{serviceHint}'"
                        });
                    }
                }
            }
        }

        private static void AppendProcessEvidence(OemComponentDefinition definition, List<ProcessSnapshot> processes, OemSoftwareFinding finding)
        {
            if (definition.ProcessHints == null || definition.ProcessHints.Length == 0)
            {
                return;
            }

            foreach (var processHint in definition.ProcessHints)
            {
                foreach (var process in processes)
                {
                    bool matchesName = ContainsIgnoreCase(process.Name, processHint);
                    bool matchesPath = ContainsIgnoreCase(process.FullPath, processHint);

                    if (matchesName || matchesPath)
                    {
                        var location = string.IsNullOrWhiteSpace(process.FullPath) ? "Unknown" : process.FullPath;
                        finding.Evidence.Add(new OemEvidence
                        {
                            EvidenceType = "process",
                            Highlight = true,
                            Message = $"Process '{process.Name}' (Path: {location}) matches indicator '{processHint}'"
                        });
                    }
                }
            }
        }

        private static void AppendPathEvidence(OemComponentDefinition definition, Dictionary<string, string> currentUserSids, OemSoftwareFinding finding)
        {
            if ((definition.DirectoryHints == null || definition.DirectoryHints.Length == 0) &&
                (definition.FileHints == null || definition.FileHints.Length == 0))
            {
                return;
            }

            if (definition.DirectoryHints != null)
            {
                foreach (var dirHint in definition.DirectoryHints)
                {
                    var expandedPath = ExpandPath(dirHint.Path);
                    if (!Directory.Exists(expandedPath))
                    {
                        continue;
                    }

                    var permissions = PermissionsHelper.GetPermissionsFolder(expandedPath, currentUserSids, PermissionType.WRITEABLE_OR_EQUIVALENT);
                    bool isWritable = permissions.Count > 0;

                    finding.Evidence.Add(new OemEvidence
                    {
                        EvidenceType = "path",
                        Highlight = isWritable,
                        Message = BuildPathMessage(expandedPath, dirHint.Description, isWritable, permissions)
                    });
                }
            }

            if (definition.FileHints != null)
            {
                foreach (var fileHint in definition.FileHints)
                {
                    var expandedPath = ExpandPath(fileHint);
                    if (!File.Exists(expandedPath))
                    {
                        continue;
                    }

                    var permissions = PermissionsHelper.GetPermissionsFile(expandedPath, currentUserSids, PermissionType.WRITEABLE_OR_EQUIVALENT);
                    bool isWritable = permissions.Count > 0;

                    finding.Evidence.Add(new OemEvidence
                    {
                        EvidenceType = "file",
                        Highlight = isWritable,
                        Message = BuildPathMessage(expandedPath, "file", isWritable, permissions)
                    });
                }
            }
        }

        private static void AppendPipeEvidence(OemComponentDefinition definition, OemSoftwareFinding finding)
        {
            if (definition.PipeHints == null)
            {
                return;
            }

            foreach (var pipeHint in definition.PipeHints)
            {
                try
                {
                    var path = $"\\\\.\\pipe\\{pipeHint.Name}";
                    var security = File.GetAccessControl(path);
                    string sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                    string identity = string.Empty;
                    string rights = string.Empty;
                    bool worldWritable = false;

                    if (pipeHint.CheckWorldWritable)
                    {
                        worldWritable = HasWorldWritableAce(security, out identity, out rights);
                    }

                    string details = worldWritable
                        ? $"Named pipe '{pipeHint.Name}' ({pipeHint.Description}) is writable by {identity} ({rights})."
                        : $"Named pipe '{pipeHint.Name}' ({pipeHint.Description}) present. SDDL: {sddl}";

                    finding.Evidence.Add(new OemEvidence
                    {
                        EvidenceType = "pipe",
                        Highlight = worldWritable,
                        Message = details
                    });
                }
                catch (FileNotFoundException)
                {
                    // Pipe not present.
                }
                catch (DirectoryNotFoundException)
                {
                    // Pipe namespace not accessible.
                }
                catch (Exception)
                {
                    // Best effort: pipes might disappear during enumeration or deny access.
                }
            }
        }

        private static List<ServiceSnapshot> GetServiceSnapshot()
        {
            var services = new List<ServiceSnapshot>();

            try
            {
                foreach (var service in ServiceController.GetServices())
                {
                    services.Add(new ServiceSnapshot
                    {
                        Name = service.ServiceName ?? string.Empty,
                        DisplayName = service.DisplayName ?? string.Empty
                    });
                }
            }
            catch (Exception)
            {
                // Ignore - this is best effort.
            }

            return services;
        }

        private static List<ProcessSnapshot> GetProcessSnapshot()
        {
            var processes = new List<ProcessSnapshot>();

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    string fullPath = string.Empty;
                    try
                    {
                        fullPath = process.MainModule?.FileName ?? string.Empty;
                    }
                    catch
                    {
                        // Access denied or 64-bit vs 32-bit mismatch.
                    }

                    processes.Add(new ProcessSnapshot
                    {
                        Name = process.ProcessName ?? string.Empty,
                        FullPath = fullPath ?? string.Empty
                    });
                }
            }
            catch (Exception)
            {
                // Ignore - enumeration is best effort.
            }

            return processes;
        }

        private static string ExpandPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            var expanded = Environment.ExpandEnvironmentVariables(rawPath);
            return expanded.Trim().Trim('"');
        }

        private static string BuildPathMessage(string path, string description, bool isWritable, List<string> permissions)
        {
            string descriptor = string.IsNullOrWhiteSpace(description) ? "" : $" ({description})";
            if (isWritable)
            {
                return $"Path '{path}'{descriptor} is writable by current user: {string.Join(", ", permissions)}";
            }

            return $"Path '{path}'{descriptor} detected.";
        }

        private static bool ContainsIgnoreCase(string value, string toFind)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(toFind))
            {
                return false;
            }

            return value.IndexOf(toFind, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasWorldWritableAce(FileSecurity security, out string identity, out string rights)
        {
            identity = string.Empty;
            rights = string.Empty;

            try
            {
                var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule.AccessControlType != AccessControlType.Allow)
                    {
                        continue;
                    }

                    if (rule.IdentityReference is SecurityIdentifier sid)
                    {
                        bool isWorld = sid.IsWellKnown(WellKnownSidType.WorldSid);
                        bool isAuthenticated = sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid);

                        if (!isWorld && !isAuthenticated)
                        {
                            continue;
                        }

                        const FileSystemRights interestingRights =
                            FileSystemRights.FullControl |
                            FileSystemRights.Modify |
                            FileSystemRights.Write |
                            FileSystemRights.WriteData |
                            FileSystemRights.CreateFiles |
                            FileSystemRights.ChangePermissions;

                        if ((rule.FileSystemRights & interestingRights) != 0)
                        {
                            identity = isWorld ? "Everyone" : "Authenticated Users";
                            rights = rule.FileSystemRights.ToString();
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing issues.
            }

            return false;
        }

        private static IEnumerable<OemComponentDefinition> GetDefinitions()
        {
            return new List<OemComponentDefinition>
            {
                new OemComponentDefinition
                {
                    Name = "ASUS DriverHub",
                    Description = "Local web API exposed by ADU.exe allowed bypassing origin/url validation and signature checks.",
                    Cves = new[] { "CVE-2025-3462", "CVE-2025-3463" },
                    ServiceHints = new[] { "asusdriverhub", "asus driverhub" },
                    ProcessHints = new[] { "adu", "asusdriverhub" },
                    DirectoryHints = new[]
                    {
                        new PathHint { Path = "%ProgramFiles%\\ASUS\\AsusDriverHub", Description = "Program Files" },
                        new PathHint { Path = "%ProgramFiles(x86)%\\ASUS\\AsusDriverHub", Description = "Program Files (x86)" },
                        new PathHint { Path = "%ProgramData%\\ASUS\\AsusDriverHub\\SupportTemp", Description = "SupportTemp updater staging" }
                    },
                    FileHints = new[]
                    {
                        "%ProgramData%\\ASUS\\AsusDriverHub\\SupportTemp\\Installer.json"
                    }
                },
                new OemComponentDefinition
                {
                    Name = "MSI Center",
                    Description = "MSI.CentralServer.exe exposed TCP commands with TOCTOU and signature bypass issues.",
                    Cves = new[] { "CVE-2025-27812", "CVE-2025-27813" },
                    ServiceHints = new[] { "msi.center", "msi centralserver" },
                    ProcessHints = new[] { "msi.centralserver", "msi center" },
                    DirectoryHints = new[]
                    {
                        new PathHint { Path = "%ProgramFiles%\\MSI\\MSI Center", Description = "Main installation" },
                        new PathHint { Path = "%ProgramFiles(x86)%\\MSI\\MSI Center", Description = "Main installation (x86)" },
                        new PathHint { Path = "%ProgramData%\\MSI\\MSI Center", Description = "Shared data" },
                        new PathHint { Path = "%ProgramData%\\MSI Center SDK", Description = "SDK temp copy location" }
                    }
                },
                new OemComponentDefinition
                {
                    Name = "Acer Control Centre",
                    Description = "ACCSvc.exe exposes treadstone_service_LightMode named pipe with weak impersonation controls.",
                    Cves = new[] { "CVE-2025-5491" },
                    ServiceHints = new[] { "accsvc", "acer control" },
                    ProcessHints = new[] { "accsvc", "accstd" },
                    DirectoryHints = new[]
                    {
                        new PathHint { Path = "%ProgramFiles%\\Acer\\Care Center", Description = "Install directory" },
                        new PathHint { Path = "%ProgramFiles(x86)%\\Acer\\Care Center", Description = "Install directory (x86)" }
                    },
                    PipeHints = new[]
                    {
                        new PipeHint { Name = "treadstone_service_LightMode", Description = "Command dispatcher", CheckWorldWritable = true }
                    }
                },
                new OemComponentDefinition
                {
                    Name = "Razer Synapse 4 Elevation Service",
                    Description = "razer_elevation_service.exe exposes COM elevation helpers that allowed arbitrary process launch.",
                    Cves = new[] { "CVE-2025-27811" },
                    ServiceHints = new[] { "razer_elevation_service" },
                    ProcessHints = new[] { "razer_elevation_service" },
                    DirectoryHints = new[]
                    {
                        new PathHint { Path = "%ProgramFiles%\\Razer\\RazerAppEngine", Description = "Razer App Engine" },
                        new PathHint { Path = "%ProgramFiles(x86)%\\Razer\\RazerAppEngine", Description = "Razer App Engine (x86)" }
                    }
                }
            };
        }

        private class ServiceSnapshot
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
        }

        private class ProcessSnapshot
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
        }

        private class OemComponentDefinition
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string[] Cves { get; set; } = Array.Empty<string>();
            public string[] ServiceHints { get; set; } = Array.Empty<string>();
            public string[] ProcessHints { get; set; } = Array.Empty<string>();
            public PathHint[] DirectoryHints { get; set; } = Array.Empty<PathHint>();
            public string[] FileHints { get; set; } = Array.Empty<string>();
            public PipeHint[] PipeHints { get; set; } = Array.Empty<PipeHint>();
        }

        private class PathHint
        {
            public string Path { get; set; }
            public string Description { get; set; }
        }

        private class PipeHint
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool CheckWorldWritable { get; set; }
        }
    }

    internal class OemSoftwareFinding
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Cves { get; set; } = Array.Empty<string>();
        public List<OemEvidence> Evidence { get; } = new List<OemEvidence>();
    }

    internal class OemEvidence
    {
        public string EvidenceType { get; set; }
        public string Message { get; set; }
        public bool Highlight { get; set; }
    }
}
