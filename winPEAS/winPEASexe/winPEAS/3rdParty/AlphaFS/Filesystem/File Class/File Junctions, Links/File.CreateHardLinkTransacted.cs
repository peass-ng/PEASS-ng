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
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      #region Obsolete

      /// <summary>[AlphaFS] Establishes a hard link (similar to CMD command: "MKLINK /H") between an existing file and a new file as a transacted operation. This function is only supported on the NTFS file system, and only for files, not directories.</summary>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileName">The name of the new file. This parameter cannot specify the name of a directory.</param>
      /// <param name="existingFileName">The name of the existing file. This parameter cannot specify the name of a directory.</param>      
      [Obsolete("Use CreateHardLinkTransacted method.")]
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Hardlink")]
      [SecurityCritical]
      public static void CreateHardlinkTransacted(KernelTransaction transaction, string fileName, string existingFileName)
      {
         CreateHardLinkCore(transaction, fileName, existingFileName, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Establishes a hard link (similar to CMD command: "MKLINK /H") between an existing file and a new file as a transacted operation. This function is only supported on the NTFS file system, and only for files, not directories.</summary>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileName">The name of the new file. This parameter cannot specify the name of a directory.</param>
      /// <param name="existingFileName">The name of the existing file. This parameter cannot specify the name of a directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [Obsolete("Use CreateHardLinkTransacted method.")]
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Hardlink")]
      [SecurityCritical]
      public static void CreateHardlinkTransacted(KernelTransaction transaction, string fileName, string existingFileName, PathFormat pathFormat)
      {
         CreateHardLinkCore(transaction, fileName, existingFileName, pathFormat);
      }

      #endregion // Obsolete


      /// <summary>[AlphaFS] Establishes a hard link (similar to CMD command: "MKLINK /H") between an existing file and a new file as a transacted operation. This function is only supported on the NTFS file system, and only for files, not directories.</summary>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileName">The name of the new file. This parameter cannot specify the name of a directory.</param>
      /// <param name="existingFileName">The name of the existing file. This parameter cannot specify the name of a directory.</param>      
      [SecurityCritical]
#pragma warning disable CS3005 // Identifier differing only in case is not CLS-compliant
      public static void CreateHardLinkTransacted(KernelTransaction transaction, string fileName, string existingFileName)
#pragma warning restore CS3005 // Identifier differing only in case is not CLS-compliant
      {
         CreateHardLinkCore(transaction, fileName, existingFileName, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Establishes a hard link (similar to CMD command: "MKLINK /H") between an existing file and a new file as a transacted operation. This function is only supported on the NTFS file system, and only for files, not directories.</summary>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileName">The name of the new file. This parameter cannot specify the name of a directory.</param>
      /// <param name="existingFileName">The name of the existing file. This parameter cannot specify the name of a directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [SecurityCritical]
#pragma warning disable CS3005 // Identifier differing only in case is not CLS-compliant
      public static void CreateHardLinkTransacted(KernelTransaction transaction, string fileName, string existingFileName, PathFormat pathFormat)
#pragma warning restore CS3005 // Identifier differing only in case is not CLS-compliant
      {
         CreateHardLinkCore(transaction, fileName, existingFileName, pathFormat);
      }
   }
}
