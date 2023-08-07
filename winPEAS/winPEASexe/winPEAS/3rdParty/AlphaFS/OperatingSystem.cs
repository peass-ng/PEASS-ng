/*  Copyright (C) 2008-2018 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32
{
   /// <summary>Static class providing access to information about the operating system under which the assembly is executing.</summary>
   public static class OperatingSystem
   {
      /// <summary>A set of flags that describe the named Windows versions.</summary>
      /// <remarks>The values of the enumeration are ordered. A later released operating system version has a higher number, so comparisons between named versions are meaningful.</remarks>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Os")]
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Os")]
      public enum EnumOsName
      {
         /// <summary>A Windows version earlier than Windows 2000.</summary>
         Earlier = -1,

         /// <summary>Windows 2000 (Server or Professional).</summary>
         Windows2000 = 0,

         /// <summary>Windows XP.</summary>
         WindowsXP = 1,

         /// <summary>Windows Server 2003.</summary>
         WindowsServer2003 = 2,

         /// <summary>Windows Vista.</summary>
         WindowsVista = 3,

         /// <summary>Windows Server 2008.</summary>
         WindowsServer2008 = 4,

         /// <summary>Windows 7.</summary>
         Windows7 = 5,

         /// <summary>Windows Server 2008 R2.</summary>
         WindowsServer2008R2 = 6,

         /// <summary>Windows 8.</summary>
         Windows8 = 7,

         /// <summary>Windows Server 2012.</summary>
         WindowsServer2012 = 8,

         /// <summary>Windows 8.1.</summary>
         Windows81 = 9,

         /// <summary>Windows Server 2012 R2</summary>
         WindowsServer2012R2 = 10,

         /// <summary>Windows 10</summary>
         Windows10 = 11,

         /// <summary>Windows Server 2016</summary>
         WindowsServer2016 = 12,

         /// <summary>A later version of Windows than currently installed.</summary>
         Later = 65535
      }


      /// <summary>A set of flags to indicate the current processor architecture for which the operating system is targeted and running.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Pa")]
      [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]      
      public enum EnumProcessorArchitecture 
      {
         /// <summary>PROCESSOR_ARCHITECTURE_INTEL
         /// <para>The system is running a 32-bit version of Windows.</para>
         /// </summary>
         X86 = 0,

         /// <summary>PROCESSOR_ARCHITECTURE_IA64
         /// <para>The system is running on a Itanium processor.</para>
         /// </summary>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ia")]
         [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ia")]
         IA64 = 6,

         /// <summary>PROCESSOR_ARCHITECTURE_AMD64
         /// <para>The system is running a 64-bit version of Windows.</para>
         /// </summary>
         X64 = 9,

         /// <summary>PROCESSOR_ARCHITECTURE_UNKNOWN
         /// <para>Unknown architecture.</para>
         /// </summary>
         Unknown = 65535
      }


      
      
      #region Properties

      private static bool _isServer;
      /// <summary>Gets a value indicating whether the operating system is a server operating system.</summary>
      /// <value><c>true</c> if the current operating system is a server operating system; otherwise, <c>false</c>.</value>
      public static bool IsServer
      {
         get
         {
            if (null == _servicePackVersion)
               UpdateData();

            return _isServer;
         }
      }


      private static bool? _isWow64Process;
      /// <summary>Gets a value indicating whether the current process is running under WOW64.</summary>
      /// <value><c>true</c> if the current process is running under WOW64; otherwise, <c>false</c>.</value>
      public static bool IsWow64Process
      {
         get
         {
            if (null == _isWow64Process)
            {
               bool value;
               var processHandle = Process.GetCurrentProcess().Handle;

               if (!NativeMethods.IsWow64Process(processHandle, out value))
                  Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());

               // A pointer to a value that is set to TRUE if the process is running under WOW64.
               // If the process is running under 32-bit Windows, the value is set to FALSE.
               // If the process is a 64-bit application running under 64-bit Windows, the value is also set to FALSE.

               _isWow64Process = value;
            }

            return (bool) _isWow64Process;
         }
      }


      private static Version _osVersion;
      /// <summary>Gets the numeric version of the operating system.</summary>            
      /// <value>The numeric version of the operating system.</value>
      public static Version OSVersion
      {
         get
         {
            if (null == _osVersion)
               UpdateData();

            return _osVersion;
         }
      }


      private static EnumOsName _enumOsName = EnumOsName.Later;
      /// <summary>Gets the named version of the operating system.</summary>
      /// <value>The named version of the operating system.</value>
      public static EnumOsName VersionName
      {
         get
         {
            if (null == _servicePackVersion)
               UpdateData();

            return _enumOsName;
         }
      }


      private static EnumProcessorArchitecture _processorArchitecture;
      /// <summary>Gets the processor architecture for which the operating system is targeted.</summary>
      /// <value>The processor architecture for which the operating system is targeted.</value>
      /// <remarks>If running under WOW64 this will return a 32-bit processor. Use <see cref="IsWow64Process"/> to determine if this is the case.</remarks>      
      public static EnumProcessorArchitecture ProcessorArchitecture
      {
         get
         {
            if (null == _servicePackVersion)
               UpdateData();

            return _processorArchitecture;
         }
      }


      private static Version _servicePackVersion;
      /// <summary>Gets the version of the service pack currently installed on the operating system.</summary>
      /// <value>The version of the service pack currently installed on the operating system.</value>
      /// <remarks>Only the <see cref="System.Version.Major"/> and <see cref="System.Version.Minor"/> fields are used.</remarks>
      public static Version ServicePackVersion
      {
         get
         {
            if (null == _servicePackVersion)
               UpdateData();

            return _servicePackVersion;
         }
      }

      #endregion // Properties


      #region Methods

      /// <summary>Determines whether the operating system is of the specified version or later.</summary>
      /// <returns><c>true</c> if the operating system is of the specified <paramref name="version"/> or later; otherwise, <c>false</c>.</returns>      
      /// <param name="version">The lowest version for which to return true.</param>
      public static bool IsAtLeast(EnumOsName version)
      {
         return VersionName >= version;
      }

      
      /// <summary>Determines whether the operating system is of the specified version or later, allowing specification of a minimum service pack that must be installed on the lowest version.</summary>
      /// <returns><c>true</c> if the operating system matches the specified <paramref name="version"/> with the specified service pack, or if the operating system is of a later version; otherwise, <c>false</c>.</returns>      
      /// <param name="version">The minimum required version.</param>
      /// <param name="servicePackVersion">The major version of the service pack that must be installed on the minimum required version to return true. This can be 0 to indicate that no service pack is required.</param>
      public static bool IsAtLeast(EnumOsName version, int servicePackVersion)
      {
         return IsAtLeast(version) && ServicePackVersion.Major >= servicePackVersion;
      }
      
      #endregion // Methods


      #region Private

      [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "RtlGetVersion")]
      private static void UpdateData()
      {
         var verInfo = new NativeMethods.RTL_OSVERSIONINFOEXW();

         // Needed to prevent: System.Runtime.InteropServices.COMException:
         // The data area passed to a system call is too small. (Exception from HRESULT: 0x8007007A)
         verInfo.dwOSVersionInfoSize = Marshal.SizeOf(verInfo);

         var sysInfo = new NativeMethods.SYSTEM_INFO();
         NativeMethods.GetNativeSystemInfo(ref sysInfo);


         // RtlGetVersion returns STATUS_SUCCESS (0).
         var success = !NativeMethods.RtlGetVersion(ref verInfo);
         
         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            throw new Win32Exception(lastError, "Function RtlGetVersion() failed to retrieve the operating system information.");


         _osVersion = new Version(verInfo.dwMajorVersion, verInfo.dwMinorVersion, verInfo.dwBuildNumber);

         _processorArchitecture = sysInfo.wProcessorArchitecture;
         _servicePackVersion = new Version(verInfo.wServicePackMajor, verInfo.wServicePackMinor);
         _isServer = verInfo.wProductType == NativeMethods.VER_NT_DOMAIN_CONTROLLER || verInfo.wProductType == NativeMethods.VER_NT_SERVER;


         // RtlGetVersion: https://msdn.microsoft.com/en-us/library/windows/hardware/ff561910%28v=vs.85%29.aspx
         // Operating System Version: https://msdn.microsoft.com/en-us/library/windows/desktop/ms724832(v=vs.85).aspx

         // The following table summarizes the most recent operating system version numbers.
         //    Operating system	            Version number    Other
         // ================================================================================
         //    Windows 10                    10.0              OSVERSIONINFOEX.wProductType == VER_NT_WORKSTATION
         //    Windows Server 2016           10.0              OSVERSIONINFOEX.wProductType != VER_NT_WORKSTATION
         //    Windows 8.1                   6.3               OSVERSIONINFOEX.wProductType == VER_NT_WORKSTATION
         //    Windows Server 2012 R2        6.3               OSVERSIONINFOEX.wProductType != VER_NT_WORKSTATION
         //    Windows 8	                  6.2               OSVERSIONINFOEX.wProductType == VER_NT_WORKSTATION
         //    Windows Server 2012	         6.2               OSVERSIONINFOEX.wProductType != VER_NT_WORKSTATION
         //    Windows 7	                  6.1               OSVERSIONINFOEX.wProductType == VER_NT_WORKSTATION
         //    Windows Server 2008 R2	      6.1               OSVERSIONINFOEX.wProductType != VER_NT_WORKSTATION
         //    Windows Server 2008	         6.0               OSVERSIONINFOEX.wProductType != VER_NT_WORKSTATION  
         //    Windows Vista	               6.0               OSVERSIONINFOEX.wProductType == VER_NT_WORKSTATION
         //    Windows Server 2003 R2	      5.2               GetSystemMetrics(SM_SERVERR2) != 0
         //    Windows Server 2003           5.2               GetSystemMetrics(SM_SERVERR2) == 0
         //    Windows XP 64-Bit Edition     5.2               (OSVERSIONINFOEX.wProductType == VER_NT_WORKSTATION) && (sysInfo.PaName == PaName.X64)
         //    Windows XP	                  5.1               Not applicable
         //    Windows 2000	               5.0               Not applicable


         // 2017-01-07: 10 == The lastest MajorVersion of Windows.
         if (verInfo.dwMajorVersion > 10)
            _enumOsName = EnumOsName.Later;

         else
            switch (verInfo.dwMajorVersion)
            {
               #region Version 10

               case 10:

                  // Windows 10 or Windows Server 2016

                  _enumOsName = verInfo.wProductType == NativeMethods.VER_NT_WORKSTATION
                     ? EnumOsName.Windows10
                     : EnumOsName.WindowsServer2016;

                  break;
                  

               #endregion // Version 10


               #region Version 6

               case 6:
                  switch (verInfo.dwMinorVersion)
                  {
                     // Windows 8.1 or Windows Server 2012 R2
                     case 3:
                        _enumOsName = verInfo.wProductType == NativeMethods.VER_NT_WORKSTATION
                           ? EnumOsName.Windows81
                           : EnumOsName.WindowsServer2012R2;
                        break;


                     // Windows 8 or Windows Server 2012
                     case 2:
                        _enumOsName = verInfo.wProductType == NativeMethods.VER_NT_WORKSTATION
                           ? EnumOsName.Windows8
                           : EnumOsName.WindowsServer2012;
                        break;


                     // Windows 7 or Windows Server 2008 R2
                     case 1:
                        _enumOsName = verInfo.wProductType == NativeMethods.VER_NT_WORKSTATION
                           ? EnumOsName.Windows7
                           : EnumOsName.WindowsServer2008R2;
                        break;


                     // Windows Vista or Windows Server 2008
                     case 0:
                        _enumOsName = verInfo.wProductType == NativeMethods.VER_NT_WORKSTATION
                           ? EnumOsName.WindowsVista
                           : EnumOsName.WindowsServer2008;
                        break;
                        

                     default:
                        _enumOsName = EnumOsName.Later;
                        break;
                  }

                  break;

               #endregion // Version 6


               #region Version 5

               case 5:
                  switch (verInfo.dwMinorVersion)
                  {
                     case 2:
                        _enumOsName = verInfo.wProductType == NativeMethods.VER_NT_WORKSTATION && _processorArchitecture == EnumProcessorArchitecture.X64
                           ? EnumOsName.WindowsXP
                           : verInfo.wProductType != NativeMethods.VER_NT_WORKSTATION ? EnumOsName.WindowsServer2003 : EnumOsName.Later;
                        break;


                     case 1:
                        _enumOsName = EnumOsName.WindowsXP;
                        break;


                     case 0:
                        _enumOsName = EnumOsName.Windows2000;
                        break;


                     default:
                        _enumOsName = EnumOsName.Later;
                        break;
                  }
                  break;

               #endregion // Version 5


               default:
                  _enumOsName = EnumOsName.Earlier;
                  break;
            }
      }
      

      private static class NativeMethods
      {
         internal const short VER_NT_WORKSTATION = 1;
         internal const short VER_NT_DOMAIN_CONTROLLER = 2;
         internal const short VER_NT_SERVER = 3;


         [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
         internal struct RTL_OSVERSIONINFOEXW
         {
            public int dwOSVersionInfoSize;
            public readonly int dwMajorVersion;
            public readonly int dwMinorVersion;
            public readonly int dwBuildNumber;
            public readonly int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public readonly string szCSDVersion;
            public readonly ushort wServicePackMajor;
            public readonly ushort wServicePackMinor;
            public readonly ushort wSuiteMask;
            public readonly byte wProductType;
            public readonly byte wReserved;
         }


         [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
         internal struct SYSTEM_INFO
         {
            public readonly EnumProcessorArchitecture wProcessorArchitecture;
            private readonly ushort wReserved;
            public readonly uint dwPageSize;
            public readonly IntPtr lpMinimumApplicationAddress;
            public readonly IntPtr lpMaximumApplicationAddress;
            public readonly IntPtr dwActiveProcessorMask;
            public readonly uint dwNumberOfProcessors;
            public readonly uint dwProcessorType;
            public readonly uint dwAllocationGranularity;
            public readonly ushort wProcessorLevel;
            public readonly ushort wProcessorRevision;
         }


         /// <summary>The RtlGetVersion routine returns version information about the currently running operating system.</summary>
         /// <returns>RtlGetVersion returns STATUS_SUCCESS.</returns>
         /// <remarks>Available starting with Windows 2000.</remarks>
         [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule"), DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
         [return: MarshalAs(UnmanagedType.Bool)]
         internal static extern bool RtlGetVersion([MarshalAs(UnmanagedType.Struct)] ref RTL_OSVERSIONINFOEXW lpVersionInformation);


         /// <summary>Retrieves information about the current system to an application running under WOW64.
         /// If the function is called from a 64-bit application, it is equivalent to the GetSystemInfo function.
         /// </summary>
         /// <returns>This function does not return a value.</returns>
         /// <remarks>To determine whether a Win32-based application is running under WOW64, call the <see cref="IsWow64Process"/> function.</remarks>
         /// <remarks>Minimum supported client: Windows XP [desktop apps | Windows Store apps]</remarks>
         /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps | Windows Store apps]</remarks>
         [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
         [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
         internal static extern void GetNativeSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);


         /// <summary>Determines whether the specified process is running under WOW64.</summary>
         /// <returns>
         /// If the function succeeds, the return value is a nonzero value.
         /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
         /// </returns>
         /// <remarks>Minimum supported client: Windows Vista, Windows XP with SP2 [desktop apps only]</remarks>
         /// <remarks>Minimum supported server: Windows Server 2008, Windows Server 2003 with SP1 [desktop apps only]</remarks>
         [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
         [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
         [return: MarshalAs(UnmanagedType.Bool)]
         internal static extern bool IsWow64Process([In] IntPtr hProcess, [Out, MarshalAs(UnmanagedType.Bool)] out bool lpSystemInfo);
      }

      #endregion // Private
   }
}
