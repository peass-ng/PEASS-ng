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

using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Represents a wrapper class for a handle used by the CM_Connect_Machine/CM_Disconnect_Machine Win32 API functions.</summary>
   [SecurityCritical]
   internal sealed class SafeCmConnectMachineHandle : SafeHandleZeroOrMinusOneIsInvalid
   {
      /// <summary>Initializes a new instance of the <see cref="SafeCmConnectMachineHandle"/> class.</summary>
      public SafeCmConnectMachineHandle() : base(true)
      {
      }


      /// <summary>When overridden in a derived class, executes the code required to free the handle.</summary>
      protected override bool ReleaseHandle()
      {
         return NativeMethods.CM_Disconnect_Machine(handle) == Win32Errors.NO_ERROR;
      }
   }
}
