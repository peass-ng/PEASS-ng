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
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Retrieves the size of all alternate data streams of the specified directory and it files.</summary>
      /// <returns>The size of all alternate data streams of the specified directory and its files.</returns>
      /// <param name="path">The path to the directory.</param>
      [SecurityCritical]
      public static long GetSize(string path)
      {
         return GetSizeCore(null, path, false, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves the size of all alternate data streams of the specified directory and it files.</summary>
      /// <returns>The size of all alternate data streams of the specified directory and its files.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static long GetSize(string path, PathFormat pathFormat)
      {
         return GetSizeCore(null, path, false, false, pathFormat);
      }


      /// <summary>[AlphaFS] Retrieves the size of all alternate data streams of the specified directory and it files.</summary>
      /// <returns>The size of all alternate data streams of the specified directory and its files.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      [SecurityCritical]
      public static long GetSize(string path, bool sizeOfAllStreams)
      {
         return GetSizeCore(null, path, sizeOfAllStreams, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves the size of all alternate data streams of the specified directory and it files.</summary>
      /// <returns>The size of all alternate data streams of the specified directory and its files.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static long GetSize(string path, bool sizeOfAllStreams, PathFormat pathFormat)
      {
         return GetSizeCore(null, path, sizeOfAllStreams, false, pathFormat);
      }


      /// <summary>[AlphaFS] Retrieves the size of all alternate data streams of the specified directory and it files.</summary>
      /// <returns>The size of all alternate data streams of the specified directory and its files.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      /// <param name="recursive"><c>true</c> to include subdirectories.</param>
      [SecurityCritical]
      public static long GetSize(string path, bool sizeOfAllStreams, bool recursive)
      {
         return GetSizeCore(null, path, sizeOfAllStreams, recursive, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves the size of all alternate data streams of the specified directory and it files.</summary>
      /// <returns>The size of all alternate data streams of the specified directory and its files.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      /// <param name="recursive"><c>true</c> to include subdirectories.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static long GetSize(string path, bool sizeOfAllStreams, bool recursive, PathFormat pathFormat)
      {
         return GetSizeCore(null, path, sizeOfAllStreams, recursive, pathFormat);
      }
   }
}
