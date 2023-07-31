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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>Creates a new directory with the attributes of a specified template directory (if one is specified). 
      ///   If the underlying file system supports security on files and directories, the function applies the specified security descriptor to the new directory.
      ///   The new directory retains the other attributes of the specified template directory.
      /// </summary>
      /// <returns>
      ///   <para>Returns an object that represents the directory at the specified path.</para>
      ///   <para>This object is returned regardless of whether a directory at the specified path already exists.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="returnNull">When <c>true</c> returns <c>null</c> instead of a <see cref="DirectoryInfo"/> instance.</param>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The directory to create.</param>
      /// <param name="templatePath">The path of the directory to use as a template when creating the new directory. May be <c>null</c> to indicate that no template should be used.</param>
      /// <param name="directorySecurity">The <see cref="DirectorySecurity"/> access control to apply to the directory, may be null.</param>
      /// <param name="compress">When <c>true</c> compresses the directory using NTFS compression.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static DirectoryInfo CreateDirectoryCore(bool returnNull, KernelTransaction transaction, string path, string templatePath, ObjectSecurity directorySecurity, bool compress, PathFormat pathFormat)
      {
         var longPath = path;

         if (pathFormat != PathFormat.LongFullPath)
         {
            if (null == path)
               throw new ArgumentNullException("path");


            Path.CheckSupportedPathFormat(path, true, true);
            Path.CheckSupportedPathFormat(templatePath, true, true);

            longPath = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator);

            pathFormat = PathFormat.LongFullPath;
         }


         if (!char.IsWhiteSpace(longPath[longPath.Length - 1]))
         {
            // Return DirectoryInfo instance if the directory specified by path already exists.

            if (File.ExistsCore(transaction, true, longPath, PathFormat.LongFullPath))

               // We are not always interested in a new DirectoryInfo instance.
               return returnNull ? null : new DirectoryInfo(transaction, longPath, PathFormat.LongFullPath);
         }


         // MSDN: .NET 3.5+: IOException: The directory specified by path is a file or the network name was not found.
         if (File.ExistsCore(transaction, false, longPath, PathFormat.LongFullPath))
            NativeError.ThrowException(Win32Errors.ERROR_ALREADY_EXISTS, longPath);


         var templatePathLp = Utils.IsNullOrWhiteSpace(templatePath)
            ? null
            : Path.GetExtendedLengthPathCore(transaction, templatePath, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator);
         

         var list = ConstructFullPath(transaction, longPath);
         
         // Directory security.
         using (var securityAttributes = new Security.NativeMethods.SecurityAttributes(directorySecurity))
         {
            // Create the directory paths.
            while (list.Count > 0)
            {
               var folderLp = list.Pop();

               // CreateDirectory() / CreateDirectoryEx()
               // 2013-01-13: MSDN confirms LongPath usage.

               if (!(transaction == null || !NativeMethods.IsAtLeastWindowsVista

                  ? (templatePathLp == null

                     ? NativeMethods.CreateDirectory(folderLp, securityAttributes)

                     : NativeMethods.CreateDirectoryEx(templatePathLp, folderLp, securityAttributes))

                  : NativeMethods.CreateDirectoryTransacted(templatePathLp, folderLp, securityAttributes, transaction.SafeHandle)))
               {
                  var lastError = Marshal.GetLastWin32Error();

                  switch ((uint) lastError)
                  {
                     // MSDN: .NET 3.5+: If the directory already exists, this method does nothing.
                     // MSDN: .NET 3.5+: IOException: The directory specified by path is a file.
                     case Win32Errors.ERROR_ALREADY_EXISTS:
                        if (File.ExistsCore(transaction, false, longPath, PathFormat.LongFullPath))
                           NativeError.ThrowException(lastError, longPath);

                        if (File.ExistsCore(transaction, false, folderLp, PathFormat.LongFullPath))
                           NativeError.ThrowException(Win32Errors.ERROR_PATH_NOT_FOUND, null, folderLp);
                        break;


                     case Win32Errors.ERROR_BAD_NET_NAME:
                        NativeError.ThrowException(Win32Errors.ERROR_BAD_NET_NAME, longPath);
                        break;


                     case Win32Errors.ERROR_DIRECTORY:
                        // MSDN: .NET 3.5+: NotSupportedException: path contains a colon character (:) that is not part of a drive label ("C:\").
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.Unsupported_Path_Format, longPath));


                     case Win32Errors.ERROR_ACCESS_DENIED:
                        // Report the parent folder, the inaccessible folder.
                        var parent = GetParent(folderLp);

                        NativeError.ThrowException(lastError, null != parent ? parent.FullName : folderLp);
                        break;


                     default:
                        NativeError.ThrowException(lastError, true, folderLp);
                        break;
                  }
               }

               else if (compress)
                  Device.ToggleCompressionCore(transaction, true, folderLp, true, PathFormat.LongFullPath);
            }


            // We are not always interested in a new DirectoryInfo instance.

            return returnNull ? null : new DirectoryInfo(transaction, longPath, PathFormat.LongFullPath);
         }
      }


      private static Stack<string> ConstructFullPath(KernelTransaction transaction, string path)
      {
         var longPathPrefix = Path.IsUncPathCore(path, false, false) ? Path.LongPathUncPrefix : Path.LongPathPrefix;
         path = Path.GetRegularPathCore(path, GetFullPathOptions.None, false);

         var length = path.Length;
         if (length >= 2 && Path.IsDVsc(path[length - 1], false))
            --length;

         var rootLength = Path.GetRootLength(path, false);
         if (length == 2 && Path.IsDVsc(path[1], false))
            throw new ArgumentException(Resources.Cannot_Create_Directory, "path");


         // Check if directories are missing.
         var list = new Stack<string>(100);

         if (length > rootLength)
         {
            for (var index = length - 1; index >= rootLength; --index)
            {
               var path1 = path.Substring(0, index + 1);
               var path2 = longPathPrefix + path1.TrimStart('\\');

               if (!File.ExistsCore(transaction, true, path2, PathFormat.LongFullPath))
                  list.Push(path2);

               while (index > rootLength && !Path.IsDVsc(path[index], false))
                  --index;
            }
         }

         return list;
      }
   }
}
