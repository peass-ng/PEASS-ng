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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Creates/appends a new file by using the specified encoding, writes a collection of strings to the file, and then closes the file.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="SecurityException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to write to.</param>
      /// <param name="contents">The lines to write to the file.</param>
      /// <param name="encoding">The character <see cref="Encoding"/> to use.</param>
      /// <param name="isAppend"><c>true</c> for file Append, <c>false</c> for file Write.</param>
      /// <param name="addNewLine"><c>true</c> to add a line terminator, <c>false</c> to ommit the line terminator.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Disposing is controlled.")]
      [SecurityCritical]
      internal static void WriteAppendAllLinesCore(KernelTransaction transaction, string path, IEnumerable<string> contents, Encoding encoding, bool isAppend, bool addNewLine, PathFormat pathFormat)
      {
         if (null == contents)
            throw new ArgumentNullException("contents");

         if (null == encoding)
            throw new ArgumentNullException("encoding");


         using (var stream = OpenCore(transaction, path, isAppend ? FileMode.OpenOrCreate : FileMode.Create, FileSystemRights.AppendData, FileShare.ReadWrite, ExtendedFileAttributes.Normal, null, null, pathFormat))
         {
            if (isAppend)
               stream.Seek(0, SeekOrigin.End);

            using (var writer = new StreamWriter(stream, encoding))
            {
               if (addNewLine)
                  foreach (var line in contents)
                     writer.WriteLine(line);

               else
                  foreach (var line in contents)
                     writer.Write(line);
            }
         }
      }
   }
}
