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
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Provides the base class for both <see cref="FileInfo"/> and <see cref="DirectoryInfo"/> objects.</summary>
   [Serializable]
   [ComVisible(true)]
   public abstract class FileSystemInfo : MarshalByRefObject, IEquatable<FileSystemInfo>
   {
      #region Fields

      #region .NET

      /// <summary>Represents the fully qualified path of the file or directory.</summary>
      /// <remarks>
      ///   <para>Classes derived from <see cref="FileSystemInfo"/> can use the FullPath field</para>
      ///   <para>to determine the full path of the object being manipulated.</para>
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
      protected string FullPath;

      /// <summary>The path originally specified by the user, whether relative or absolute.</summary>
      [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
      protected string OriginalPath;

      #endregion // .NET


      // We use this field in conjunction with the Refresh methods, if we succeed we store a zero,
      // on failure we store the HResult in it so that we can give back a generic error back.
      [NonSerialized] internal int DataInitialised = -1;


      // The pre-cached FileSystemInfo information.
      [NonSerialized] internal NativeMethods.WIN32_FILE_ATTRIBUTE_DATA Win32AttributeData;

      #endregion // Fields


      #region Properties

      #region .NET

      /// <summary>Gets or sets the attributes for the current file or directory.</summary>
      /// <remarks>
      ///   <para>The value of the CreationTime property is pre-cached</para>
      ///   <para>To get the latest value, call the Refresh method.</para>
      /// </remarks>
      /// <value><see cref="FileAttributes"/> of the current <see cref="FileSystemInfo"/>.</value>
      ///
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      public FileAttributes Attributes
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
               NativeError.ThrowException(DataInitialised, FullPath);

            return Win32AttributeData.dwFileAttributes;
         }


         [SecurityCritical]
         set
         {
            File.SetAttributesCore(Transaction, IsDirectory, LongFullName, value, PathFormat.LongFullPath);
            Reset();
         }
      }


      /// <summary>Gets or sets the creation time of the current file or directory.</summary>
      /// <remarks>
      ///   <para>The value of the CreationTime property is pre-cached To get the latest value, call the Refresh method.</para>
      ///   <para>This method may return an inaccurate value, because it uses native functions whose values may not be continuously updated by
      ///   the operating system.</para>
      ///   <para>If the file described in the FileSystemInfo object does not exist, this property will return
      ///   12:00 midnight, January 1, 1601 A.D. (C.E.) Coordinated Universal Time (UTC), adjusted to local time.</para>
      ///   <para>NTFS-formatted drives may cache file meta-info, such as file creation time, for a short period of time.
      ///   This process is known as file tunneling. As a result, it may be necessary to explicitly set the creation time of a file if you are
      ///   overwriting or replacing an existing file.</para>
      /// </remarks>
      /// <value>The creation date and time of the current <see cref="FileSystemInfo"/> object.</value>
      ///
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      public DateTime CreationTime
      {
         [SecurityCritical] get { return CreationTimeUtc.ToLocalTime(); }

         [SecurityCritical] set { CreationTimeUtc = value.ToUniversalTime(); }
      }


      /// <summary>Gets or sets the creation time, in coordinated universal time (UTC), of the current file or directory.</summary>
      /// <remarks>
      ///   <para>The value of the CreationTimeUtc property is pre-cached
      ///   To get the latest value, call the Refresh method.</para>
      ///   <para>This method may return an inaccurate value, because it uses native functions
      ///   whose values may not be continuously updated by the operating system.</para>
      ///   <para>To get the latest value, call the Refresh method.</para>
      ///   <para>If the file described in the FileSystemInfo object does not exist, this property will return
      ///   12:00 midnight, January 1, 1601 A.D. (C.E.) Coordinated Universal Time (UTC).</para>
      ///   <para>NTFS-formatted drives may cache file meta-info, such as file creation time, for a short period of time.
      ///   This process is known as file tunneling. As a result, it may be necessary to explicitly set the creation time
      ///   of a file if you are overwriting or replacing an existing file.</para>
      /// </remarks>
      /// <value>The creation date and time in UTC format of the current <see cref="FileSystemInfo"/> object.</value>
      ///
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      [ComVisible(false)]
      public DateTime CreationTimeUtc
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
               NativeError.ThrowException(DataInitialised, LongFullName);

            return DateTime.FromFileTimeUtc(Win32AttributeData.ftCreationTime);
         }


         [SecurityCritical]
         set
         {
            File.SetFsoDateTimeCore(Transaction, IsDirectory, LongFullName, value, null, null, false, PathFormat.LongFullPath);

            Reset();
         }
      }


      /// <summary>Gets a value indicating whether the file or directory exists.</summary>
      /// <remarks>
      ///   <para>The <see cref="Exists"/> property returns <c>false</c> if any error occurs while trying to determine if the
      ///   specified file or directory exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a directory- or file name with invalid characters or too
      ///   many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the file or directory.</para>
      /// </remarks>
      /// <value><c>true</c> if the file or directory exists; otherwise, <c>false</c>.</value>
      public abstract bool Exists { get; }


      /// <summary>Gets the string representing the extension part of the file.</summary>
      /// <remarks>
      ///   The Extension property returns the <see cref="FileSystemInfo"/> extension, including the period (.).
      ///   For example, for a file c:\NewFile.txt, this property returns ".txt".
      /// </remarks>
      /// <value>A string containing the <see cref="FileSystemInfo"/> extension.</value>
      public string Extension
      {
         get { return Path.GetExtension(FullPath, false); }
      }


      /// <summary>Gets the full path of the directory or file.</summary>
      /// <value>A string containing the full path.</value>
      public virtual string FullName
      {
         [SecurityCritical] get { return FullPath; }
      }

      
      /// <summary>Gets or sets the time the current file or directory was last accessed.</summary>
      /// <remarks>
      ///   <para>The value of the LastAccessTime property is pre-cached
      ///   To get the latest value, call the Refresh method.</para>
      ///   <para>This method may return an inaccurate value, because it uses native functions
      ///   whose values may not be continuously updated by the operating system.</para>
      ///   <para>If the file described in the FileSystemInfo object does not exist, this property will return
      ///   12:00 midnight, January 1, 1601 A.D. (C.E.) Coordinated Universal Time (UTC), adjusted to local time.</para>
      /// </remarks>
      /// <value>The time that the current file or directory was last accessed.</value>
      ///
      /// <exception cref="IOException"/>
      public DateTime LastAccessTime
      {
         [SecurityCritical] get { return LastAccessTimeUtc.ToLocalTime(); }

         [SecurityCritical] set { LastAccessTimeUtc = value.ToUniversalTime(); }
      }


      /// <summary>Gets or sets the time, in coordinated universal time (UTC), that the current file or directory was last accessed.</summary>
      /// <remarks>
      ///   <para>The value of the LastAccessTimeUtc property is pre-cached.
      ///   To get the latest value, call the Refresh method.</para>
      ///   <para>This method may return an inaccurate value, because it uses native functions
      ///   whose values may not be continuously updated by the operating system.</para>
      ///   <para>If the file described in the FileSystemInfo object does not exist, this property will return
      ///   12:00 midnight, January 1, 1601 A.D. (C.E.) Coordinated Universal Time (UTC), adjusted to local time.</para>
      /// </remarks>
      /// <value>The UTC time that the current file or directory was last accessed.</value>
      ///
      /// <exception cref="IOException"/>
      [ComVisible(false)]
      public DateTime LastAccessTimeUtc
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
               NativeError.ThrowException(DataInitialised, LongFullName);

            return DateTime.FromFileTimeUtc(Win32AttributeData.ftLastAccessTime);
         }


         [SecurityCritical]
         set
         {
            File.SetFsoDateTimeCore(Transaction, IsDirectory, LongFullName, null, value, null, false, PathFormat.LongFullPath);

            Reset();
         }
      }


      /// <summary>Gets or sets the time when the current file or directory was last written to.</summary>
      /// <remarks>
      ///   <para>The value of the LastWriteTime property is pre-cached.
      ///   To get the latest value, call the Refresh method.</para>
      ///   <para>This method may return an inaccurate value, because it uses native functions
      ///   whose values may not be continuously updated by the operating system.</para>
      ///   <para>If the file described in the FileSystemInfo object does not exist, this property will return
      ///   12:00 midnight, January 1, 1601 A.D. (C.E.) Coordinated Universal Time (UTC), adjusted to local time.</para>
      /// </remarks>
      /// <value>The time the current file was last written.</value>
      ///
      /// <exception cref="IOException"/>
      public DateTime LastWriteTime
      {
         get { return LastWriteTimeUtc.ToLocalTime(); }

         set { LastWriteTimeUtc = value.ToUniversalTime(); }
      }


      /// <summary>Gets or sets the time, in coordinated universal time (UTC), when the current file or directory was last written to.</summary>
      /// <remarks>
      ///   <para>The value of the LastWriteTimeUtc property is pre-cached. To get the latest value, call the Refresh method.</para>
      ///   <para>This method may return an inaccurate value, because it uses native functions whose values may not be continuously updated by
      ///   the operating system.</para>
      ///   <para>If the file described in the FileSystemInfo object does not exist, this property will return 12:00 midnight, January 1, 1601
      ///   A.D. (C.E.) Coordinated Universal Time (UTC), adjusted to local time.</para>
      /// </remarks>
      /// <value>The UTC time when the current file was last written to.</value>
      [ComVisible(false)]
      public DateTime LastWriteTimeUtc
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
               NativeError.ThrowException(DataInitialised, LongFullName);

            return DateTime.FromFileTimeUtc(Win32AttributeData.ftLastWriteTime);
         }


         [SecurityCritical]
         set
         {
            File.SetFsoDateTimeCore(Transaction, IsDirectory, LongFullName, null, null, value, false, PathFormat.LongFullPath);

            Reset();
         }
      }


      /// <summary>
      ///   For files, gets the name of the file. For directories, gets the name of the last directory in the hierarchy if a hierarchy exists.
      ///   <para>Otherwise, the Name property gets the name of the directory.</para>
      /// </summary>
      /// <remarks>
      ///   <para>For a directory, Name returns only the name of the parent directory, such as Dir, not c:\Dir.</para>
      ///   <para>For a subdirectory, Name returns only the name of the subdirectory, such as Sub1, not c:\Dir\Sub1.</para>
      ///   <para>For a file, Name returns only the file name and file name extension, such as MyFile.txt, not c:\Dir\Myfile.txt.</para>
      /// </remarks>
      /// <value>
      ///   <para>A string that is the name of the parent directory, the name of the last directory in the hierarchy,</para>
      ///   <para>or the name of a file, including the file name extension.</para>
      /// </value>
      public abstract string Name { get; }

      #endregion // .NET


      #region AlphaFS

      /// <summary>Returns the path as a string.</summary>
      protected internal string DisplayPath { get; protected set; }


      private FileSystemEntryInfo _entryInfo;

      /// <summary>[AlphaFS] Gets the instance of the <see cref="FileSystemEntryInfo"/> class.</summary>
      public FileSystemEntryInfo EntryInfo
      {
         [SecurityCritical]
         get
         {
            if (null == _entryInfo)
            {
               Win32AttributeData = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();
               RefreshEntryInfo();
            }

            // MSDN: .NET 3.5+: IOException: Refresh cannot initialize the data. 
            if (DataInitialised > 0)
               NativeError.ThrowException(DataInitialised, LongFullName);

            return _entryInfo;
         }


         internal set
         {
            _entryInfo = value;

            DataInitialised = value == null ? -1 : 0;

            if (DataInitialised == 0 && null != _entryInfo)
               Win32AttributeData = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA(_entryInfo.Win32FindData);
         }
      }


      /// <summary>[AlphaFS] The initial "IsDirectory" indicator that was passed to the constructor.</summary>
      protected bool IsDirectory { get; set; }


      /// <summary>The full path of the file system object in Unicode (LongPath) format.</summary>
      protected string LongFullName { get; set; }


      /// <summary>[AlphaFS] Represents the KernelTransaction that was passed to the constructor.</summary>
      protected KernelTransaction Transaction { get; set; }

      #endregion // AlphaFS

      #endregion // Properties


      #region Methods

      #region .NET

      /// <summary>Deletes a file or directory.</summary>
      [SecurityCritical]
      public abstract void Delete();


      /// <summary>Refreshes the state of the object.</summary>
      /// <remarks>
      ///   <para>FileSystemInfo.Refresh() takes a snapshot of the file from the current file system.</para>
      ///   <para>Refresh cannot correct the underlying file system even if the file system returns incorrect or outdated information.</para>
      ///   <para>This can happen on platforms such as Windows 98.</para>
      ///   <para>Calls must be made to Refresh() before attempting to get the attribute information, or the information will be
      ///   outdated.</para>
      /// </remarks>
      [SecurityCritical]
      public void Refresh()
      {
         DataInitialised = File.FillAttributeInfoCore(Transaction, LongFullName, ref Win32AttributeData, false, false);

         IsDirectory = File.IsDirectory(Win32AttributeData.dwFileAttributes);
      }


      /// <summary>Returns a string that represents the current object.</summary>
      /// <remarks>
      ///   ToString is the major formatting method in the .NET Framework. It converts an object to its string representation so that it is
      ///   suitable for display.
      /// </remarks>
      /// <returns>A string that represents this instance.</returns>
      public override string ToString()
      {
         // "Alphaleonis.Win32.Filesystem.FileSystemInfo"
         return GetType().ToString();
      }


      /// <summary>Serves as a hash function for a particular type.</summary>
      /// <returns>A hash code for the current Object.</returns>
      public override int GetHashCode()
      {
         return null != FullName ? FullName.GetHashCode() : 0;
      }


      /// <summary>Determines whether the specified Object is equal to the current Object.</summary>
      /// <param name="other">Another <see cref="FileSystemInfo"/> instance to compare to.</param>
      /// <returns><c>true</c> if the specified Object is equal to the current Object; otherwise, <c>false</c>.</returns>
      public bool Equals(FileSystemInfo other)
      {
         return null != other && GetType() == other.GetType() &&
                Equals(Name, other.Name) &&
                Equals(FullName, other.FullName) &&
                Equals(Attributes, other.Attributes) &&
                Equals(CreationTimeUtc, other.CreationTimeUtc) &&
                Equals(LastAccessTimeUtc, other.LastAccessTimeUtc);
      }


      /// <summary>Determines whether the specified Object is equal to the current Object.</summary>
      /// <param name="obj">Another object to compare to.</param>
      /// <returns><c>true</c> if the specified Object is equal to the current Object; otherwise, <c>false</c>.</returns>
      public override bool Equals(object obj)
      {
         var other = obj as FileSystemInfo;

         return null != other && Equals(other);
      }


      /// <summary>Implements the operator ==</summary>
      /// <param name="left">A.</param>
      /// <param name="right">B.</param>
      /// <returns>The result of the operator.</returns>
      public static bool operator ==(FileSystemInfo left, FileSystemInfo right)
      {
         return ReferenceEquals(left, null) && ReferenceEquals(right, null) ||
                !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right);
      }


      /// <summary>Implements the operator !=</summary>
      /// <param name="left">A.</param>
      /// <param name="right">B.</param>
      /// <returns>The result of the operator.</returns>
      public static bool operator !=(FileSystemInfo left, FileSystemInfo right)
      {
         return !(left == right);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Refreshes the current <see cref="FileSystemInfo"/> instance (<see cref="DirectoryInfo"/> or <see cref="FileInfo"/>) with a new destination path.</summary>
      internal void UpdateSourcePath(string destinationPath, string destinationPathLp)
      {
         LongFullName = destinationPathLp;

         FullPath = null != destinationPathLp ? Path.GetRegularPathCore(LongFullName, GetFullPathOptions.None, false) : null;

         OriginalPath = destinationPath;

         DisplayPath = null != OriginalPath ? Path.GetRegularPathCore(OriginalPath, GetFullPathOptions.None, false) : null;

         // Flush any cached information about the FileSystemInfo instance.
         Reset();
      }


      /// <summary>[AlphaFS] Refreshes the state of the <see cref="FileSystemEntryInfo"/> EntryInfo property.</summary>
      /// <remarks>
      ///   <para>FileSystemInfo.RefreshEntryInfo() takes a snapshot of the file from the current file system.</para>
      ///   <para>Refresh cannot correct the underlying file system even if the file system returns incorrect or outdated information.</para>
      ///   <para>This can happen on platforms such as Windows 98.</para>
      ///   <para>Calls must be made to Refresh() before attempting to get the attribute information, or the information will be outdated.</para>
      /// </remarks>
      [SecurityCritical]
      protected void RefreshEntryInfo()
      {
         _entryInfo = File.GetFileSystemEntryInfoCore(Transaction, IsDirectory, LongFullName, true, PathFormat.LongFullPath);

         if (null == _entryInfo)
            DataInitialised = -1;

         else
         {
            DataInitialised = 0;
            Win32AttributeData = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA(_entryInfo.Win32FindData);
         }
      }


      /// <summary>[AlphaFS] Resets the state of the file system object to uninitialized.</summary>
      private void Reset()
      {
         DataInitialised = -1;
      }


      /// <summary>Initializes the specified file name.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="isFolder">Specifies that <paramref name="path"/> is a file or directory.</param>
      /// <param name="path">The full path and name of the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      internal void InitializeCore(KernelTransaction transaction, bool isFolder, string path, PathFormat pathFormat)
      {
         if (pathFormat == PathFormat.RelativePath)
            Path.CheckSupportedPathFormat(path, true, true);

         LongFullName = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.TrimEnd | (isFolder ? GetFullPathOptions.RemoveTrailingDirectorySeparator : 0) | GetFullPathOptions.ContinueOnNonExist);
         
         // (Not on MSDN): .NET 4+ Trailing spaces are removed from the end of the path parameter before creating the FileSystemInfo instance.

         FullPath = Path.GetRegularPathCore(LongFullName, GetFullPathOptions.None, false);

         IsDirectory = isFolder;

         Transaction = transaction;

         OriginalPath = FullPath.Length == 2 && FullPath[1] == Path.VolumeSeparatorChar ? Path.CurrentDirectoryPrefix : path;

         DisplayPath = OriginalPath.Length != 2 || OriginalPath[1] != Path.VolumeSeparatorChar ? Path.GetRegularPathCore(OriginalPath, GetFullPathOptions.None, false) : Path.CurrentDirectoryPrefix;
      }


      internal static SafeFindFileHandle FindFirstFileNative(KernelTransaction transaction, string pathLp, NativeMethods.FINDEX_INFO_LEVELS infoLevel, NativeMethods.FINDEX_SEARCH_OPS searchOption, NativeMethods.FIND_FIRST_EX_FLAGS additionalFlags, out int lastError, out NativeMethods.WIN32_FIND_DATA win32FindData)
      {
         var safeHandle = null == transaction || !NativeMethods.IsAtLeastWindowsVista

            // FindFirstFileEx() / FindFirstFileTransacted()
            // 2013-01-13: MSDN confirms LongPath usage.

            // A trailing backslash is not allowed.
            ? NativeMethods.FindFirstFileEx(Path.RemoveTrailingDirectorySeparator(pathLp), infoLevel, out win32FindData, searchOption, IntPtr.Zero, additionalFlags)

            : NativeMethods.FindFirstFileTransacted(Path.RemoveTrailingDirectorySeparator(pathLp), infoLevel, out win32FindData, searchOption, IntPtr.Zero, additionalFlags, transaction.SafeHandle);

         lastError = Marshal.GetLastWin32Error();

         if (!NativeMethods.IsValidHandle(safeHandle, false))
            safeHandle = null;


         return safeHandle;
      }

      #endregion // Methods
   }
}
