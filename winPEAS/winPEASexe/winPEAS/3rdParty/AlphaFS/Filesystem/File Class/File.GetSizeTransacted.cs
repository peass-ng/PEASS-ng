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
   public static partial class File
   {
      /// <summary>[AlphaFS] Retrieves the size of the specified file.</summary>
      /// <returns>The file size, in bytes.</returns>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file.</param>
      [SecurityCritical]
      public static long GetSizeTransacted(KernelTransaction transaction, string path)
      {
         return GetSizeCore(null, transaction, path, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves the size of the specified file.</summary>
      /// <returns>The file size, in bytes.</returns>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static long GetSizeTransacted(KernelTransaction transaction, string path, PathFormat pathFormat)
      {
         return GetSizeCore(null, transaction, path, false, pathFormat);
      }


      /// <summary>[AlphaFS] Retrieves the size of the specified file.</summary>
      /// <returns>The file size of the first or all streams, in bytes.</returns>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      [SecurityCritical]
      public static long GetSizeTransacted(KernelTransaction transaction, string path, bool sizeOfAllStreams)
      {
         return GetSizeCore(null, transaction, path, sizeOfAllStreams, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves the size of the specified file.</summary>
      /// <returns>The file size of the first or all streams, in bytes.</returns>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static long GetSizeTransacted(KernelTransaction transaction, string path, bool sizeOfAllStreams, PathFormat pathFormat)
      {
         return GetSizeCore(null, transaction, path, sizeOfAllStreams, pathFormat);
      }
   }
}
