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

using System.IO;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Checks that the <see cref="FileAttributes"/> instance is valid.</summary>
      internal static bool HasValidAttributes(FileAttributes fileAttributes)
      {
         return Utils.IsNotNull(fileAttributes) && !fileAttributes.Equals(NativeMethods.InvalidFileAttributes);
      }


      /// <summary>Checks that the file system object is a directory.</summary>
      internal static bool IsDirectory(FileAttributes fileAttributes)
      {
         return HasValidAttributes(fileAttributes) && (fileAttributes & FileAttributes.Directory) != 0;
      }


      /// <summary>Checks that the file system object is hidden.</summary>
      internal static bool IsHidden(FileAttributes fileAttributes)
      {
         return HasValidAttributes(fileAttributes) && (fileAttributes & FileAttributes.Hidden) != 0;
      }
      

      /// <summary>Checks that the file system object is read-only.</summary>
      internal static bool IsReadOnly(FileAttributes fileAttributes)
      {
         return HasValidAttributes(fileAttributes) && (fileAttributes & FileAttributes.ReadOnly) != 0;
      }


      /// <summary>Checks that the file system object is read-only or hidden.</summary>
      internal static bool IsReadOnlyOrHidden(FileAttributes fileAttributes)
      {
         return IsReadOnly(fileAttributes) || IsHidden(fileAttributes);
      }
   }
}
