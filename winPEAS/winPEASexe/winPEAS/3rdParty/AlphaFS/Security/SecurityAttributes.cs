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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Security
{
   internal static partial class NativeMethods
   {
      /// <summary>Class used to represent the SECURITY_ATTRIBUTES native Win32 structure.
      /// The SECURITY_ATTRIBUTES structure contains the security descriptor for an object and specifies whether the handle retrieved by specifying this structure is inheritable.
      /// This structure provides security settings for objects created by various functions, such as CreateFile, CreatePipe, CreateProcess, RegCreateKeyEx, or RegSaveKeyEx.
      /// </summary>
      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      internal sealed class SecurityAttributes : IDisposable
      {
         // Removing the StructLayout attribute results in errors.


         [MarshalAs(UnmanagedType.U4)]
         private int _length;

         private readonly SafeGlobalMemoryBufferHandle _securityDescriptor;


         public SecurityAttributes(ObjectSecurity securityDescriptor)
         {
            var safeBuffer = ToUnmanagedSecurityAttributes(securityDescriptor);

            _length = safeBuffer.Capacity;
            _securityDescriptor = safeBuffer;
         }


         public SecurityAttributes(ObjectSecurity securityDescriptor, bool inheritHandle) : this(securityDescriptor)
         {
            InheritHandle = inheritHandle;
         }


         public bool InheritHandle { get; set; }


         /// <summary>Marshals an ObjectSecurity instance to unmanaged memory.</summary>
         /// <returns>A safe handle containing the marshalled security descriptor.</returns>
         /// <param name="securityDescriptor">The security descriptor.</param>
         [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
         private static SafeGlobalMemoryBufferHandle ToUnmanagedSecurityAttributes(ObjectSecurity securityDescriptor)
         {
            if (null == securityDescriptor)
               return new SafeGlobalMemoryBufferHandle();


            var src = securityDescriptor.GetSecurityDescriptorBinaryForm();
            var safeBuffer = new SafeGlobalMemoryBufferHandle(src.Length);

            try
            {
               safeBuffer.CopyFrom(src, 0, src.Length);
               return safeBuffer;
            }
            catch
            {
               safeBuffer.Close();
               throw;
            }
         }


         public void Dispose()
         {
            if (null != _securityDescriptor && !_securityDescriptor.IsClosed)
               _securityDescriptor.Close();
         }
      }
   }
}
