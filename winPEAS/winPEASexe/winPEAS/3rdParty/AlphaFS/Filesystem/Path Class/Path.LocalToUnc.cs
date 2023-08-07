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
using System.Net.NetworkInformation;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      #region Obsolete

      /// <summary>[AlphaFS] Converts a local path to a network share path, optionally returning it as a long path format and the ability to add or remove a trailing backslash.
      ///   <para>A Local path, e.g.: "C:\Windows" or "C:\Windows\" will be returned as: "\\localhost\C$\Windows".</para>
      ///   <para>If a logical drive points to a network share path (mapped drive), the share path will be returned without a trailing <see cref="DirectorySeparator"/> character.</para>
      /// </summary>
      /// <returns>On successful conversion a UNC path is returned.
      ///   <para>If the conversion fails, <paramref name="localPath"/> is returned.</para>
      ///   <para>If <paramref name="localPath"/> is an empty string or <c>null</c>, <c>null</c> is returned.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="PathTooLongException"/>
      /// <exception cref="NetworkInformationException"/>
      /// <param name="localPath">A local path, e.g.: "C:\Windows".</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <param name="fullPathOptions">Options for controlling the full path retrieval.</param>
      //[Obsolete]
      //[SecurityCritical]
      //public static string LocalToUnc(string localPath, PathFormat pathFormat, GetFullPathOptions fullPathOptions)
      //{
      //   return LocalToUncCore(localPath, fullPathOptions, pathFormat);
      //}

      #endregion // Obsolete


      /// <summary>[AlphaFS] Converts a local path to a network share path.  
      ///   <para>A Local path, e.g.: "C:\Windows" or "C:\Windows\" will be returned as: "\\localhost\C$\Windows".</para>
      ///   <para>If a logical drive points to a network share path (mapped drive), the share path will be returned without a trailing <see cref="DirectorySeparator"/> character.</para>
      /// </summary>
      /// <returns>On successful conversion a UNC path is returned.
      ///   <para>If the conversion fails, <paramref name="localPath"/> is returned.</para>
      ///   <para>If <paramref name="localPath"/> is an empty string or <c>null</c>, <c>null</c> is returned.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="PathTooLongException"/>
      /// <exception cref="NetworkInformationException"/>
      /// <param name="localPath">A local path, e.g.: "C:\Windows".</param>
      //[SecurityCritical]
      //public static string LocalToUnc(string localPath)
      //{
      //   return LocalToUncCore(localPath, GetFullPathOptions.None, PathFormat.RelativePath);
      //}


      /// <summary>[AlphaFS] Converts a local path to a network share path.  
      ///   <para>A Local path, e.g.: "C:\Windows" or "C:\Windows\" will be returned as: "\\localhost\C$\Windows".</para>
      ///   <para>If a logical drive points to a network share path (mapped drive), the share path will be returned without a trailing <see cref="DirectorySeparator"/> character.</para>
      /// </summary>
      /// <returns>On successful conversion a UNC path is returned.
      ///   <para>If the conversion fails, <paramref name="localPath"/> is returned.</para>
      ///   <para>If <paramref name="localPath"/> is an empty string or <c>null</c>, <c>null</c> is returned.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="PathTooLongException"/>
      /// <exception cref="NetworkInformationException"/>
      /// <param name="localPath">A local path, e.g.: "C:\Windows".</param>
      ///// <param name="pathFormat">Indicates the format of the path parameter.</param>
      //[SecurityCritical]
      //public static string LocalToUnc(string localPath, PathFormat pathFormat)
      //{
      //   return LocalToUncCore(localPath, GetFullPathOptions.None, pathFormat);
      //}
      

      /// <summary>[AlphaFS] Converts a local path to a network share path, optionally returning it as a long path format and the ability to add or remove a trailing backslash.
      ///   <para>A Local path, e.g.: "C:\Windows" or "C:\Windows\" will be returned as: "\\localhost\C$\Windows".</para>
      ///   <para>If a logical drive points to a network share path (mapped drive), the share path will be returned without a trailing <see cref="DirectorySeparator"/> character.</para>
      /// </summary>
      /// <returns>On successful conversion a UNC path is returned.
      ///   <para>If the conversion fails, <paramref name="localPath"/> is returned.</para>
      ///   <para>If <paramref name="localPath"/> is an empty string or <c>null</c>, <c>null</c> is returned.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="PathTooLongException"/>
      /// <exception cref="NetworkInformationException"/>
      /// <param name="localPath">A local path, e.g.: "C:\Windows".</param>
      /// <param name="fullPathOptions">Options for controlling the full path retrieval.</param>
      //[SecurityCritical]
      //public static string LocalToUnc(string localPath, GetFullPathOptions fullPathOptions)
      //{
      //   return LocalToUncCore(localPath, fullPathOptions, PathFormat.RelativePath);
      //}


      /// <summary>[AlphaFS] Converts a local path to a network share path, optionally returning it as a long path format and the ability to add or remove a trailing backslash.
      ///   <para>A Local path, e.g.: "C:\Windows" or "C:\Windows\" will be returned as: "\\localhost\C$\Windows".</para>
      ///   <para>If a logical drive points to a network share path (mapped drive), the share path will be returned without a trailing <see cref="DirectorySeparator"/> character.</para>
      /// </summary>
      /// <returns>On successful conversion a UNC path is returned.
      ///   <para>If the conversion fails, <paramref name="localPath"/> is returned.</para>
      ///   <para>If <paramref name="localPath"/> is an empty string or <c>null</c>, <c>null</c> is returned.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="PathTooLongException"/>
      /// <exception cref="NetworkInformationException"/>
      /// <param name="localPath">A local path, e.g.: "C:\Windows".</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <param name="fullPathOptions">Options for controlling the full path retrieval.</param>
      //[SecurityCritical]
      //public static string LocalToUnc(string localPath, GetFullPathOptions fullPathOptions, PathFormat pathFormat)
      //{
      //   return LocalToUncCore(localPath, fullPathOptions, pathFormat);
      //}
   }
}
