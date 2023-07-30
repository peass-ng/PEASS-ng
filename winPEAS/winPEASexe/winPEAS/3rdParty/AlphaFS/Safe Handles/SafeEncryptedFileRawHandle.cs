using System;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Represents a wrapper class for a handle used by the OpenEncryptedFileRaw Win32 API functions.</summary>
   [SecurityCritical]
   internal sealed class SafeEncryptedFileRawHandle : SafeHandleZeroOrMinusOneIsInvalid
   {
      /// <summary>Constructor that prevents a default instance of this class from being created.</summary>
      private SafeEncryptedFileRawHandle() : base(true)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="SafeEncryptedFileRawHandle"/> class.</summary>
      /// <param name="handle">The handle.</param>
      /// <param name="callerHandle"><c>true</c> to reliably release the handle during the finalization phase; <c>false</c> to prevent reliable release (not recommended).</param>
      public SafeEncryptedFileRawHandle(IntPtr handle, bool callerHandle) : base(callerHandle)
      {
         SetHandle(handle);
      }

      
      /// <summary>When overridden in a derived class, executes the code required to free the handle.</summary>
      /// <returns>
      /// <c>true</c> if the handle is released successfully; otherwise, in the event of a catastrophic failure,
      /// <c>false</c>. In this case, it generates a ReleaseHandleFailed Managed Debugging Assistant.
      /// </returns>
      protected override bool ReleaseHandle()
      {
         NativeMethods.CloseEncryptedFileRaw(handle);

         return true;
      }
   }
}
