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
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      #region .NET

      /// <summary>Returns the absolute path for the specified path string.</summary>
      /// <returns>The fully qualified location of path, such as "C:\MyFile.txt".</returns>
      /// <remarks>
      /// <para>GetFullPathName merges the name of the current drive and directory with a specified file name to determine the full path and file name of a specified file.</para>
      /// <para>It also calculates the address of the file name portion of the full path and file name.</para>
      /// <para>&#160;</para>
      /// <para>This method does not verify that the resulting path and file name are valid, or that they see an existing file on the associated volume.</para>
      /// <para>The .NET Framework does not support direct access to physical disks through paths that are device names, such as <c>\\.\PhysicalDrive0</c>.</para>
      /// <para>&#160;</para>
      /// <para>MSDN: Multithreaded applications and shared library code should not use the GetFullPathName function and</para>
      /// <para>should avoid using relative path names. The current directory state written by the SetCurrentDirectory function is stored as a global variable in each process,</para>
      /// <para>therefore multithreaded applications cannot reliably use this value without possible data corruption from other threads that may also be reading or setting this value.</para>
      /// <para>This limitation also applies to the SetCurrentDirectory and GetCurrentDirectory functions. The exception being when the application is guaranteed to be running in a single thread,</para>
      /// <para>for example parsing file names from the command line argument string in the main thread prior to creating any additional threads.</para>
      /// <para>Using relative path names in multithreaded applications or shared library code can yield unpredictable results and is not supported.</para>
      /// </remarks>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">The file or directory for which to obtain absolute path information.</param>
      [SecurityCritical]
      public static string GetFullPath(string path)
      {
         return GetFullPathCore(null, true, path, GetFullPathOptions.None);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Returns the absolute path for the specified path string.</summary>
      /// <returns>The fully qualified location of path, such as "C:\MyFile.txt".</returns>
      /// <remarks>
      /// <para>GetFullPathName merges the name of the current drive and directory with a specified file name to determine the full path and file name of a specified file.</para>
      /// <para>It also calculates the address of the file name portion of the full path and file name.</para>
      /// <para>&#160;</para>
      /// <para>This method does not verify that the resulting path and file name are valid, or that they see an existing file on the associated volume.</para>
      /// <para>The .NET Framework does not support direct access to physical disks through paths that are device names, such as <c>\\.\PhysicalDrive0</c>.</para>
      /// <para>&#160;</para>
      /// <para>MSDN: Multithreaded applications and shared library code should not use the GetFullPathName function and</para>
      /// <para>should avoid using relative path names. The current directory state written by the SetCurrentDirectory function is stored as a global variable in each process,</para>
      /// <para>therefore multithreaded applications cannot reliably use this value without possible data corruption from other threads that may also be reading or setting this value.</para>
      /// <para>This limitation also applies to the SetCurrentDirectory and GetCurrentDirectory functions. The exception being when the application is guaranteed to be running in a single thread,</para>
      /// <para>for example parsing file names from the command line argument string in the main thread prior to creating any additional threads.</para>
      /// <para>Using relative path names in multithreaded applications or shared library code can yield unpredictable results and is not supported.</para>
      /// </remarks>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">The file or directory for which to obtain absolute path information.</param>
      /// <param name="options">Options for controlling the full path retrieval.</param>
      [SecurityCritical]
      public static string GetFullPath(string path, GetFullPathOptions options)
      {
         return GetFullPathCore(null, true, path, options);
      }
   }
}
