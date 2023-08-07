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

using System.IO;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      public static readonly bool IsAtLeastWindows8 = OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows8);
      public static readonly bool IsAtLeastWindows7 = OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows7);
      public static readonly bool IsAtLeastWindowsVista = OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsVista);

      /// <summary>The FindFirstFileEx function does not query the short file name, improving overall enumeration speed.
      /// <para>&#160;</para>
      /// <remarks>
      /// <para>The data is returned in a <see cref="WIN32_FIND_DATA"/> structure,</para>
      /// <para>and cAlternateFileName member is always a NULL string.</para>
      /// <para>This value is not supported until Windows Server 2008 R2 and Windows 7.</para>
      /// </remarks>
      /// </summary>
      public static readonly FINDEX_INFO_LEVELS FindexInfoLevel = IsAtLeastWindows7 ? FINDEX_INFO_LEVELS.Basic : FINDEX_INFO_LEVELS.Standard;

      /// <summary>Uses a larger buffer for directory queries, which can increase performance of the find operation.</summary>
      /// <remarks>This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
      public static readonly FIND_FIRST_EX_FLAGS UseLargeCache = IsAtLeastWindows7 ? FIND_FIRST_EX_FLAGS.LARGE_FETCH : FIND_FIRST_EX_FLAGS.NONE;

      /// <summary>DefaultFileBufferSize = 4096; Default type buffer size used for reading and writing files.</summary>
      public const int DefaultFileBufferSize = 4096;

      /// <summary>DefaultFileEncoding = Encoding.UTF8; Default type of Encoding used for reading and writing files.</summary>
      public static readonly Encoding DefaultFileEncoding = Encoding.UTF8;

      /// <summary>MaxDirectoryLength = 255</summary>
      internal const int MaxDirectoryLength = 255;

      /// <summary>MaxPath = 260
      /// The specified path, file name, or both exceed the system-defined maximum length.
      /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters. 
      /// </summary>
      internal const int MaxPath = 260;

      /// <summary>MaxPathUnicode = 32700</summary>
      internal const int MaxPathUnicode = 32700;


      /// <summary>When an exception is raised, bit shifting is needed to prevent: "System.OverflowException: Arithmetic operation resulted in an overflow."</summary>
      internal const int OverflowExceptionBitShift = 65535;


      /// <summary>Invalid FileAttributes = -1</summary>
      internal const FileAttributes InvalidFileAttributes = (FileAttributes) (-1);




      /// <summary>MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16384</summary>
      internal const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16384;

      /// <summary>REPARSE_DATA_BUFFER_HEADER_SIZE = 8</summary>
      internal const int REPARSE_DATA_BUFFER_HEADER_SIZE = 8;


      private const int DeviceIoControlMethodBuffered = 0;
      private const int DeviceIoControlFileDeviceFileSystem = 9;

      // <summary>Command to compression state of a file or directory on a volume whose file system supports per-file and per-directory compression.</summary>
      internal const int FSCTL_SET_COMPRESSION = (DeviceIoControlFileDeviceFileSystem << 16) | (16 << 2) | DeviceIoControlMethodBuffered | (int) (FileAccess.Read | FileAccess.Write) << 14;

      // <summary>Command to set the reparse point data block.</summary>
      internal const int FSCTL_SET_REPARSE_POINT = (DeviceIoControlFileDeviceFileSystem << 16) | (41 << 2) | DeviceIoControlMethodBuffered | (0 << 14);
      
      /// <summary>Command to delete the reparse point data base.</summary>
      internal const int FSCTL_DELETE_REPARSE_POINT = (DeviceIoControlFileDeviceFileSystem << 16) | (43 << 2) | DeviceIoControlMethodBuffered | (0 << 14);

      /// <summary>Command to get the reparse point data block.</summary>
      internal const int FSCTL_GET_REPARSE_POINT = (DeviceIoControlFileDeviceFileSystem << 16) | (42 << 2) | DeviceIoControlMethodBuffered | (0 << 14);
   }
}
