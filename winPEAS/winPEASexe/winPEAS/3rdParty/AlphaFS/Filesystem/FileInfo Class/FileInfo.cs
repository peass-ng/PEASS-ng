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
using System.Globalization;
using System.IO;
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Provides properties and instance methods for the creation, copying, deletion, moving, and opening of files, and aids in the creation of <see cref="FileStream"/> objects. This class cannot be inherited.</summary>
   [Serializable]
   public sealed partial class FileInfo : FileSystemInfo
   {
      #region Fields

      [NonSerialized]
      private string _name;

      #endregion // Fields


      #region Constructors

      #region .NET

      /// <summary>Initializes a new instance of the <see cref="Alphaleonis.Win32.Filesystem.FileInfo"/> class, which acts as a wrapper for a file path.</summary>
      /// <param name="fileName">The fully qualified name of the new file, or the relative file name. Do not end the path with the directory separator character.</param>
      /// <remarks>This constructor does not check if a file exists. This constructor is a placeholder for a string that is used to access the file in subsequent operations.</remarks>
      public FileInfo(string fileName) : this(null, fileName, PathFormat.RelativePath)
      {
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="Alphaleonis.Win32.Filesystem.FileInfo"/> class, which acts as a wrapper for a file path.</summary>
      /// <param name="fileName">The fully qualified name of the new file, or the relative file name. Do not end the path with the directory separator character.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <remarks>This constructor does not check if a file exists. This constructor is a placeholder for a string that is used to access the file in subsequent operations.</remarks>
      public FileInfo(string fileName, PathFormat pathFormat) : this(null, fileName, pathFormat)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="Alphaleonis.Win32.Filesystem.FileInfo"/> class, which acts as a wrapper for a file path.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileName">The fully qualified name of the new file, or the relative file name. Do not end the path with the directory separator character.</param>
      /// <remarks>This constructor does not check if a file exists. This constructor is a placeholder for a string that is used to access the file in subsequent operations.</remarks>
      public FileInfo(KernelTransaction transaction, string fileName) : this(transaction, fileName, PathFormat.RelativePath)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="Alphaleonis.Win32.Filesystem.FileInfo"/> class, which acts as a wrapper for a file path.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileName">The fully qualified name of the new file, or the relative file name. Do not end the path with the directory separator character.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <remarks>This constructor does not check if a file exists. This constructor is a placeholder for a string that is used to access the file in subsequent operations.</remarks>
      public FileInfo(KernelTransaction transaction, string fileName, PathFormat pathFormat)
      {
         InitializeCore(transaction, false, fileName, pathFormat);

         _name = Path.GetFileName(Path.RemoveTrailingDirectorySeparator(fileName), pathFormat != PathFormat.LongFullPath);
      }

      #endregion // Constructors


      #region Properties

      #region .NET

      /// <summary>Gets an instance of the parent directory.</summary>
      /// <value>A <see cref="DirectoryInfo"/> object representing the parent directory of this file.</value>
      /// <remarks>To get the parent directory as a string, use the DirectoryName property.</remarks>
      /// <exception cref="DirectoryNotFoundException"/>
      public DirectoryInfo Directory
      {
         get
         {
            var dirName = DirectoryName;
            return dirName == null ? null : new DirectoryInfo(Transaction, dirName, PathFormat.FullPath);
         }
      }


      /// <summary>Gets a string representing the directory's full path.</summary>
      /// <value>A string representing the directory's full path.</value>
      /// <remarks>
      ///   <para>To get the parent directory as a DirectoryInfo object, use the Directory property.</para>
      ///   <para>When first called, FileInfo calls Refresh and caches information about the file.</para>
      ///   <para>On subsequent calls, you must call Refresh to get the latest copy of the information.</para>
      /// </remarks>
      /// <exception cref="ArgumentNullException"/>
      public string DirectoryName
      {
         [SecurityCritical]
         get { return Path.GetDirectoryName(FullPath, false); }
      }


      /// <summary>Gets a value indicating whether the file exists.</summary>
      /// <value><c>true</c> if the file exists; otherwise, <c>false</c>.</value>
      /// <remarks>
      ///   <para>The <see cref="Exists"/> property returns <c>false</c> if any error occurs while trying to determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the file.</para>
      /// </remarks>
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

               var attrs = Win32AttributeData.dwFileAttributes;

               return DataInitialised == 0 && File.HasValidAttributes(attrs) && !IsDirectory;
            }
            catch
            {
               return false;
            }
         }
      }


      /// <summary>Gets or sets a value that determines if the current file is read only.</summary>
      /// <value><c>true</c> if the current file is read only; otherwise, <c>false</c>.</value>
      /// <remarks>
      ///   <para>Use the IsReadOnly property to quickly determine or change whether the current file is read only.</para>
      ///   <para>When first called, FileInfo calls Refresh and caches information about the file.</para>
      ///   <para>On subsequent calls, you must call Refresh to get the latest copy of the information.</para>
      /// </remarks>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      public bool IsReadOnly
      {
         get { return EntryInfo == null || EntryInfo.IsReadOnly; }

         set
         {
            if (value)
               Attributes |= FileAttributes.ReadOnly;
            else
               Attributes &= ~FileAttributes.ReadOnly;
         }
      }


      /// <summary>Gets the size, in bytes, of the current file.</summary>
      /// <value>The size of the current file in bytes.</value>
      /// <remarks>
      ///   <para>The value of the Length property is pre-cached</para>
      ///   <para>To get the latest value, call the Refresh method.</para>
      /// </remarks>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
      public long Length
      {
         [SecurityCritical]
         get
         {
            if (DataInitialised == -1)
            {
               Win32AttributeData = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();
               Refresh();
            }

            // MSDN: .NET 3.5+: IOException: Refresh cannot initialize the data. 
            if (DataInitialised != 0)
               NativeError.ThrowException(DataInitialised, FullName);


            var attrs = Win32AttributeData.dwFileAttributes;

            // MSDN: .NET 3.5+: FileNotFoundException: The file does not exist or the Length property is called for a directory.
            if (!File.HasValidAttributes(attrs))
               NativeError.ThrowException(Win32Errors.ERROR_FILE_NOT_FOUND, FullName);


            // MSDN: .NET 3.5+: FileNotFoundException: The file does not exist or the Length property is called for a directory.
            if (File.IsDirectory(attrs))
               NativeError.ThrowException(Win32Errors.ERROR_FILE_NOT_FOUND, string.Format(CultureInfo.InvariantCulture, Resources.Target_File_Is_A_Directory, FullName));


            return Win32AttributeData.FileSize;
         }
      }


      /// <summary>Gets the name of the file.</summary>
      /// <value>The name of the file.</value>
      /// <remarks>
      ///   <para>The name of the file includes the file extension.</para>
      ///   <para>When first called, <see cref="FileInfo"/> calls Refresh and caches information about the file.</para>
      ///   <para>On subsequent calls, you must call Refresh to get the latest copy of the information.</para>
      ///   <para>The name of the file includes the file extension.</para>
      /// </remarks>
      public override string Name
      {
         get { return _name; }
      }

      #endregion // .NET

      #endregion // Properties


      #region Methods

      /// <summary>Returns the path as a string.</summary>
      /// <returns>The path.</returns>
      public override string ToString()
      {
         return DisplayPath;
      }

      #endregion // Methods
   }
}
