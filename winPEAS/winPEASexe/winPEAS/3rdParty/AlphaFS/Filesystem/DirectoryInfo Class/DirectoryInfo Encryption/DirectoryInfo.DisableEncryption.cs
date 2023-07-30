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

namespace Alphaleonis.Win32.Filesystem
{
   public sealed partial class DirectoryInfo
   {
      /// <summary>[AlphaFS] Disables encryption of the specified directory and the files in it. It does not affect encryption of subdirectories below the indicated directory.</summary>
      /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
      /// <remarks>This method will create/change the file "Desktop.ini" and wil set Encryption value: "Disable=0"</remarks>
      [SecurityCritical]
      public void DisableEncryption()
      {
         Directory.EnableDisableEncryptionCore(LongFullName, false, PathFormat.LongFullPath);
      }
   }
}
