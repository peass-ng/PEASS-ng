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

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] Directory enumeration options, flags that specify how a directory is to be enumerated.</summary>
   [Flags]
   public enum DirectoryEnumerationOptions
   {
      /// <summary>None (do not use).</summary>
      None = 0,

      /// <summary>Enumerate files only.</summary>
      Files = 1,

      /// <summary>Enumerate directories only.</summary>
      Folders = 2,

      /// <summary>Enumerate files and directories.</summary>
      FilesAndFolders = Files | Folders,

      /// <summary>Return full path as long full path (Unicode format), only valid when return type is <see cref="string"/>.</summary>
      AsLongPath = 4,

      /// <summary>Skip reparse points during directory enumeration.</summary>
      SkipReparsePoints = 8,

      /// <summary>Suppress any Exception that might be thrown as a result from a failure, such as ACLs protected directories or non-accessible reparse points.</summary>
      ContinueOnException = 16,

      /// <summary>Specifies whether to search the current directory, or the current directory and all subdirectories.</summary>
      Recursive = 32,

      /// <summary>Enumerates the directory without querying the short file name, improving overall enumeration speed.</summary>
      /// <remarks>This option is enabled by default if supported. This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
      BasicSearch = 64,

      /// <summary>Enumerates the directory using a larger buffer for directory queries, which can increase performance of the find operation.</summary>
      /// <remarks>This option is enabled by default if supported. This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
      LargeCache = 128
   }
}
