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

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>[AlphaFS] Retrieves the final path for the specified file, formatted as <see cref="FinalPathFormats"/>.</summary>
      /// <returns>The final path as a string.</returns>
      /// <remarks>
      ///   A final path is the path that is returned when a path is fully resolved. For example, for a symbolic link named "C:\tmp\mydir" that
      ///   points to "D:\yourdir", the final path would be "D:\yourdir".
      /// </remarks>
      /// <param name="handle">Then handle to a <see cref="SafeFileHandle"/> instance.</param>
      [SecurityCritical]
      public static string GetFinalPathNameByHandle(SafeFileHandle handle)
      {
         return GetFinalPathNameByHandleCore(handle, FinalPathFormats.None);
      }


      /// <summary>[AlphaFS] Retrieves the final path for the specified file, formatted as <see cref="FinalPathFormats"/>.</summary>
      /// <returns>The final path as a string.</returns>
      /// <remarks>
      ///   A final path is the path that is returned when a path is fully resolved. For example, for a symbolic link named "C:\tmp\mydir" that
      ///   points to "D:\yourdir", the final path would be "D:\yourdir".
      /// </remarks>
      /// <param name="handle">Then handle to a <see cref="SafeFileHandle"/> instance.</param>
      /// <param name="finalPath">The final path, formatted as <see cref="FinalPathFormats"/></param>
      [SecurityCritical]
      public static string GetFinalPathNameByHandle(SafeFileHandle handle, FinalPathFormats finalPath)
      {
         return GetFinalPathNameByHandleCore(handle, finalPath);
      }
   }
}
