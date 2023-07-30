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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using winPEAS._3rdParty.AlphaFS;

#if !NET35
using System.Threading;
#endif

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Class that retrieves file system entries (i.e. files and directories) using Win32 API FindFirst()/FindNext().</summary>
   [Serializable]
   internal sealed class FindFileSystemEntryInfo
   {
      #region Fields

      [NonSerialized] private static readonly Regex WildcardMatchAll = new Regex(@"^(\*)+(\.\*+)+$", RegexOptions.IgnoreCase | RegexOptions.Compiled); // special case to recognize *.* or *.** etc
      [NonSerialized] private Regex _nameFilter;
      [NonSerialized] private string _searchPattern = Path.WildcardStarMatchAll;

      #endregion // Fields


      #region Constructor

      /// <summary>Initializes a new instance of the <see cref="FindFileSystemEntryInfo"/> class.</summary>
      /// <param name="transaction">The NTFS Kernel transaction, if used.</param>
      /// <param name="isFolder">if set to <c>true</c> the path is a folder.</param>
      /// <param name="path">The path.</param>
      /// <param name="searchPattern">The wildcard search pattern.</param>
      /// <param name="options">The enumeration options.</param>
      /// <param name="customFilters">The custom filters.</param>
      /// <param name="pathFormat">The format of the path.</param>
      /// <param name="typeOfT">The type of objects to be retrieved.</param>
      [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
      public FindFileSystemEntryInfo(KernelTransaction transaction, bool isFolder, string path, string searchPattern, DirectoryEnumerationOptions? options, DirectoryEnumerationFilters customFilters, PathFormat pathFormat, Type typeOfT)
      {
         if (null == options)
            throw new ArgumentNullException("options");


         Transaction = transaction;

         OriginalInputPath = path;

         InputPath = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         IsRelativePath = !Path.IsPathRooted(OriginalInputPath, false);

         RelativeAbsolutePrefix = IsRelativePath ? InputPath.Replace(OriginalInputPath, string.Empty) : null;
         
         SearchPattern = searchPattern.TrimEnd(Path.TrimEndChars); // .NET behaviour.

         FileSystemObjectType = null;

         ContinueOnException = (options & DirectoryEnumerationOptions.ContinueOnException) != 0;

         AsLongPath = (options & DirectoryEnumerationOptions.AsLongPath) != 0;

         AsString = typeOfT == typeof(string);
         AsFileSystemInfo = !AsString && (typeOfT == typeof(FileSystemInfo) || typeOfT.BaseType == typeof(FileSystemInfo));

         LargeCache = (options & DirectoryEnumerationOptions.LargeCache) != 0 ? NativeMethods.UseLargeCache : NativeMethods.FIND_FIRST_EX_FLAGS.NONE;

         // Only FileSystemEntryInfo makes use of (8.3) AlternateFileName.
         FindExInfoLevel = AsString || AsFileSystemInfo || (options & DirectoryEnumerationOptions.BasicSearch) != 0 ? NativeMethods.FindexInfoLevel : NativeMethods.FINDEX_INFO_LEVELS.Standard;


         if (null != customFilters)
         {
            InclusionFilter = customFilters.InclusionFilter;

            RecursionFilter = customFilters.RecursionFilter;

            ErrorHandler = customFilters.ErrorFilter;

#if !NET35
            CancellationToken = customFilters.CancellationToken;
#endif
         }


         if (isFolder)
         {
            IsDirectory = true;

            Recursive = (options & DirectoryEnumerationOptions.Recursive) != 0 || null != RecursionFilter;

            SkipReparsePoints = (options & DirectoryEnumerationOptions.SkipReparsePoints) != 0;


            // Need folders or files to enumerate.
            if ((options & DirectoryEnumerationOptions.FilesAndFolders) == 0)
               options |= DirectoryEnumerationOptions.FilesAndFolders;
         }

         else
         {
            options &= ~DirectoryEnumerationOptions.Folders; // Remove enumeration of folders.
            options |= DirectoryEnumerationOptions.Files; // Add enumeration of files.
         }


         FileSystemObjectType = (options & DirectoryEnumerationOptions.FilesAndFolders) == DirectoryEnumerationOptions.FilesAndFolders

            // Folders and files (null).
            ? (bool?) null

            // Only folders (true) or only files (false).
            : (options & DirectoryEnumerationOptions.Folders) != 0;
      }

      #endregion // Constructor


      #region Properties

      /// <summary>Gets or sets the ability to return the object as a <see cref="FileSystemInfo"/> instance.</summary>
      /// <value><c>true</c> returns the object as a <see cref="FileSystemInfo"/> instance.</value>
      public bool AsFileSystemInfo { get; private set; }


      /// <summary>Gets or sets the ability to return the full path in long full path format.</summary>
      /// <value><c>true</c> returns the full path in long full path format, <c>false</c> returns the full path in regular path format.</value>
      public bool AsLongPath { get; private set; }


      /// <summary>Gets or sets the ability to return the object instance as a <see cref="string"/>.</summary>
      /// <value><c>true</c> returns the full path of the object as a <see cref="string"/></value>
      public bool AsString { get; private set; }


      /// <summary>Gets or sets the ability to skip on access errors.</summary>
      /// <value><c>true</c> suppress any Exception that might be thrown as a result from a failure, such as ACLs protected directories or non-accessible reparse points.</value>
      public bool ContinueOnException { get; private set; }


      /// <summary>Gets the file system object type.</summary>
      /// <value>
      /// <c>null</c> = Return files and directories.
      /// <c>true</c> = Return only directories.
      /// <c>false</c> = Return only files.
      /// </value>
      public bool? FileSystemObjectType { get; private set; }


      /// <summary>Gets or sets if the path is an absolute or relative path.</summary>
      /// <value>Gets a value indicating whether the specified path string contains absolute or relative path information.</value>
      public bool IsRelativePath { get; private set; }

      
      /// <summary>Gets or sets the initial path to the folder.</summary>
      /// <value>The initial path to the file or folder in long path format.</value>
      public string OriginalInputPath { get; private set; }


      /// <summary>Gets or sets the path to the folder.</summary>
      /// <value>The path to the file or folder in long path format.</value>
      public string InputPath { get; private set; }


      /// <summary>Gets or sets the absolute full path prefix of the relative path.</summary>
      private string RelativeAbsolutePrefix { get; set; }

      
      /// <summary>Gets or sets a value indicating which <see cref="NativeMethods.FINDEX_INFO_LEVELS"/> to use.</summary>
      /// <value><c>true</c> indicates a folder object, <c>false</c> indicates a file object.</value>
      public bool IsDirectory { get; private set; }


      /// <summary>Uses a larger buffer for directory queries, which can increase performance of the find operation.</summary>
      /// <remarks>This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
      public NativeMethods.FIND_FIRST_EX_FLAGS LargeCache { get; private set; }


      /// <summary>The FindFirstFileEx function does not query the short file name, improving overall enumeration speed.</summary>
      /// <remarks>This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
      public NativeMethods.FINDEX_INFO_LEVELS FindExInfoLevel { get; private set; }


      /// <summary>Specifies whether the search should include only the current directory or should include all subdirectories.</summary>
      /// <value><c>true</c> to include all subdirectories.</value>
      public bool Recursive { get; private set; }


      /// <summary>Search for file system object-name using a pattern.</summary>
      /// <value>The path which has wildcard characters, for example, an asterisk (<see cref="Path.WildcardStarMatchAll"/>) or a question mark (<see cref="Path.WildcardQuestion"/>).</value>
      [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
      public string SearchPattern
      {
         get { return _searchPattern; }

         internal set
         {
            if (null == value)
               throw new ArgumentNullException("SearchPattern");

            _searchPattern = value;

            _nameFilter = _searchPattern == Path.WildcardStarMatchAll || WildcardMatchAll.IsMatch(_searchPattern)
               ? null
               : new Regex(string.Format(CultureInfo.InvariantCulture, "^{0}$", Regex.Escape(_searchPattern).Replace(@"\*", ".*").Replace(@"\?", ".")), RegexOptions.IgnoreCase | RegexOptions.Compiled);
         }
      }


      /// <summary><c>true</c> skips ReparsePoints, <c>false</c> will follow ReparsePoints.</summary>
      public bool SkipReparsePoints { get; private set; }


      /// <summary>Get or sets the KernelTransaction instance.</summary>
      /// <value>The transaction.</value>
      public KernelTransaction Transaction { get; private set; }


      /// <summary>Gets or sets the custom enumeration in/exclusion filter.</summary>
      /// <value>The method determining if the object should be in/excluded from the output or not.</value>
      public Predicate<FileSystemEntryInfo> InclusionFilter { get; private set; }


      /// <summary>Gets or sets the custom enumeration recursion filter.</summary>
      /// <value>The method determining if the directory should be recursively traversed or not.</value>
      public Predicate<FileSystemEntryInfo> RecursionFilter { get; private set; }


      /// <summary>Gets or sets the handler of errors that may occur.</summary>
      /// <value>The error handler method.</value>
      public ErrorHandler ErrorHandler { get; private set; }


#if !NET35
      /// <summary>Gets or sets the cancellation token to abort the enumeration.</summary>
      /// <value>A <see cref="CancellationToken"/> instance.</value>
      private CancellationToken CancellationToken { get; set; }
#endif

      #endregion // Properties


      #region Methods

      private SafeFindFileHandle FindFirstFile(string pathLp, out NativeMethods.WIN32_FIND_DATA win32FindData, out int lastError, bool suppressException = false)
      {
         lastError = (int) Win32Errors.NO_ERROR;

         var searchOption = null != FileSystemObjectType && (bool) FileSystemObjectType ? NativeMethods.FINDEX_SEARCH_OPS.SearchLimitToDirectories : NativeMethods.FINDEX_SEARCH_OPS.SearchNameMatch;

         var handle = FileSystemInfo.FindFirstFileNative(Transaction, pathLp, FindExInfoLevel, searchOption, LargeCache, out lastError, out win32FindData);


         if (!suppressException && !ContinueOnException)
         {
            if (null == handle)
            {
               switch ((uint) lastError)
               {
                  case Win32Errors.ERROR_FILE_NOT_FOUND: // FileNotFoundException.
                  case Win32Errors.ERROR_PATH_NOT_FOUND: // DirectoryNotFoundException.
                  case Win32Errors.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.

                     Directory.ExistsDriveOrFolderOrFile(Transaction, pathLp, IsDirectory, lastError, true, true);
                     break;
               }


               ThrowPossibleException((uint) lastError, pathLp);
            }
         }


         return handle;
      }


      private FileSystemEntryInfo NewFilesystemEntry(string pathLp, string fileName, NativeMethods.WIN32_FIND_DATA win32FindData)
      {
         var fullPath = (IsRelativePath ? pathLp.Replace(RelativeAbsolutePrefix, string.Empty) : pathLp) + fileName;

         return new FileSystemEntryInfo(win32FindData) {FullPath = fullPath};
      }


      private T NewFileSystemEntryType<T>(bool isFolder, FileSystemEntryInfo fsei, string fileName, string pathLp, NativeMethods.WIN32_FIND_DATA win32FindData)
      {
         // Determine yield, e.g. don't return files when only folders are requested and vice versa.

         if (null != FileSystemObjectType && (!(bool) FileSystemObjectType || !isFolder) && (!(bool) !FileSystemObjectType || isFolder))

            return (T) (object) null;


         // Determine yield from name filtering.

         if (null != fileName && !(null == _nameFilter || null != _nameFilter && _nameFilter.IsMatch(fileName)))

            return (T) (object) null;


         if (null == fsei)
            fsei = NewFilesystemEntry(pathLp, fileName, win32FindData);


         // Return object instance FullPath property as string, optionally in long path format.

         return AsString ? null == InclusionFilter || InclusionFilter(fsei) ? (T) (object) (AsLongPath ? fsei.LongFullPath : fsei.FullPath) : (T) (object) null


            // Make sure the requested file system object type is returned.
            // null = Return files and directories.
            // true = Return only directories.
            // false = Return only files.

            : null != InclusionFilter && !InclusionFilter(fsei)
               ? (T) (object) null

               // Return object instance of type FileSystemInfo.

               : AsFileSystemInfo
                  ? (T) (object) (fsei.IsDirectory

                     ? (FileSystemInfo) new DirectoryInfo(Transaction, fsei.LongFullPath, PathFormat.LongFullPath) {EntryInfo = fsei}

                     : new FileInfo(Transaction, fsei.LongFullPath, PathFormat.LongFullPath) {EntryInfo = fsei})

                  // Return object instance of type FileSystemEntryInfo.

                  : (T) (object) fsei;
      }


      private void ThrowPossibleException(uint lastError, string pathLp)
      {
         switch (lastError)
         {
            case Win32Errors.ERROR_NO_MORE_FILES:
               lastError = Win32Errors.NO_ERROR;
               break;


            case Win32Errors.ERROR_FILE_NOT_FOUND: // On files.
            case Win32Errors.ERROR_PATH_NOT_FOUND: // On folders.
            case Win32Errors.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.
               // MSDN: .NET 3.5+: DirectoryNotFoundException: Path is invalid, such as referring to an unmapped drive.
               // Directory.Delete()

               lastError = IsDirectory ? (int) Win32Errors.ERROR_PATH_NOT_FOUND : Win32Errors.ERROR_FILE_NOT_FOUND;
               break;


            //case Win32Errors.ERROR_DIRECTORY:
            //   // MSDN: .NET 3.5+: IOException: path is a file name.
            //   // Directory.EnumerateDirectories()
            //   // Directory.EnumerateFiles()
            //   // Directory.EnumerateFileSystemEntries()
            //   // Directory.GetDirectories()
            //   // Directory.GetFiles()
            //   // Directory.GetFileSystemEntries()
            //   break;

            //case Win32Errors.ERROR_ACCESS_DENIED:
            //   // MSDN: .NET 3.5+: UnauthorizedAccessException: The caller does not have the required permission.
            //   break;
         }


         if (lastError != Win32Errors.NO_ERROR)
         {
            var regularPath = Path.GetCleanExceptionPath(pathLp);

            // Pass control to the ErrorHandler when set.

            if (null == ErrorHandler || !ErrorHandler((int) lastError, new Win32Exception((int) lastError).Message, regularPath))
            {
               // When the ErrorHandler returns false, thrown the Exception.

               NativeError.ThrowException(lastError, regularPath);
            }
         }
      }


      private void VerifyInstanceType(NativeMethods.WIN32_FIND_DATA win32FindData)
      {
         var regularPath = Path.GetCleanExceptionPath(InputPath);

         var isFolder = File.IsDirectory(win32FindData.dwFileAttributes);

         if (IsDirectory)
         {
            if (!isFolder)
               throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "({0}) {1}", Win32Errors.ERROR_PATH_NOT_FOUND, string.Format(CultureInfo.InvariantCulture, Resources.Target_Directory_Is_A_File, regularPath)));
         }

         else if (isFolder)
            throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "({0}) {1}", Win32Errors.ERROR_FILE_NOT_FOUND, string.Format(CultureInfo.InvariantCulture, Resources.Target_File_Is_A_Directory, regularPath)));
      }


      /// <summary>Gets an enumerator that returns all of the file system objects that match both the wildcards that are in any of the directories to be searched and the custom predicate.</summary>
      /// <returns>An <see cref="IEnumerable{T}"/> instance: FileSystemEntryInfo, DirectoryInfo, FileInfo or string (full path).</returns>
      [SecurityCritical]
      public IEnumerable<T> Enumerate<T>()
      {
         // MSDN: Queue
         // Represents a first-in, first-out collection of objects.
         // The capacity of a Queue is the number of elements the Queue can hold.
         // As elements are added to a Queue, the capacity is automatically increased as required through reallocation. The capacity can be decreased by calling TrimToSize.
         // The growth factor is the number by which the current capacity is multiplied when a greater capacity is required. The growth factor is determined when the Queue is constructed.
         // The capacity of the Queue will always increase by a minimum value, regardless of the growth factor; a growth factor of 1.0 will not prevent the Queue from increasing in size.
         // If the size of the collection can be estimated, specifying the initial capacity eliminates the need to perform a number of resizing operations while adding elements to the Queue.
         // This constructor is an O(n) operation, where n is capacity.

         var dirs = new Queue<string>(NativeMethods.DefaultFileBufferSize);

         dirs.Enqueue(Path.AddTrailingDirectorySeparator(InputPath, false));


         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))

            while (dirs.Count > 0
#if !NET35
               && !CancellationToken.IsCancellationRequested
#endif
            )
            {
               int lastError;

               NativeMethods.WIN32_FIND_DATA win32FindData;


               // Removes the object at the beginning of your Queue.
               // The algorithmic complexity of this is O(1). It doesn't loop over elements.

               var pathLp = dirs.Dequeue();
               

               using (var handle = FindFirstFile(pathLp + Path.WildcardStarMatchAll, out win32FindData, out lastError))
               {
                  // When the handle is null and we are still here, it means the ErrorHandler is active.
                  // We hit an inaccessible folder, so break and continue with the next one.
                  if (null == handle)
                     continue;

                  do
                  {
                     if (lastError == (int) Win32Errors.ERROR_NO_MORE_FILES)
                     {
                        lastError = (int) Win32Errors.NO_ERROR;
                        continue;
                     }


                     // Skip reparse points here to cleanly separate regular directories from links.
                     if (SkipReparsePoints && (win32FindData.dwFileAttributes & FileAttributes.ReparsePoint) != 0)
                        continue;


                     var fileName = win32FindData.cFileName;

                     var isFolder = (win32FindData.dwFileAttributes & FileAttributes.Directory) != 0;

                     // Skip entries ".." and "."
                     if (isFolder && (fileName.Equals(Path.ParentDirectoryPrefix, StringComparison.Ordinal) || fileName.Equals(Path.CurrentDirectoryPrefix, StringComparison.Ordinal)))
                        continue;


                     var fsei = NewFilesystemEntry(pathLp, fileName, win32FindData);

                     var res = NewFileSystemEntryType<T>(isFolder, fsei, fileName, pathLp, win32FindData);


                     // If recursion is requested, add it to the queue for later traversal.
                     if (isFolder && Recursive && (null == RecursionFilter || RecursionFilter(fsei)))

                        dirs.Enqueue(Path.AddTrailingDirectorySeparator(pathLp + fileName, false));


                     // Codacy: When constraints have not been applied to restrict a generic type parameter to be a reference type, then a value type,
                     // such as a struct, could also be passed. In such cases, comparing the type parameter to null would always be false,
                     // because a struct can be empty, but never null. If a value type is truly what's expected, then the comparison should use default().
                     // If it's not, then constraints should be added so that no value type can be passed.

                     if (Equals(res, default(T)))
                        continue;


                     yield return res;

                  } while (
#if !NET35
                     !CancellationToken.IsCancellationRequested &&
#endif
                     NativeMethods.FindNextFile(handle, out win32FindData));


                  lastError = Marshal.GetLastWin32Error();

                  if (!ContinueOnException
#if !NET35
                      && !CancellationToken.IsCancellationRequested
#endif
                  )
                     ThrowPossibleException((uint) lastError, pathLp);
               }
            }
      }


      /// <summary>Gets a specific file system object.</summary>
      /// <returns>
      /// <para>The return type is based on C# inference. Possible return types are:</para>
      /// <para> <see cref="string"/>- (full path), <see cref="FileSystemInfo"/>- (<see cref="DirectoryInfo"/> or <see cref="FileInfo"/>), <see cref="FileSystemEntryInfo"/> instance</para>
      /// <para>or null in case an Exception is raised and <see cref="ContinueOnException"/> is <c>true</c>.</para>
      /// </returns>
      [SecurityCritical]
      public T Get<T>()
      {
         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         {
            NativeMethods.WIN32_FIND_DATA win32FindData;
            var lastError = 0;

            // Not explicitly set to be a folder.

            if (!IsDirectory)
            {
               using (var handle = FindFirstFile(InputPath, out win32FindData, out lastError))
               {
                  if (null != handle)
                  {
                     if (!ContinueOnException)
                        VerifyInstanceType(win32FindData);
                  }

                  else
                     return (T) (object) null;


                  return NewFileSystemEntryType<T>((win32FindData.dwFileAttributes & FileAttributes.Directory) != 0, null, null, InputPath, win32FindData);
               }
            }


            using (var handle = FindFirstFile(InputPath, out win32FindData, out lastError, true))
            {
               if (null == handle)
               {
                  // InputPath might be a logical drive such as: "C:\", "D:\".

                  var attrs = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();

                  lastError = File.FillAttributeInfoCore(Transaction, Path.GetRegularPathCore(InputPath, GetFullPathOptions.None, false), ref attrs, false, true);

                  if (lastError != Win32Errors.NO_ERROR)
                  {
                     if (!ContinueOnException)
                     {
                        switch ((uint) lastError)
                        {
                           case Win32Errors.ERROR_FILE_NOT_FOUND: // FileNotFoundException.
                           case Win32Errors.ERROR_PATH_NOT_FOUND: // DirectoryNotFoundException.
                           case Win32Errors.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.
                           case Win32Errors.ERROR_BAD_NET_NAME:

                              Directory.ExistsDriveOrFolderOrFile(Transaction, InputPath, IsDirectory, lastError, true, true);
                              break;
                        }

                        ThrowPossibleException((uint) lastError, InputPath);
                     }

                     return (T) (object) null;
                  }


                  win32FindData = new NativeMethods.WIN32_FIND_DATA
                  {
                     cFileName = Path.CurrentDirectoryPrefix,
                     dwFileAttributes = attrs.dwFileAttributes,
                     ftCreationTime = attrs.ftCreationTime,
                     ftLastAccessTime = attrs.ftLastAccessTime,
                     ftLastWriteTime = attrs.ftLastWriteTime,
                     nFileSizeHigh = attrs.nFileSizeHigh,
                     nFileSizeLow = attrs.nFileSizeLow
                  };
               }


               if (!ContinueOnException)
                  VerifyInstanceType(win32FindData);
            }


            return NewFileSystemEntryType<T>((win32FindData.dwFileAttributes & FileAttributes.Directory) != 0, null, null, InputPath, win32FindData);
         }
      }

      #endregion // Methods
   }
}
