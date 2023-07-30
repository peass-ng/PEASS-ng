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
      /// <summary>Decrypts/encrypts a directory recursively so that only the account used to encrypt the directory can decrypt it.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryReadOnlyException"/>
      /// <exception cref="FileReadOnlyException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">A path that describes a directory to encrypt.</param>
      /// <param name="encrypt"><c>true</c> encrypt, <c>false</c> decrypt.</param>
      /// <param name="recursive"><c>true</c> to decrypt the directory recursively. <c>false</c> only decrypt files and directories in the root of <paramref name="path"/>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void EncryptDecryptDirectoryCore(string path, bool encrypt, bool recursive, PathFormat pathFormat)
      {
         if (pathFormat != PathFormat.LongFullPath)
         {
            path = Path.GetExtendedLengthPathCore(null, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

            pathFormat = PathFormat.LongFullPath;
         }


         // Process folders and files when recursive. 

         if (recursive)
         {
            foreach (var fsei in EnumerateFileSystemEntryInfosCore<string>(null, null, path, Path.WildcardStarMatchAll, SearchOption.AllDirectories, DirectoryEnumerationOptions.AsLongPath, null, pathFormat))

               File.EncryptDecryptFileCore(true, fsei, encrypt, pathFormat);
         }

         // Process the root folder, the given path.

         File.EncryptDecryptFileCore(true, path, encrypt, pathFormat);
      }
   }
}
