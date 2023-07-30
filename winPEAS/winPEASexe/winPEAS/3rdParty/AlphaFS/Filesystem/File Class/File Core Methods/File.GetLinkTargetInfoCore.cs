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
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Gets information about the target of a mount point or symbolic link on an NTFS file system.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnrecognizedReparsePointException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="reparsePath">The path to the reparse point.</param>
      /// <param name="continueOnException">
      ///    <para><c>true</c> suppress any Exception that might be thrown as a result from a failure,</para>
      ///    <para>such as ACLs protected directories or non-accessible reparse points.</para>
      /// </param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   An instance of <see cref="LinkTargetInfo"/> or <see cref="SymbolicLinkTargetInfo"/> containing information about the symbolic link
      ///   or mount point pointed to by <paramref name="reparsePath"/>.
      /// </returns>
      [SecurityCritical]
      internal static LinkTargetInfo GetLinkTargetInfoCore(KernelTransaction transaction, string reparsePath, bool continueOnException, PathFormat pathFormat)
      {
         // Codacy: The value of a static readonly field is computed at runtime while the value of a const field is calculated at compile time, which improves performance.

         const ExtendedFileAttributes eAttributes = ExtendedFileAttributes.OpenReparsePoint | ExtendedFileAttributes.BackupSemantics;


         using (var safeHandle = CreateFileCore(transaction, false, reparsePath, eAttributes, null, FileMode.Open, 0, FileShare.ReadWrite, pathFormat != PathFormat.LongFullPath, continueOnException, pathFormat))

            return null != safeHandle ? Device.GetLinkTargetInfo(safeHandle, reparsePath) : null;
      }
   }
}
