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
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Exposes instance methods for creating, moving, and enumerating through directories and subdirectories. This class cannot be inherited.</summary>
   [Serializable]
   public sealed partial class DirectoryInfo : FileSystemInfo
   {
      #region Constructors

      #region .NET

      /// <summary>Initializes a new instance of the <see cref="DirectoryInfo"/> class on the specified path.</summary>
      /// <param name="path">The path on which to create the <see cref="DirectoryInfo"/>.</param>
      /// <remarks>
      /// This constructor does not check if a directory exists. This constructor is a placeholder for a string that is used to access the disk in subsequent operations.
      /// The path parameter can be a file name, including a file on a Universal Naming Convention (UNC) share.
      /// </remarks>
      public DirectoryInfo(string path) : this(null, path, PathFormat.RelativePath)
      {
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DirectoryInfo"/> class on the specified path.</summary>
      /// <param name="path">The path on which to create the <see cref="DirectoryInfo"/>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <remarks>This constructor does not check if a directory exists. This constructor is a placeholder for a string that is used to access the disk in subsequent operations.</remarks>
      public DirectoryInfo(string path, PathFormat pathFormat) : this(null, path, pathFormat)
      {
      }

      /// <summary>[AlphaFS] Special internal implementation.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fullPath">The full path on which to create the <see cref="DirectoryInfo"/>.</param>
      /// <param name="junk1">Not used.</param>
      /// <param name="junk2">Not used.</param>
      /// <remarks>This constructor does not check if a directory exists. This constructor is a placeholder for a string that is used to access the disk in subsequent operations.</remarks>
      [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "junk1")]
      [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "junk2")]
      private DirectoryInfo(KernelTransaction transaction, string fullPath, bool junk1, bool junk2)
      {
         IsDirectory = true;
         Transaction = transaction;

         LongFullName = Path.GetLongPathCore(fullPath, GetFullPathOptions.None);

         OriginalPath = Path.GetFileName(fullPath, true);

         FullPath = fullPath;

         DisplayPath = OriginalPath.Length != 2 || OriginalPath[1] != Path.VolumeSeparatorChar ? OriginalPath : Path.CurrentDirectoryPrefix;
      }


      #region Transactional

      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DirectoryInfo"/> class on the specified path.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path on which to create the <see cref="DirectoryInfo"/>.</param>
      /// <remarks>This constructor does not check if a directory exists. This constructor is a placeholder for a string that is used to access the disk in subsequent operations.</remarks>
      public DirectoryInfo(KernelTransaction transaction, string path) : this(transaction, path, PathFormat.RelativePath)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DirectoryInfo"/> class on the specified path.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path on which to create the <see cref="DirectoryInfo"/>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <remarks>This constructor does not check if a directory exists. This constructor is a placeholder for a string that is used to access the disk in subsequent operations.</remarks>
      public DirectoryInfo(KernelTransaction transaction, string path, PathFormat pathFormat)
      {
         InitializeCore(transaction, true, path, pathFormat);
      }

      #endregion // Transactional

      #endregion // Constructors


      #region Properties

      #region .NET

      /// <summary>Gets a value indicating whether the directory exists.</summary>
      /// <remarks>
      ///   <para>The <see cref="Exists"/> property returns <c>false</c> if any error occurs while trying to determine if the
      ///   specified directory exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a directory name with invalid characters or too many
      ///   characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the directory.</para>
      /// </remarks>
      /// <value><c>true</c> if the directory exists; otherwise, <c>false</c>.</value>
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      public override bool Exists
      {
         [SecurityCritical]
         get
         {
            try
            {
               if (DataInitialised == -1)
                  Refresh();

               return DataInitialised == 0 && IsDirectory;
            }
            catch
            {
               return false;
            }
         }
      }


      /// <summary>Gets the name of this <see cref="DirectoryInfo"/> instance.</summary>
      /// <value>The directory name.</value>
      /// <remarks>
      ///   <para>This Name property returns only the name of the directory, such as "Bin".</para>
      ///   <para>To get the full path, such as "c:\public\Bin", use the FullName property.</para>
      /// </remarks>
      public override string Name
      {
         get { return FullPath.Length > 3 ? Path.GetFileName(Path.RemoveTrailingDirectorySeparator(FullPath), true) : FullPath; }
      }


      /// <summary>Gets the parent directory of a specified subdirectory.</summary>
      /// <value>The parent directory, or null if the path is null or if the file path denotes a root (such as "\", "C:", or * "\\server\share").</value>
      public DirectoryInfo Parent
      {
         [SecurityCritical]
         get
         {
            var path = FullPath;

            if (path.Length > 3)
               path = Path.RemoveTrailingDirectorySeparator(FullPath);

            var dirName = Path.GetDirectoryName(path, false);

            return null != dirName ? new DirectoryInfo(Transaction, dirName, true, true) : null;
         }
      }


      /// <summary>Gets the root portion of the directory.</summary>
      /// <value>An object that represents the root of the directory.</value>
      public DirectoryInfo Root
      {
         [SecurityCritical]
         get { return new DirectoryInfo(Transaction, Path.GetPathRoot(FullPath, false), PathFormat.RelativePath); }
      }

      #endregion // .NET

      #endregion // Properties


      #region Methods

      /// <summary>Returns the original path that was passed by the user.</summary>
      /// <returns>A string that represents this object.</returns>
      public override string ToString()
      {
         return DisplayPath;
      }

      #endregion // Methods
   }
}
