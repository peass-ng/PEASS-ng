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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Provides access to information on a local or remote drive.</summary>
   /// <remarks>
   /// This class models a drive and provides methods and properties to query for drive information.
   /// Use DriveInfo to determine what drives are available, and what type of drives they are.
   /// You can also query to determine the capacity and available free space on the drive.
   /// </remarks>
   [Serializable]
   [SecurityCritical]
   public sealed class DriveInfo
   {
      [NonSerialized] private readonly VolumeInfo _volumeInfo;
      [NonSerialized] private readonly DiskSpaceInfo _dsi;
      [NonSerialized] private bool _initDsie;
      [NonSerialized] private DriveType? _driveType;
      [NonSerialized] private string _dosDeviceName;
      [NonSerialized] private DirectoryInfo _rootDirectory;
      [NonSerialized] private readonly string _name;


      #region Constructors

      /// <summary>Provides access to information on the specified drive.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <param name="driveName">
      ///   A valid drive path or drive letter.
      ///   <para>This can be either uppercase or lowercase,</para>
      ///   <para>'a' to 'z' or a network share in the format: \\server\share</para>
      /// </param>
      [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Utils.IsNullOrWhiteSpace validates arguments.")]
      [SecurityCritical]
      public DriveInfo(string driveName)
      {
         if (Utils.IsNullOrWhiteSpace(driveName))
            throw new ArgumentNullException("driveName");


         driveName = driveName.Length == 1 ? driveName + Path.VolumeSeparatorChar : Path.GetPathRoot(driveName, false);

         if (Utils.IsNullOrWhiteSpace(driveName))
            throw new ArgumentException(Resources.InvalidDriveLetterArgument, "driveName");


         _name = Path.AddTrailingDirectorySeparator(driveName, false);

         // Initiate VolumeInfo() lazyload instance.
         _volumeInfo = new VolumeInfo(_name, false, true);

         // Initiate DiskSpaceInfo() lazyload instance.
         _dsi = new DiskSpaceInfo(_name, null, false, true);
      }

      #endregion // Constructors


      #region Properties

      /// <summary>Indicates the amount of available free space on a drive.</summary>
      /// <returns>The amount of free space available on the drive, in bytes.</returns>
      /// <remarks>This property indicates the amount of free space available on the drive. Note that this number may be different from the <see cref="TotalFreeSpace"/> number because this property takes into account disk quotas.</remarks>
      public long AvailableFreeSpace
      {
         get
         {
            GetDeviceInfo(3, 0);
            return null == _dsi ? 0 : _dsi.FreeBytesAvailable;
         }
      }

      /// <summary>Gets the name of the file system, such as NTFS or FAT32.</summary>
      /// <remarks>Use DriveFormat to determine what formatting a drive uses.</remarks>
      public string DriveFormat
      {
         get { return (string) GetDeviceInfo(0, 1); }
      }


      /// <summary>Gets the drive type.</summary>
      /// <returns>One of the <see cref="System.IO.DriveType"/> values.</returns>
      /// <remarks>
      /// The DriveType property indicates whether a drive is any of: CDRom, Fixed, Unknown, Network, NoRootDirectory,
      /// Ram, Removable, or Unknown. Values are listed in the <see cref="System.IO.DriveType"/> enumeration.
      /// </remarks>
      public DriveType DriveType
      {
         get { return (DriveType) GetDeviceInfo(2, 0); }
      }


      /// <summary>Gets a value indicating whether a drive is ready.</summary>
      /// <returns><c>true</c> if the drive is ready; otherwise, <c>false</c>.</returns>
      /// <remarks>
      /// IsReady indicates whether a drive is ready. For example, it indicates whether a CD is in a CD drive or whether
      /// a removable storage device is ready for read/write operations. If you do not test whether a drive is ready, and
      /// it is not ready, querying the drive using DriveInfo will raise an IOException.
      /// 
      /// Do not rely on IsReady() to avoid catching exceptions from other members such as TotalSize, TotalFreeSpace, and DriveFormat.
      /// Between the time that your code checks IsReady and then accesses one of the other properties
      /// (even if the access occurs immediately after the check), a drive may have been disconnected or a disk may have been removed.
      /// </remarks>
      public bool IsReady
      {
         get { return File.ExistsCore(null, true, Name, PathFormat.LongFullPath); }
      }


      /// <summary>Gets the name of the drive.</summary>
      /// <returns>The name of the drive.</returns>
      /// <remarks>This property is the name assigned to the drive, such as C:\ or E:\</remarks>
      public string Name
      {
         get { return _name; }
      }


      /// <summary>Gets the root directory of a drive.</summary>
      /// <returns>A DirectoryInfo object that contains the root directory of the drive.</returns>
      public DirectoryInfo RootDirectory
      {
         get { return (DirectoryInfo) GetDeviceInfo(2, 1); }
      }

      /// <summary>Gets the total amount of free space available on a drive.</summary>
      /// <returns>The total free space available on a drive, in bytes.</returns>
      /// <remarks>This property indicates the total amount of free space available on the drive, not just what is available to the current user.</remarks>
      public long TotalFreeSpace
      {
         get
         {
            GetDeviceInfo(3, 0);
            return null == _dsi ? 0 : _dsi.TotalNumberOfFreeBytes;
         }
      }


      /// <summary>Gets the total size of storage space on a drive.</summary>
      /// <returns>The total size of the drive, in bytes.</returns>
      /// <remarks>This property indicates the total size of the drive in bytes, not just what is available to the current user.</remarks>
      public long TotalSize
      {
         get
         {
            GetDeviceInfo(3, 0);
            return null == _dsi ? 0 : _dsi.TotalNumberOfBytes;
         }
      }


      /// <summary>Gets or sets the volume label of a drive.</summary>
      /// <returns>The volume label.</returns>
      /// <remarks>
      /// The label length is determined by the operating system. For example, NTFS allows a volume label
      /// to be up to 32 characters long. Note that <c>null</c> is a valid VolumeLabel.
      /// </remarks>
      public string VolumeLabel
      {
         get { return (string) GetDeviceInfo(0, 2); }
         set { Volume.SetVolumeLabel(Name, value); }
      }

      /// <summary>[AlphaFS] Returns the <see ref="Alphaleonis.Win32.Filesystem.DiskSpaceInfo"/> instance.</summary>
      public DiskSpaceInfo DiskSpaceInfo
      {
         get
         {
            GetDeviceInfo(3, 0);
            return _dsi;
         }
      }


      /// <summary>[AlphaFS] The MS-DOS device name.</summary>
      public string DosDeviceName
      {
         get { return (string) GetDeviceInfo(1, 0); }
      }


      /// <summary>[AlphaFS] Indicates if this drive is a SUBST.EXE / DefineDosDevice drive mapping.</summary>
      public bool IsDosDeviceSubstitute
      {
         get { return !Utils.IsNullOrWhiteSpace(DosDeviceName) && DosDeviceName.StartsWith(Path.NonInterpretedPathPrefix, StringComparison.OrdinalIgnoreCase); }
      }


      /// <summary>[AlphaFS] Indicates if this drive is a UNC path.</summary>
      public bool IsUnc
      {
         get
         {
            return !IsDosDeviceSubstitute && DriveType == DriveType.Network ||
               
                   // Handle Host devices with file systems: FAT/FAT32, UDF (CDRom), ...
                   Name.StartsWith(Path.UncPrefix, StringComparison.Ordinal) && DriveType == DriveType.NoRootDirectory && DriveFormat.Equals(DriveType.Unknown.ToString(), StringComparison.OrdinalIgnoreCase);
         }
      }


      /// <summary>[AlphaFS] Determines whether the specified volume name is a defined volume on the current computer.</summary>
      public bool IsVolume
      {
         get { return null != GetDeviceInfo(0, 0); }
      }


      /// <summary>[AlphaFS] Contains information about a file-system volume.</summary>
      /// <returns>A VolumeInfo object that contains file-system volume information of the drive.</returns>
      public VolumeInfo VolumeInfo
      {
         get { return (VolumeInfo) GetDeviceInfo(0, 0); }
      }


      #endregion // Properties


      #region Methods

      #region .NET

      /// <summary>Retrieves the <see cref="DriveInfo"/> of all logical drives on the Computer.</summary>
      /// <returns>An array of type <see cref="Alphaleonis.Win32.Filesystem.DriveInfo"/> that represents the logical drives on the Computer.</returns>
      [SecurityCritical]
      public static DriveInfo[] GetDrives()
      {
         return Directory.EnumerateLogicalDrivesCore(false, false).ToArray();
      }


      /// <summary>Returns a drive name as a string.</summary>
      /// <returns>The name of the drive.</returns>
      /// <remarks>This method returns the Name property.</remarks>
      public override string ToString()
      {
         return _name;
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Enumerates the drive names of all logical drives on the Computer.</summary>
      /// <param name="fromEnvironment">Retrieve logical drives as known by the Environment.</param>
      /// <param name="isReady">Retrieve only when accessible (IsReady) logical drives.</param>
      /// <returns>
      ///   An IEnumerable of type <see cref="Alphaleonis.Win32.Filesystem.DriveInfo"/> that represents
      ///   the logical drives on the Computer.
      /// </returns>      
      [SecurityCritical]
      public static IEnumerable<DriveInfo> EnumerateDrives(bool fromEnvironment, bool isReady)
      {
         return Directory.EnumerateLogicalDrivesCore(fromEnvironment, isReady);
      }


      /// <summary>[AlphaFS] Gets the first available drive letter on the local system.</summary>
      /// <returns>A drive letter as <see cref="char"/>. When no drive letters are available, an exception is thrown.</returns>
      /// <remarks>The letters "A" and "B" are reserved for floppy drives and will never be returned by this function.</remarks>
      public static char GetFreeDriveLetter()
      {
         return GetFreeDriveLetter(false);
      }


      /// <summary>Gets an available drive letter on the local system.</summary>
      /// <param name="getLastAvailable">When <c>true</c> get the last available drive letter. When <c>false</c> gets the first available drive letter.</param>
      /// <returns>A drive letter as <see cref="char"/>. When no drive letters are available, an exception is thrown.</returns>
      /// <remarks>The letters "A" and "B" are reserved for floppy drives and will never be returned by this function.</remarks>
      /// <exception cref="ArgumentOutOfRangeException">No drive letters available.</exception>
      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
      public static char GetFreeDriveLetter(bool getLastAvailable)
      {
         var freeDriveLetters = "CDEFGHIJKLMNOPQRSTUVWXYZ".Except(Directory.EnumerateLogicalDrivesCore(false, false).Select(d => d.Name[0]));

         try
         {
            return getLastAvailable ? freeDriveLetters.Last() : freeDriveLetters.First();
         }
         catch
         {
            throw new ArgumentOutOfRangeException(Resources.No_Drive_Letters_Available);
         }
      }

      #endregion // Methods


      #region Private Methods

      /// <summary>Retrieves information about the file system and volume associated with the specified root file or directorystream.</summary>
      [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      [SecurityCritical]
      private object GetDeviceInfo(int type, int mode)
      {
         try
         {
            switch (type)
            {
               #region Volume

               // VolumeInfo properties.
               case 0:
                  if (Utils.IsNullOrWhiteSpace(_volumeInfo.FullPath))
                     _volumeInfo.Refresh();

                  switch (mode)
                  {
                     case 0:
                        // IsVolume, VolumeInfo
                        return _volumeInfo;

                     case 1:
                        // DriveFormat
                        return null == _volumeInfo ? DriveType.Unknown.ToString() : _volumeInfo.FileSystemName ?? DriveType.Unknown.ToString();

                     case 2:
                        // VolumeLabel
                        return null == _volumeInfo ? string.Empty : _volumeInfo.Name ?? string.Empty;
                  }

                  break;


               // Volume related.
               case 1:
                  switch (mode)
                  {
                     case 0:
                        // DosDeviceName
                        return _dosDeviceName ?? (_dosDeviceName = Volume.GetVolumeDeviceName(Name));
                  }

                  break;

               #endregion // Volume


               #region Drive

               // Drive related.
               case 2:
                  switch (mode)
                  {
                     case 0:
                        // DriveType
                        return _driveType ?? (_driveType = Volume.GetDriveType(Name));

                     case 1:
                        // RootDirectory
                        return _rootDirectory ?? (_rootDirectory = new DirectoryInfo(null, Name, PathFormat.RelativePath));
                  }

                  break;

               // DiskSpaceInfo related.
               case 3:
                  switch (mode)
                  {
                     case 0:
                        // AvailableFreeSpace, TotalFreeSpace, TotalSize, DiskSpaceInfo
                        if (!_initDsie)
                        {
                           _dsi.Refresh();
                           _initDsie = true;
                        }

                        break;
                  }

                  break;

               #endregion // Drive
            }
         }
         catch
         {
         }

         return type == 0 && mode > 0 ? string.Empty : null;
      }
      
      #endregion // Private
   }
}
