using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo
{
    internal static class DmaProtection
    {
        private const int SystemDmaGuardPolicyInformation = 202;

        private enum ProtectionState
        {
            Unknown,
            Disabled,
            Enabled
        }

        private sealed class DeviceGuardState
        {
            internal ProtectionState DmaProtectionAvailable { get; set; } = ProtectionState.Unknown;
            internal string VbsStatus { get; set; } = "Unavailable";
        }

        private sealed class BitLockerState
        {
            internal bool Available { get; set; }
            internal bool ProtectionEnabled { get; set; }
            internal List<uint> ProtectorTypes { get; } = new List<uint>();
        }

        internal static void PrintInfo()
        {
            Beaprint.MainPrint("Hardware and DMA Security", "T1200");
            Beaprint.LinkPrint(
                "https://learn.microsoft.com/en-us/windows/security/hardware-security/kernel-dma-protection-for-thunderbolt",
                "Kernel DMA Protection uses the IOMMU to restrict DMA-capable peripherals");
            Beaprint.LinkPrint(
                "https://mdsec.co.uk/2026/03/disabling-security-features-in-a-locked-bios",
                "Physical firmware tampering can make visible UEFI settings differ from effective DMA protection");

            ProtectionState kernelDmaProtection = GetKernelDmaProtectionState();
            DeviceGuardState deviceGuard = GetDeviceGuardState();
            string firmwareType = GetFirmwareType();
            ProtectionState secureBoot = GetSecureBootState();
            BitLockerState bitLocker = GetBitLockerState();
            List<string> dmaInterfaces = GetDmaCapableInterfaces();

            PrintProtectionState("Kernel DMA Protection", kernelDmaProtection);
            PrintProtectionState("DMA protection available (Device Guard)", deviceGuard.DmaProtectionAvailable);
            PrintProtectionState("Secure Boot", secureBoot);
            Beaprint.InfoPrint("    Firmware type: " + firmwareType);

            if (deviceGuard.VbsStatus == "Enabled and running")
            {
                Beaprint.GoodPrint("    Virtualization Based Security: " + deviceGuard.VbsStatus);
            }
            else if (deviceGuard.VbsStatus == "Not enabled" || deviceGuard.VbsStatus == "Enabled but not running")
            {
                Beaprint.BadPrint("    Virtualization Based Security: " + deviceGuard.VbsStatus);
            }
            else
            {
                Beaprint.InfoPrint("    Virtualization Based Security: " + deviceGuard.VbsStatus);
            }

            PrintBitLockerState(bitLocker);

            if (dmaInterfaces.Count > 0)
            {
                Beaprint.InfoPrint("    DMA-capable external interfaces/devices:");
                foreach (string device in dmaInterfaces)
                {
                    Beaprint.BadPrint("        " + device);
                }
            }
            else
            {
                Beaprint.InfoPrint("    DMA-capable external interfaces/devices: none identified");
            }

            bool tpmOnly = bitLocker.ProtectionEnabled && bitLocker.ProtectorTypes.Contains(1);
            bool dmaExposure = kernelDmaProtection == ProtectionState.Disabled && dmaInterfaces.Count > 0;

            if (dmaExposure || (tpmOnly && kernelDmaProtection == ProtectionState.Disabled))
            {
                Beaprint.BadPrint("    Potential physical DMA escalation exposure: Kernel DMA Protection is disabled" +
                    (tpmOnly ? " and the OS volume has a TPM-only BitLocker protector." : "."));
                if (deviceGuard.VbsStatus != "Enabled and running")
                {
                    Beaprint.BadPrint("    VBS is also not confirmed running, reducing defense in depth for the described attack chain.");
                }
                Beaprint.InfoPrint("    Exploitation requires physical access, firmware/ACPI tampering, and specialized DMA hardware.");
            }
            else
            {
                Beaprint.InfoPrint("    No high-risk physical DMA condition was identified from the available runtime data.");
            }

            Beaprint.InfoPrint("    Runtime checks cannot prove that pre-boot firmware settings or ACPI tables have not been modified offline.");
        }

        private static ProtectionState GetKernelDmaProtectionState()
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                buffer = Marshal.AllocHGlobal(1);
                Marshal.WriteByte(buffer, 0);
                int returnLength = 0;
                uint status = Native.Ntdll.NtQuerySystemInformation(
                    SystemDmaGuardPolicyInformation,
                    buffer,
                    1,
                    ref returnLength);

                if (status != 0)
                {
                    return ProtectionState.Unknown;
                }

                return Marshal.ReadByte(buffer) == 0 ? ProtectionState.Disabled : ProtectionState.Enabled;
            }
            catch
            {
                return ProtectionState.Unknown;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        private static DeviceGuardState GetDeviceGuardState()
        {
            DeviceGuardState state = new DeviceGuardState();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"root\Microsoft\Windows\DeviceGuard",
                    "SELECT AvailableSecurityProperties, VirtualizationBasedSecurityStatus FROM Win32_DeviceGuard"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        List<uint> properties = ToUInt32List(result["AvailableSecurityProperties"]);
                        state.DmaProtectionAvailable = properties.Contains(3)
                            ? ProtectionState.Enabled
                            : ProtectionState.Disabled;

                        uint vbsStatus = Convert.ToUInt32(result["VirtualizationBasedSecurityStatus"] ?? 0);
                        switch (vbsStatus)
                        {
                            case 0:
                                state.VbsStatus = "Not enabled";
                                break;
                            case 1:
                                state.VbsStatus = "Enabled but not running";
                                break;
                            case 2:
                                state.VbsStatus = "Enabled and running";
                                break;
                            default:
                                state.VbsStatus = "Unknown";
                                break;
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Older Windows versions may not provide the Device Guard namespace.
            }
            return state;
        }

        private static string GetFirmwareType()
        {
            try
            {
                uint firmwareType;
                if (!Native.Kernel32.GetFirmwareType(out firmwareType))
                {
                    return "Unknown";
                }

                switch (firmwareType)
                {
                    case 1:
                        return "Legacy BIOS";
                    case 2:
                        return "UEFI";
                    default:
                        return "Unknown";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private static ProtectionState GetSecureBootState()
        {
            try
            {
                string value = RegistryHelper.GetRegValue(
                    "HKLM",
                    @"SYSTEM\CurrentControlSet\Control\SecureBoot\State",
                    "UEFISecureBootEnabled");

                if (value == "1")
                {
                    return ProtectionState.Enabled;
                }
                if (value == "0")
                {
                    return ProtectionState.Disabled;
                }
            }
            catch
            {
                // The key is absent on legacy BIOS and some older Windows versions.
            }
            return ProtectionState.Unknown;
        }

        private static BitLockerState GetBitLockerState()
        {
            BitLockerState state = new BitLockerState();
            string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? string.Empty;

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"root\CIMV2\Security\MicrosoftVolumeEncryption",
                    "SELECT * FROM Win32_EncryptableVolume"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject volume in results)
                    {
                        string driveLetter = Convert.ToString(volume["DriveLetter"]);
                        if (!string.Equals(driveLetter, systemDrive, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        state.Available = true;
                        state.ProtectionEnabled = Convert.ToUInt32(volume["ProtectionStatus"] ?? 0) == 1;

                        using (ManagementBaseObject inParameters = volume.GetMethodParameters("GetKeyProtectors"))
                        {
                            inParameters["KeyProtectorType"] = 0;
                            using (ManagementBaseObject outParameters = volume.InvokeMethod("GetKeyProtectors", inParameters, null))
                            {
                                if (outParameters == null || Convert.ToUInt32(outParameters["ReturnValue"] ?? 1) != 0)
                                {
                                    break;
                                }

                                string[] protectorIds = outParameters["VolumeKeyProtectorID"] as string[];
                                foreach (string protectorId in protectorIds ?? new string[0])
                                {
                                    using (ManagementBaseObject typeInput = volume.GetMethodParameters("GetKeyProtectorType"))
                                    {
                                        typeInput["VolumeKeyProtectorID"] = protectorId;
                                        using (ManagementBaseObject typeOutput = volume.InvokeMethod("GetKeyProtectorType", typeInput, null))
                                        {
                                            if (typeOutput != null && Convert.ToUInt32(typeOutput["ReturnValue"] ?? 1) == 0)
                                            {
                                                state.ProtectorTypes.Add(Convert.ToUInt32(typeOutput["KeyProtectorType"]));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
            catch
            {
                // BitLocker WMI can be unavailable or access restricted.
            }

            return state;
        }

        private static List<string> GetDmaCapableInterfaces()
        {
            string[] indicators =
            {
                "Thunderbolt", "USB4", "FireWire", "IEEE 1394", "1394 OHCI",
                "ExpressCard", "PCMCIA", "CardBus", "CFexpress"
            };
            var devices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name, Description, Service FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject device in results)
                    {
                        string name = Convert.ToString(device["Name"]);
                        string searchable = string.Join(" ", new[]
                        {
                            name,
                            Convert.ToString(device["Description"]),
                            Convert.ToString(device["Service"])
                        });

                        if (indicators.Any(indicator =>
                            searchable.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            devices.Add(string.IsNullOrWhiteSpace(name) ? searchable.Trim() : name);
                        }
                    }
                }
            }
            catch
            {
                // Device enumeration is supplementary; retain the other results.
            }

            return devices.OrderBy(device => device, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<uint> ToUInt32List(object value)
        {
            var values = new List<uint>();
            Array array = value as Array;
            if (array == null)
            {
                return values;
            }

            foreach (object item in array)
            {
                values.Add(Convert.ToUInt32(item));
            }
            return values;
        }

        private static void PrintProtectionState(string name, ProtectionState state)
        {
            switch (state)
            {
                case ProtectionState.Enabled:
                    Beaprint.GoodPrint("    " + name + ": enabled");
                    break;
                case ProtectionState.Disabled:
                    Beaprint.BadPrint("    " + name + ": disabled/not available");
                    break;
                default:
                    Beaprint.InfoPrint("    " + name + ": unavailable");
                    break;
            }
        }

        private static void PrintBitLockerState(BitLockerState state)
        {
            if (!state.Available)
            {
                Beaprint.InfoPrint("    OS volume BitLocker: unavailable or access denied");
                return;
            }

            if (!state.ProtectionEnabled)
            {
                Beaprint.BadPrint("    OS volume BitLocker protection: off");
                return;
            }

            Beaprint.GoodPrint("    OS volume BitLocker protection: on");
            string[] protectors = state.ProtectorTypes
                .Distinct()
                .Select(GetProtectorName)
                .ToArray();

            if (protectors.Length == 0)
            {
                Beaprint.InfoPrint("    BitLocker protectors: unavailable");
                return;
            }

            string protectorList = string.Join(", ", protectors);
            if (state.ProtectorTypes.Contains(1))
            {
                Beaprint.BadPrint("    BitLocker protectors: " + protectorList + " (TPM-only permits unattended boot)");
            }
            else
            {
                Beaprint.GoodPrint("    BitLocker protectors: " + protectorList);
            }
        }

        private static string GetProtectorName(uint type)
        {
            switch (type)
            {
                case 1: return "TPM";
                case 2: return "External key";
                case 3: return "Recovery password";
                case 4: return "TPM+PIN";
                case 5: return "TPM+startup key";
                case 6: return "TPM+PIN+startup key";
                case 7: return "Public key";
                case 8: return "Passphrase";
                case 9: return "TPM certificate";
                case 10: return "CNG protector";
                default: return "Unknown (" + type + ")";
            }
        }
    }
}
