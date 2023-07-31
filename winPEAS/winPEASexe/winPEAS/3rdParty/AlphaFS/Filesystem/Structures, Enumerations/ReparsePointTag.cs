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

using System.Diagnostics.CodeAnalysis;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Enumeration specifying the different reparse point tags.</summary>
   /// <remarks>
   ///   <para>Reparse tags, with the exception of IO_REPARSE_TAG_SYMLINK, are processed on the server and are not processed by a client after transmission over the wire.</para>
   ///   <para>Clients should treat associated reparse data as opaque data.</para>
   /// </remarks>
   public enum ReparsePointTag
   {
      /// <summary>The entry is not a reparse point.</summary>
      None = 0,

      /// <summary>IO_REPARSE_APPXSTREAM</summary>
      AppXStream = unchecked ((int) 3221225492),

      /// <summary>IO_REPARSE_TAG_CSV</summary>
      Csv = unchecked ((int) 2147483657),

      /// <summary>IO_REPARSE_TAG_DRIVER_EXTENDER
      /// <para>Used by Home server drive extender.</para>
      /// </summary>
      DriverExtender = unchecked ((int) 2147483653),

      /// <summary>IO_REPARSE_TAG_DEDUP</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dedup")]
      Dedup = unchecked ((int) 2147483667),

      /// <summary>IO_REPARSE_TAG_DFS
      /// <para>Used by the DFS filter.</para>
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dfs")]
      Dfs = unchecked ((int) 2147483658),

      /// <summary>IO_REPARSE_TAG_DFSR
      /// <para>Used by the DFS filter.</para>
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dfsr")]
      Dfsr = unchecked ((int) 2147483666),

      /// <summary>IO_REPARSE_TAG_FILTER_MANAGER
      /// <para>Used by filter manager test harness.</para>
      /// </summary>
      FilterManager = unchecked ((int) 2147483659),

      /// <summary>IO_REPARSE_TAG_HSM
      /// <para>(Obsolete) Used by legacy Hierarchical Storage Manager Product.</para>
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Hsm")]
      Hsm = unchecked ((int) 3221225476),

      /// <summary>IO_REPARSE_TAG_HSM2
      /// <para>(Obsolete) Used by legacy Hierarchical Storage Manager Product.</para>
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Hsm")]
      Hsm2 = unchecked ((int) 2147483654),

      /// <summary>IO_REPARSE_TAG_NFS
      /// <para>NFS symlinks, Windows 8 / SMB3 and later.</para>
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nfs")]
      Nfs = unchecked ((int) 2147483668),

      /// <summary>IO_REPARSE_TAG_MOUNT_POINT
      /// <para>Used for mount point support.</para>
      /// </summary>
      MountPoint = unchecked ((int) 2684354563),

      /// <summary>IO_REPARSE_TAG_SIS
      /// <para>Used by single-instance storage (SIS) filter driver.</para>
      /// </summary>
      Sis = unchecked ((int) 2147483655),

      /// <summary>IO_REPARSE_TAG_SYMLINK
      /// <para>Used for symbolic link support.</para>
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sym")]
      SymLink = unchecked ((int) 2684354572),

      /// <summary>IO_REPARSE_TAG_WIM</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wim")]
      Wim = unchecked ((int) 2147483656)
   }
}
