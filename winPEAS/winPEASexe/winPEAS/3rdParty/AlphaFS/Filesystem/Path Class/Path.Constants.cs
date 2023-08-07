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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>[AlphaFS] Characters to trim from the SearchPattern.</summary>
      internal static readonly char[] TrimEndChars = {(char) 0x9, (char) 0xA, (char) 0xB, (char) 0xC, (char) 0xD, (char) 0x20, (char) 0x85, (char) 0xA0};

      /// <summary>AltDirectorySeparatorChar = '/' Provides a platform-specific alternate character used to separate directory levels in a path string that reflects a hierarchical file system organization.</summary>
      public static readonly char AltDirectorySeparatorChar = System.IO.Path.AltDirectorySeparatorChar;

      /// <summary>[AlphaFS] AltDirectorySeparatorChar = "/" Provides a platform-specific alternate string used to separate directory levels in a path string that reflects a hierarchical file system organization.</summary>
      public static readonly string AltDirectorySeparator = AltDirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);


      /// <summary>DirectorySeparatorChar = '\' Provides a platform-specific character used to separate directory levels in a path string that reflects a hierarchical file system organization.</summary>
      public static readonly char DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

      /// <summary>[AlphaFS] DirectorySeparator = "\" Provides a platform-specific string used to separate directory levels in a path string that reflects a hierarchical file system organization.</summary>
      public static readonly string DirectorySeparator = DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);


      /// <summary>[AlphaFS] NetworkDriveSeparator = '$' Provides a platform-specific network drive separator character.</summary>
      public const char NetworkDriveSeparatorChar = '$';

      /// <summary>[AlphaFS] NetworkDriveSeparator = "$" Provides a platform-specific network drive separator string.</summary>
      public static readonly string NetworkDriveSeparator = NetworkDriveSeparatorChar.ToString(CultureInfo.InvariantCulture);


      /// <summary>PathSeparator = ';' A platform-specific separator character used to separate path strings in environment variables.</summary>
      public static readonly char PathSeparator = System.IO.Path.PathSeparator;


      /// <summary>VolumeSeparatorChar = ':' Provides a platform-specific Volume Separator character.</summary>
      public static readonly char VolumeSeparatorChar = System.IO.Path.VolumeSeparatorChar;

      /// <summary>[AlphaFS] VolumeSeparator = ":" Provides a platform-specific Volume Separator string.</summary>
      public static readonly string VolumeSeparator = VolumeSeparatorChar.ToString(CultureInfo.InvariantCulture);


      /// <summary>[AlphaFS] StreamSeparator = ':' Provides a platform-specific Stream-name character.</summary>
      public const char StreamSeparatorChar = ':';

      /// <summary>[AlphaFS] StreamSeparator = ':' Provides a platform-specific Stream-name string.</summary>
      public static readonly string StreamSeparator = StreamSeparatorChar.ToString(CultureInfo.InvariantCulture);


      /// <summary>[AlphaFS] StreamDataLabel = ':$DATA' Provides a platform-specific Stream :$DATA label.</summary>
      public static readonly string StreamDataLabel = StreamSeparator + "$DATA";

      /// <summary>[AlphaFS] StringTerminatorChar = '\0' String Terminator Suffix.</summary>
      public const char StringTerminatorChar = '\0';
      
      
      /// <summary>[AlphaFS] CurrentDirectoryPrefix = '.' Provides a current directory character.</summary>
      public const char CurrentDirectoryPrefixChar = '.';

      /// <summary>[AlphaFS] CurrentDirectoryPrefix = "." Provides a current directory string.</summary>
      public static readonly string CurrentDirectoryPrefix = CurrentDirectoryPrefixChar.ToString(CultureInfo.InvariantCulture);
      
      /// <summary>[AlphaFS] ExtensionSeparatorChar = '.' Provides an Extension Separator character.</summary>
      public const char ExtensionSeparatorChar = '.';

      /// <summary>[AlphaFS] ParentDirectoryPrefix = ".." Provides a parent directory string.</summary>
      public const string ParentDirectoryPrefix = "..";
      
      /// <summary>[AlphaFS] WildcardStarMatchAll = '*' Provides a match-all-items character.</summary>
      public const char WildcardStarMatchAllChar = '*';
      
      /// <summary>[AlphaFS] WildcardStarMatchAll = "*" Provides a match-all-items string.</summary>
      public static readonly string WildcardStarMatchAll = WildcardStarMatchAllChar.ToString(CultureInfo.InvariantCulture);
      
      /// <summary>[AlphaFS] WildcardQuestion = '?' Provides a replace-item string.</summary>
      public const char WildcardQuestionChar = '?';

      /// <summary>[AlphaFS] WildcardQuestion = "?" Provides a replace-item string.</summary>
      public static readonly string WildcardQuestion = WildcardQuestionChar.ToString(CultureInfo.InvariantCulture);


      /// <summary>[AlphaFS] Win32 File Namespace. The "\\?\" prefix to a path string tells the Windows APIs to disable all string parsing and to send the string that follows it straight to the file system.</summary>
      public static readonly string LongPathPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{0}{1}{0}", DirectorySeparatorChar, WildcardQuestion);

      /// <summary>[AlphaFS] Win32 Device Namespace. The "\\.\"prefix is how to access physical disks and volumes, without going through the file system, if the API supports this type of access.</summary>
      public static readonly string LogicalDrivePrefix = string.Format(CultureInfo.InvariantCulture, "{0}{0}.{0}", DirectorySeparatorChar);

      /// <summary>[AlphaFS] PhysicalDrivePrefix = "\\.\PhysicalDrive" Provides standard physical drive prefix.</summary>
      public static readonly string PhysicalDrivePrefix = string.Format(CultureInfo.InvariantCulture, "{0}PhysicalDrive", LogicalDrivePrefix);


      /// <summary>[AlphaFS] GlobalRootPrefix = "\\?\GlobalRoot\" Provides standard Windows Volume prefix.</summary>
      public static readonly string GlobalRootPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", LongPathPrefix, "GlobalRoot", DirectorySeparatorChar);

      /// <summary>[AlphaFS] GlobalRootDevicePrefix = "\\?\GlobalRoot\Device\" Provides standard Windows Volume prefix.</summary>
      public static readonly string GlobalRootDevicePrefix = string.Format(CultureInfo.InvariantCulture, "{0}{2}{1}{3}{1}", LongPathPrefix, DirectorySeparatorChar, "GlobalRoot", "Device");

      /// <summary>[AlphaFS] NonInterpretedPathPrefix = "\??\" Provides a non-interpreted path prefix.</summary>
      public static readonly string NonInterpretedPathPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{1}{0}", DirectorySeparatorChar, WildcardQuestion);

      /// <summary>[AlphaFS] VolumePrefix = "\\?\Volume" Provides standard Windows Volume prefix.</summary>
      public static readonly string VolumePrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}", LongPathPrefix, "Volume");

      /// <summary>[AlphaFS] DevicePrefix = "\Device\" Provides standard Windows Device prefix.</summary>
      public static readonly string DevicePrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{0}", DirectorySeparatorChar, "Device");

      /// <summary>[AlphaFS] DosDeviceLanmanPrefix = "\Device\LanmanRedirector\" Provides a MS-Dos Lanman Redirector Path UNC prefix to a network share.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lanman")]
      [Obsolete("Unused")]
      public static readonly string DosDeviceLanmanPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", DevicePrefix, "LanmanRedirector", DirectorySeparatorChar);

      /// <summary>[AlphaFS] DosDeviceMupPrefix = "\Device\Mup\" Provides a MS-Dos Mup Redirector Path UNC prefix to a network share.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mup")]
      [Obsolete("Unused")]
      public static readonly string DosDeviceMupPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", DevicePrefix, "Mup", DirectorySeparatorChar);


      /// <summary>[AlphaFS] UncPrefix = "\\" Provides standard Windows Path UNC prefix.</summary>
      public static readonly string UncPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{0}", DirectorySeparatorChar);

      /// <summary>[AlphaFS] DosDeviceUncPrefix = "\??\UNC\" Provides a SUBST.EXE Path UNC prefix to a network share.</summary>
      public static readonly string DosDeviceUncPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", NonInterpretedPathPrefix, "UNC", DirectorySeparatorChar);

      /// <summary>[AlphaFS] LongPathUncPrefix = "\\?\UNC\" Provides standard Windows Long Path UNC prefix.</summary>
      public static readonly string LongPathUncPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", LongPathPrefix, "UNC", DirectorySeparatorChar);
   }
}
