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

using System.Linq;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      #region .NET

      /// <summary>Retrieves the names of the logical drives on the Computer in the form "&lt;drive letter&gt;:\".</summary>
      /// <returns>An array of type <see cref="string"/> that represents the logical drives on the Computer.</returns>
      [SecurityCritical]
      public static string[] GetLogicalDrives()
      {
         return EnumerateLogicalDrivesCore(false, false).Select(drive => drive.Name).ToArray();
      }

      #endregion // .NET
      

      /// <summary>[AlphaFS] Retrieves the names of the logical drives on the Computer in the form "C:\".</summary>
      /// <returns>An array of type <see cref="string"/> that represents the logical drives on the Computer.</returns>
      /// <param name="fromEnvironment">Retrieve logical drives as known by the Environment.</param>
      /// <param name="isReady">Retrieve only when accessible (IsReady) logical drives.</param>
      [SecurityCritical]
      public static string[] GetLogicalDrives(bool fromEnvironment, bool isReady)
      {
         return EnumerateLogicalDrivesCore(fromEnvironment, isReady).Select(drive => drive.Name).ToArray();
      }
   }
}
