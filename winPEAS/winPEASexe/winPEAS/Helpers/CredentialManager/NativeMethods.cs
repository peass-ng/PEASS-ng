using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using winPEAS.Native;

namespace winPEAS.Helpers.CredentialManager
{
    /// <summary>
    ///     Wrapper for advapi32.dll library.
    ///     Advanced Services
    ///     Provide access to functionality additional to the kernel.
    ///     Included are things like the Windows registry, shutdown/restart the system (or abort),
    ///     start/stop/create a Windows service, manage user accounts.
    ///     These functions reside in advapi32.dll on 32-bit Windows.
    /// </summary>
    public class NativeMethods
    {

        /// <summary>
        /// The CREDENTIAL structure contains an individual credential.
        /// 
        /// See CREDENTIAL structure <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx">documentation.</see>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
            public long LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
        }

        internal static IEnumerable<CREDENTIAL> CredEnumerate()
        {
            int count;
            IntPtr pCredentials;
            var ret = Advapi32.CredEnumerate(null, 0, out count, out pCredentials);

            if (ret == false)
            {
                string exceptionDetails = string.Format("Win32Exception: {0}", new Win32Exception(Marshal.GetLastWin32Error()).ToString());
                Beaprint.NoColorPrint($"  [!] Unable to enumerate credentials automatically, error: '{exceptionDetails}'");
                Beaprint.NoColorPrint("Please run: ");
                Beaprint.ColorPrint("cmdkey /list", Beaprint.ansi_color_yellow);
                return Enumerable.Empty<CREDENTIAL>();
            }

            var credentials = new IntPtr[count];
            for (var n = 0; n < count; n++)
                credentials[n] = Marshal.ReadIntPtr(pCredentials,
                    n * Marshal.SizeOf(typeof(IntPtr)));

            return credentials.Select(ptr => (CREDENTIAL)Marshal.PtrToStructure(ptr, typeof(CREDENTIAL)));
        }

        internal sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
        {
            // Set the handle.
            internal CriticalCredentialHandle(IntPtr preexistingHandle)
            {
                SetHandle(preexistingHandle);
            }

            internal CREDENTIAL GetCredential()
            {
                if (!IsInvalid)
                {
                    // Get the Credential from the mem location
                    return (CREDENTIAL)Marshal.PtrToStructure(handle, typeof(CREDENTIAL));
                }

                throw new InvalidOperationException("Invalid CriticalHandle!");
            }

            // Perform any specific actions to release the handle in the ReleaseHandle method.
            // Often, you need to use P/Invoke to make a call into the Win32 API to release the 
            // handle. In this case, however, we can use the Marshal class to release the unmanaged memory.
            protected override bool ReleaseHandle()
            {
                // If the handle was set, free it. Return success.
                if (!IsInvalid)
                {
                    // NOTE: We should also ZERO out the memory allocated to the handle, before free'ing it
                    // so there are no traces of the sensitive data left in memory.
                    Advapi32.CredFree(handle);
                    // Mark the handle as invalid for future users.
                    SetHandleAsInvalid();
                    return true;
                }
                // Return false. 
                return false;
            }
        }
    }
}
