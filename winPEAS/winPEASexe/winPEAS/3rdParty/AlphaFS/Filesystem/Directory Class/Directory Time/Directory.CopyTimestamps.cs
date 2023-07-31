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
   public static partial class Directory
   {
      #region Obsolete

      /// <summary>[AlphaFS] Copies the date and timestamps for the specified directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      [Obsolete("Use new method name: CopyTimestamp")]
      [SecurityCritical]
      public static void TransferTimestamps(string sourcePath, string destinationPath)
      {
         CopyTimestamps(sourcePath, destinationPath);
      }

      /// <summary>[AlphaFS] Copies the date and timestamps for the specified directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("Use new method name: CopyTimestamp")]
      [SecurityCritical]
      public static void TransferTimestamps(string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         CopyTimestamps(sourcePath, destinationPath, pathFormat);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <param name="transaction">The transaction.</param>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      [Obsolete("Use new method name: CopyTimestampsTransacted")]
      [SecurityCritical]
      public static void TransferTimestampsTransacted(KernelTransaction transaction, string sourcePath, string destinationPath)
      {
         CopyTimestampsTransacted(transaction, sourcePath, destinationPath, PathFormat.RelativePath);
      }

      /// <summary>[AlphaFS] Copies the date and timestamps for the specified directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <param name="transaction">The transaction.</param>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("Use new method name: CopyTimestampsTransacted")]
      [SecurityCritical]
      public static void TransferTimestampsTransacted(KernelTransaction transaction, string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         CopyTimestampsTransacted(transaction, sourcePath, destinationPath, pathFormat);
      }

      #endregion // Obsolete




      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      [SecurityCritical]
      public static void CopyTimestamps(string sourcePath, string destinationPath)
      {
         File.CopyTimestampsCore(null, true, sourcePath, destinationPath, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CopyTimestamps(string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         File.CopyTimestampsCore(null, true, sourcePath, destinationPath, false, pathFormat);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the directory linked to. No effect if <paramref name="destinationPath"/> does not refer to a reparse point.</param>
      [SecurityCritical]
      public static void CopyTimestamps(string sourcePath, string destinationPath, bool modifyReparsePoint)
      {
         File.CopyTimestampsCore(null, true, sourcePath, destinationPath, modifyReparsePoint, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing directories.</summary>
      /// <remarks>This method uses BackupSemantics flag to get Timestamp changed for directories.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="sourcePath">The source directory to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination directory to set the date and time stamps.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the directory linked to. No effect if <paramref name="destinationPath"/> does not refer to a reparse point.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CopyTimestamps(string sourcePath, string destinationPath, bool modifyReparsePoint, PathFormat pathFormat)
      {
         File.CopyTimestampsCore(null, true, sourcePath, destinationPath, modifyReparsePoint, pathFormat);
      }
   }
}
