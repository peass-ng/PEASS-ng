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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Provides access to a file system object, using Shell32.</summary>
   public static class Shell32
   {
      /// <summary>Provides information for the IQueryAssociations interface methods, used by Shell32.</summary>
      [Flags]
      public enum AssociationAttributes
      {
         /// <summary>None.</summary>
         None = 0,

         /// <summary>Instructs not to map CLSID values to ProgID values.</summary>
         InitNoRemapClsid = 1,

         /// <summary>Identifies the value of the supplied file parameter (3rd parameter of function GetFileAssociation()) as an executable file name.</summary>
         /// <remarks>If this flag is not set, the root key will be set to the ProgID associated with the .exe key instead of the executable file's ProgID.</remarks>
         InitByExeName = 2,

         /// <summary>Specifies that when an IQueryAssociation method does not find the requested value under the root key, it should attempt to retrieve the comparable value from the * subkey.</summary>
         InitDefaultToStar = 4,

         /// <summary>Specifies that when an IQueryAssociation method does not find the requested value under the root key, it should attempt to retrieve the comparable value from the Folder subkey.</summary>
         InitDefaultToFolder = 8,

         /// <summary>Specifies that only HKEY_CLASSES_ROOT should be searched, and that HKEY_CURRENT_USER should be ignored.</summary>
         NoUserSettings = 16,

         /// <summary>Specifies that the return string should not be truncated. Instead, return an error value and the required size for the complete string.</summary>
         NoTruncate = 32,

         /// <summary>
         /// Instructs IQueryAssociations methods to verify that data is accurate.
         /// This setting allows IQueryAssociations methods to read data from the user's hard disk for verification.
         /// For example, they can check the friendly name in the registry against the one stored in the .exe file.
         /// </summary>
         /// <remarks>Setting this flag typically reduces the efficiency of the method.</remarks>
         Verify = 64,

         /// <summary>
         /// Instructs IQueryAssociations methods to ignore Rundll.exe and return information about its target.
         /// Typically IQueryAssociations methods return information about the first .exe or .dll in a command string.
         /// If a command uses Rundll.exe, setting this flag tells the method to ignore Rundll.exe and return information about its target.
         /// </summary>
         RemapRunDll = 128,

         /// <summary>Instructs IQueryAssociations methods not to fix errors in the registry, such as the friendly name of a function not matching the one found in the .exe file.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FixUps")]
         NoFixUps = 256,

         /// <summary>Specifies that the BaseClass value should be ignored.</summary>
         IgnoreBaseClass = 512,

         /// <summary>Specifies that the "Unknown" ProgID should be ignored; instead, fail.</summary>
         /// <remarks>Introduced in Windows 7.</remarks>
         InitIgnoreUnknown = 1024,

         /// <summary>Specifies that the supplied ProgID should be mapped using the system defaults, rather than the current user defaults.</summary>
         /// <remarks>Introduced in Windows 8.</remarks>
         InitFixedProgId = 2048,

         /// <summary>Specifies that the value is a protocol, and should be mapped using the current user defaults.</summary>
         /// <remarks>Introduced in Windows 8.</remarks>
         IsProtocol = 4096
      }


      //internal enum AssociationData
      //{
      //   MsiDescriptor = 1,
      //   NoActivateHandler = 2 ,
      //   QueryClassStore = 3,
      //   HasPerUserAssoc = 4,
      //   EditFlags = 5,
      //   Value = 6
      //}


      //internal enum AssociationKey
      //{
      //   ShellExecClass = 1,
      //   App = 2,
      //   Class = 3,
      //   BaseClass = 4
      //}


      /// <summary>ASSOCSTR enumeration - Used by the AssocQueryString() function to define the type of string that is to be returned.</summary>
      public enum AssociationString
      {
         /// <summary>None.</summary>
         None = 0,

         /// <summary>A command string associated with a Shell verb.</summary>
         Command = 1,

         /// <summary>
         /// An executable from a Shell verb command string.
         /// For example, this string is found as the (Default) value for a subkey such as HKEY_CLASSES_ROOT\ApplicationName\shell\Open\command.
         /// If the command uses Rundll.exe, set the <see cref="AssociationAttributes.RemapRunDll"/> flag in the attributes parameter of IQueryAssociations::GetString to retrieve the target executable.
         /// </summary>
         Executable = 2,

         /// <summary>The friendly name of a document type.</summary>
         FriendlyDocName = 3,

         /// <summary>The friendly name of an executable file.</summary>
         FriendlyAppName = 4,

         /// <summary>Ignore the information associated with the open subkey.</summary>
         NoOpen = 5,

         /// <summary>Look under the ShellNew subkey.</summary>
         ShellNewValue = 6,

         /// <summary>A template for DDE commands.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dde")]
         DdeCommand = 7,

         /// <summary>The DDE command to use to create a process.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dde")]
         DdeIfExec = 8,

         /// <summary>The application name in a DDE broadcast.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dde")]
         DdeApplication = 9,

         /// <summary>The topic name in a DDE broadcast.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dde")]
         DdeTopic = 10,

         /// <summary>
         /// Corresponds to the InfoTip registry value.
         /// Returns an info tip for an item, or list of properties in the form of an IPropertyDescriptionList from which to create an info tip, such as when hovering the cursor over a file name.
         /// The list of properties can be parsed with PSGetPropertyDescriptionListFromString.
         /// </summary>
         InfoTip = 11,

         /// <summary>
         /// Corresponds to the QuickTip registry value. This is the same as <see cref="InfoTip"/>, except that it always returns a list of property names in the form of an IPropertyDescriptionList.
         /// The difference between this value and <see cref="InfoTip"/> is that this returns properties that are safe for any scenario that causes slow property retrieval, such as offline or slow networks.
         /// Some of the properties returned from <see cref="InfoTip"/> might not be appropriate for slow property retrieval scenarios.
         /// The list of properties can be parsed with PSGetPropertyDescriptionListFromString.
         /// </summary>
         QuickTip = 12,

         /// <summary>
         /// Corresponds to the TileInfo registry value. Contains a list of properties to be displayed for a particular file type in a Windows Explorer window that is in tile view.
         /// This is the same as <see cref="InfoTip"/>, but, like <see cref="QuickTip"/>, it also returns a list of property names in the form of an IPropertyDescriptionList.
         /// The list of properties can be parsed with PSGetPropertyDescriptionListFromString.
         /// </summary>
         TileInfo = 13,

         /// <summary>
         /// Describes a general type of MIME file association, such as image and bmp,
         /// so that applications can make general assumptions about a specific file type.
         /// </summary>
         ContentType = 14,

         /// <summary>
         /// Returns the path to the icon resources to use by default for this association.
         /// Positive numbers indicate an index into the dll's resource table, while negative numbers indicate a resource ID.
         /// An example of the syntax for the resource is "c:\myfolder\myfile.dll,-1".
         /// </summary>
         DefaultIcon = 15,

         /// <summary>
         /// For an object that has a Shell extension associated with it,
         /// you can use this to retrieve the CLSID of that Shell extension object by passing a string representation
         /// of the IID of the interface you want to retrieve as the pwszExtra parameter of IQueryAssociations::GetString.
         /// For example, if you want to retrieve a handler that implements the IExtractImage interface,
         /// you would specify "{BB2E617C-0920-11d1-9A0B-00C04FC2D6C1}", which is the IID of IExtractImage.
         /// </summary>
         ShellExtension = 16,

         /// <summary>
         /// For a verb invoked through COM and the IDropTarget interface, you can use this flag to retrieve the IDropTarget object's CLSID.
         /// This CLSID is registered in the DropTarget subkey.
         /// The verb is specified in the supplied file parameter in the call to IQueryAssociations::GetString.
         /// </summary>
         DropTarget = 17,

         /// <summary>
         /// For a verb invoked through COM and the IExecuteCommand interface, you can use this flag to retrieve the IExecuteCommand object's CLSID.
         /// This CLSID is registered in the verb's command subkey as the DelegateExecute entry.
         /// The verb is specified in the supplied file parameter in the call to IQueryAssociations::GetString.
         /// </summary>
         DelegateExecute = 18,

         /// <summary>(No description available on MSDN)</summary>
         /// <remarks>Introduced in Windows 8.</remarks>
         SupportedUriProtocols = 19,

         /// <summary>The maximum defined <see cref="AssociationString"/> value, used for validation purposes.</summary>
         Max = 20
      }


      /// <summary>Shell32 FileAttributes structure, used to retrieve the different types of a file system object.</summary>
      [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
      [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
      [Flags]
      public enum FileAttributes
      {
         /// <summary>0x000000000 - Get file system object large icon.</summary>
         /// <remarks>The <see cref="Icon"/> flag must also be set.</remarks>
         LargeIcon = 0,

         /// <summary>0x000000001 - Get file system object small icon.</summary>
         /// <remarks>The <see cref="Icon"/> flag must also be set.</remarks>
         SmallIcon = 1,

         /// <summary>0x000000002 - Get file system object open icon.</summary>
         /// <remarks>A container object displays an open icon to indicate that the container is open.</remarks>
         /// <remarks>The <see cref="Icon"/> and/or <see cref="SysIconIndex"/> flag must also be set.</remarks>
         OpenIcon = 2,

         /// <summary>0x000000004 - Get file system object Shell-sized icon.</summary>
         /// <remarks>If this attribute is not specified the function sizes the icon according to the system metric values.</remarks>
         ShellIconSize = 4,

         /// <summary>0x000000008 - Get file system object by its PIDL.</summary>
         /// <remarks>Indicate that the given file contains the address of an ITEMIDLIST structure rather than a path name.</remarks>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pidl")]
         Pidl = 8,

         /// <summary>0x000000010 - Indicates that the given file should not be accessed. Rather, it should act as if the given file exists and use the supplied attributes.</summary>
         /// <remarks>This flag cannot be combined with the <see cref="Attributes"/>, <see cref="ExeType"/> or <see cref="Pidl"/> attributes.</remarks>
         UseFileAttributes = 16,

         /// <summary>0x000000020 - Apply the appropriate overlays to the file's icon.</summary>
         /// <remarks>The <see cref="Icon"/> flag must also be set.</remarks>
         AddOverlays = 32,

         /// <summary>0x000000040 - Returns the index of the overlay icon.</summary>
         /// <remarks>The value of the overlay index is returned in the upper eight bits of the iIcon member of the structure specified by psfi.</remarks>
         OverlayIndex = 64,

         /// <summary>0x000000100 - Retrieve the handle to the icon that represents the file and the index of the icon within the system image list. The handle is copied to the <see cref="FileInfo.IconHandle"/> member of the structure, and the index is copied to the <see cref="FileInfo.IconIndex"/> member.</summary>
         Icon = 256,

         /// <summary>0x000000200 - Retrieve the display name for the file. The name is copied to the <see cref="FileInfo.DisplayName"/> member of the structure.</summary>
         /// <remarks>The returned display name uses the long file name, if there is one, rather than the 8.3 form of the file name.</remarks>
         DisplayName = 512,

         /// <summary>0x000000400 - Retrieve the string that describes the file's type.</summary>
         TypeName = 1024,

         /// <summary>0x000000800 - Retrieve the item attributes. The attributes are copied to the <see cref="FileInfo.Attributes"/> member of the structure.</summary>
         /// <remarks>Will touch every file, degrading performance.</remarks>
         Attributes = 2048,

         /// <summary>0x000001000 - Retrieve the name of the file that contains the icon representing the file specified by pszPath. The name of the file containing the icon is copied to the <see cref="FileInfo.DisplayName"/> member of the structure.  The icon's index is copied to that structure's <see cref="FileInfo.IconIndex"/> member.</summary>
         IconLocation = 4096,

         /// <summary>0x000002000 - Retrieve the type of the executable file if pszPath identifies an executable file.</summary>
         /// <remarks>This flag cannot be specified with any other attributes.</remarks>
         ExeType = 8192,

         /// <summary>0x000004000 - Retrieve the index of a system image list icon.</summary>
         SysIconIndex = 16384,

         /// <summary>0x000008000 - Add the link overlay to the file's icon.</summary>
         /// <remarks>The <see cref="Icon"/> flag must also be set.</remarks>
         LinkOverlay = 32768,

         /// <summary>0x000010000 - Blend the file's icon with the system highlight color.</summary>
         Selected = 65536,

         /// <summary>0x000020000 - Modify <see cref="Attributes"/> to indicate that <see cref="FileInfo.Attributes"/> contains specific attributes that are desired.</summary>
         /// <remarks>This flag cannot be specified with the <see cref="Icon"/> attribute. Will touch every file, degrading performance.</remarks>
         AttributesSpecified = 131072
      }


      /// <summary>SHFILEINFO structure, contains information about a file system object.</summary>
      [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
      [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      public struct FileInfo
      {
         /// <summary>A handle to the icon that represents the file.</summary>
         /// <remarks>Caller is responsible for destroying this handle with DestroyIcon() when no longer needed.</remarks>
         [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
         public readonly IntPtr IconHandle;

         /// <summary>The index of the icon image within the system image list.</summary>
         [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
         public int IconIndex;

         /// <summary>An array of values that indicates the attributes of the file object.</summary>
         [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
         [MarshalAs(UnmanagedType.U4)]
         public readonly GetAttributesOf Attributes;

         /// <summary>The name of the file as it appears in the Windows Shell, or the path and file name of the file that contains the icon representing the file.</summary>
         [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.MaxPath)]
         public string DisplayName;

         /// <summary>The type of file.</summary>
         [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
         public string TypeName;
      }


      /// <summary>SFGAO - Attributes that can be retrieved from a file system object.</summary>      
      [SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags"), SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Sh")]
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sh")]
      [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
      [Flags]
      public enum GetAttributesOf
      {
         /// <summary>0x00000000 - None.</summary>
         None = 0,

         /// <summary>0x00000001 - The specified items can be copied.</summary>
         CanCopy = 1,

         /// <summary>0x00000002 - The specified items can be moved.</summary>
         CanMove = 2,

         /// <summary>0x00000004 - Shortcuts can be created for the specified items.</summary>
         CanLink = 4,

         /// <summary>0x00000008 - The specified items can be bound to an IStorage object through IShellFolder::BindToObject. For more information about namespace manipulation capabilities, see IStorage.</summary>
         Storage = 8,

         /// <summary>0x00000010 - The specified items can be renamed. Note that this value is essentially a suggestion; not all namespace clients allow items to be renamed. However, those that do must have this attribute set.</summary>
         CanRename = 16,

         /// <summary>0x00000020 - The specified items can be deleted.</summary>
         CanDelete = 32,

         /// <summary>0x00000040 - The specified items have property sheets.</summary>
         HasPropSheet = 64,

         /// <summary>0x00000100 - The specified items are drop targets.</summary>
         DropTarget = 256,

         /// <summary>0x00001000 - The specified items are system items.</summary>
         ///  <remarks>Windows 7 and later.</remarks>
         System = 4096,

         /// <summary>0x00002000 - The specified items are encrypted and might require special presentation.</summary>
         Encrypted = 8192,

         /// <summary>0x00004000 - Accessing the item (through IStream or other storage interfaces) is expected to be a slow operation.</summary>
         IsSlow = 16384,

         /// <summary>0x00008000 - The specified items are shown as dimmed and unavailable to the user.</summary>
         Ghosted = 32768,

         /// <summary>0x00010000 - The specified items are shortcuts.</summary>
         Link = 65536,

         /// <summary>0x00020000 - The specified objects are shared.</summary>
         Share = 131072,

         /// <summary>0x00040000 - The specified items are read-only. In the case of folders, this means that new items cannot be created in those folders.</summary>
         ReadOnly = 262144,

         /// <summary>0x00080000 - The item is hidden and should not be displayed unless the Show hidden files and folders option is enabled in Folder Settings.</summary>
         Hidden = 524288,

         /// <summary>0x00100000 - The items are nonenumerated items and should be hidden. They are not returned through an enumerator such as that created by the IShellFolder::EnumObjects method.</summary>
         NonEnumerated = 1048576,

         /// <summary>0x00200000 - The items contain new content, as defined by the particular application.</summary>
         NewContent = 2097152,

         /// <summary>0x00400000 - Indicates that the item has a stream associated with it.</summary>
         Stream = 4194304,

         /// <summary>0x00800000 - Children of this item are accessible through IStream or IStorage.</summary>
         StorageAncestor = 8388608,

         /// <summary>0x01000000 - When specified as input, instructs the folder to validate that the items contained in a folder or Shell item array exist.</summary>
         Validate = 16777216,

         /// <summary>0x02000000 - The specified items are on removable media or are themselves removable devices.</summary>
         Removable = 33554432,

         /// <summary>0x04000000 - The specified items are compressed.</summary>
         Compressed = 67108864,

         /// <summary>0x08000000 - The specified items can be hosted inside a web browser or Windows Explorer frame.</summary>
         Browsable = 134217728,

         /// <summary>0x10000000 - The specified folders are either file system folders or contain at least one descendant (child, grandchild, or later) that is a file system folder.</summary>
         FileSysAncestor = 268435456,

         /// <summary>0x20000000 - The specified items are folders.</summary>
         Folder = 536870912,

         /// <summary>0x40000000 - The specified folders or files are part of the file system (that is, they are files, directories, or root directories).</summary>
         FileSystem = 1073741824,

         /// <summary>0x80000000 - The specified folders have subfolders.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubFolder")]
         HasSubFolder = unchecked((int)0x80000000)
      }


      /// <summary>Used by method UrlIs() to define a URL type.</summary>
      public enum UrlType
      {
         /// <summary>Is the URL valid?</summary>
         IsUrl = 0,

         /// <summary>Is the URL opaque?</summary>
         IsOpaque = 1,

         /// <summary>Is the URL a URL that is not typically tracked in navigation history?</summary>
         IsNoHistory = 2,

         /// <summary>Is the URL a file URL?</summary>
         IsFileUrl = 3,

         /// <summary>Attempt to determine a valid scheme for the URL.</summary>
         [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Appliable")]
         IsAppliable = 4,

         /// <summary>Does the URL string end with a directory?</summary>
         IsDirectory = 5,

         /// <summary>Does the URL have an appended query string?</summary>
         IsHasQuery = 6
      }


      #region Methods

      /// <summary>Destroys an icon and frees any memory the icon occupied.</summary>
      /// <param name="iconHandle">An <see cref="IntPtr"/> handle to an icon.</param>
      public static void DestroyIcon(IntPtr iconHandle)
      {
         if (IntPtr.Zero != iconHandle)
            NativeMethods.DestroyIcon(iconHandle);
      }


      /// <summary>Gets the file or protocol that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related string from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileAssociation(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.Verify, AssociationString.Executable);
      }


      /// <summary>Gets the content-type that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related content-type from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileContentType(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.Verify, AssociationString.ContentType);
      }


      /// <summary>Gets the default icon that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related default icon from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileDefaultIcon(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.Verify, AssociationString.DefaultIcon);
      }


      /// <summary>Gets the friendly application name that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related friendly application name from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileFriendlyAppName(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.InitByExeName, AssociationString.FriendlyAppName);
      }


      /// <summary>Gets the friendly document name that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related friendly document name from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileFriendlyDocName(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.Verify, AssociationString.FriendlyDocName);
      }


      /// <summary>Gets an <see cref="IntPtr"/> handle to the Shell icon that represents the file.</summary>
      /// <remarks>Caller is responsible for destroying this handle with DestroyIcon() when no longer needed.</remarks>
      /// <param name="filePath">
      ///   The path to the file system object which should not exceed maximum path length. Both absolute and
      ///   relative paths are valid.
      /// </param>
      /// <param name="iconAttributes">
      ///   Icon size <see cref="Shell32.FileAttributes.SmallIcon"/> or <see cref="Shell32.FileAttributes.LargeIcon"/>. Can also be combined
      ///   with <see cref="Shell32.FileAttributes.AddOverlays"/> and others.
      /// </param>
      /// <returns>An <see cref="IntPtr"/> handle to the Shell icon that represents the file, or IntPtr.Zero on failure.</returns>
      [SecurityCritical]
      public static IntPtr GetFileIcon(string filePath, FileAttributes iconAttributes)
      {
         if (Utils.IsNullOrWhiteSpace(filePath))
            return IntPtr.Zero;

         var fileInfo = GetFileInfoCore(filePath, System.IO.FileAttributes.Normal, FileAttributes.Icon | iconAttributes, true, true);
         return fileInfo.IconHandle == IntPtr.Zero ? IntPtr.Zero : fileInfo.IconHandle;
      }


      /// <summary>Retrieves information about an object in the file system, such as a file, folder, directory, or drive root.</summary>
      /// <returns>A <see cref="FileInfo"/> struct instance.</returns>
      /// <remarks>
      /// <para>You should call this function from a background thread.</para>
      /// <para>Failure to do so could cause the UI to stop responding.</para>
      /// <para>Unicode path are supported.</para>
      /// </remarks>
      /// <param name="filePath">The path to the file system object which should not exceed the maximum path length. Both absolute and relative paths are valid.</param>
      /// <param name="attributes">A <see cref="System.IO.FileAttributes"/> attribute.</param>
      /// <param name="fileAttributes">One ore more <see cref="FileAttributes"/> attributes.</param>
      /// <param name="continueOnException">
      /// <para><c>true</c> suppress any Exception that might be thrown as a result from a failure,</para>
      /// <para>such as ACLs protected directories or non-accessible reparse points.</para>
      /// </param>
      [SecurityCritical]
      public static FileInfo GetFileInfo(string filePath, System.IO.FileAttributes attributes, FileAttributes fileAttributes, bool continueOnException)
      {
         return GetFileInfoCore(filePath, attributes, fileAttributes, true, continueOnException);
      }


      /// <summary>Retrieves an instance of <see cref="Shell32Info"/> containing information about the specified file.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>A <see cref="Shell32Info"/> class instance.</returns>
      [SecurityCritical]
      public static Shell32Info GetShell32Info(string path)
      {
         return new Shell32Info(path);
      }

      /// <summary>Retrieves an instance of <see cref="Shell32Info"/> containing information about the specified file.</summary>
      /// <param name="path">A path to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A <see cref="Shell32Info"/> class instance.</returns>
      [SecurityCritical]
      public static Shell32Info GetShell32Info(string path, PathFormat pathFormat)
      {
         return new Shell32Info(path, pathFormat);
      }


      /// <summary>Gets the "Open With" command that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related "Open With" command from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileOpenWithAppName(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.Verify, AssociationString.FriendlyAppName);
      }


      /// <summary>Gets the Shell command that is associated with <paramref name="path"/> from the registry.</summary>
      /// <param name="path">A path to the file.</param>
      /// <returns>The associated file- or protocol-related Shell command from the registry or <c>string.Empty</c> if no association can be found.</returns>
      [SecurityCritical]
      public static string GetFileVerbCommand(string path)
      {
         return GetFileAssociationCore(path, AssociationAttributes.Verify, AssociationString.Command);
      }


      /// <summary>Converts a file URL to a Microsoft MS-DOS path.</summary>
      /// <param name="urlPath">The file URL.</param>
      /// <returns>
      /// <para>The Microsoft MS-DOS path. If no path can be created, <c>string.Empty</c> is returned.</para>
      /// <para>If <paramref name="urlPath"/> is <c>null</c>, <c>null</c> will also be returned.</para>
      /// </returns>
      [SecurityCritical]
      internal static string PathCreateFromUrl(string urlPath)
      {
         if (urlPath == null)
            return null;

         var buffer = new StringBuilder(NativeMethods.MaxPathUnicode);
         var bufferSize = (uint)buffer.Capacity;

         var lastError = NativeMethods.PathCreateFromUrl(urlPath, buffer, ref bufferSize, 0);

         // Don't throw exception, but return string.Empty;
         return lastError == Win32Errors.S_OK ? buffer.ToString() : string.Empty;
      }


      /// <summary>Creates a path from a file URL.</summary>
      /// <returns>
      /// <para>The file path. If no path can be created, <c>string.Empty</c> is returned.</para>
      /// <para>If <paramref name="urlPath"/> is <c>null</c>, <c>null</c> will also be returned.</para>
      /// </returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="urlPath">The URL.</param>
      [SecurityCritical]
      internal static string PathCreateFromUrlAlloc(string urlPath)
      {
         if (!NativeMethods.IsAtLeastWindowsVista)
            throw new PlatformNotSupportedException(new Win32Exception((int)Win32Errors.ERROR_OLD_WIN_VERSION).Message);


         if (urlPath == null)
            return null;

         StringBuilder buffer;
         var lastError = NativeMethods.PathCreateFromUrlAlloc(urlPath, out buffer, 0);

         // Don't throw exception, but return string.Empty;
         return lastError == Win32Errors.S_OK ? buffer.ToString() : string.Empty;
      }


      /// <summary>Determines whether a path to a file system object such as a file or folder is valid.</summary>
      /// <param name="path">The full path of maximum length the maximum path length to the object to verify.</param>
      /// <returns><c>true</c> if the file exists; <c>false</c> otherwise</returns>
      [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "lastError")]
      [SecurityCritical]
      public static bool PathFileExists(string path)
      {
         // PathFileExists()
         // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

         return !Utils.IsNullOrWhiteSpace(path) && NativeMethods.PathFileExists(Path.GetFullPathCore(null, false, path, GetFullPathOptions.AsLongPath | GetFullPathOptions.FullCheck | GetFullPathOptions.ContinueOnNonExist));
      }


      /// <summary>Tests whether a URL is a specified type.</summary>
      /// <param name="url">The URL.</param>
      /// <param name="urlType"></param>
      /// <returns>
      /// For all but one of the URL types, UrlIs returns <c>true</c> if the URL is the specified type, or <c>false</c> otherwise.
      /// If UrlIs is set to <see cref="UrlType.IsAppliable"/>, UrlIs will attempt to determine the URL scheme.
      /// If the function is able to determine a scheme, it returns <c>true</c>, or <c>false</c> otherwise.
      /// </returns>
      [SecurityCritical]
      internal static bool UrlIs(string url, UrlType urlType)
      {
         return NativeMethods.UrlIs(url, urlType);
      }


      /// <summary>Converts a Microsoft MS-DOS path to a canonicalized URL.</summary>
      /// <param name="path">The full MS-DOS path of maximum length <see cref="NativeMethods.MaxPath"/>.</param>
      /// <returns>
      /// <para>The URL. If no URL can be created <c>string.Empty</c> is returned.</para>
      /// <para>If <paramref name="path"/> is <c>null</c>, <c>null</c> will also be returned.</para>
      /// </returns>
      [SecurityCritical]
      internal static string UrlCreateFromPath(string path)
      {
         if (path == null)
            return null;

         // UrlCreateFromPath does not support extended paths.
         var pathRp = Path.GetRegularPathCore(path, GetFullPathOptions.CheckInvalidPathChars, false);

         var buffer = new StringBuilder(NativeMethods.MaxPathUnicode);
         var bufferSize = (uint)buffer.Capacity;

         var lastError = NativeMethods.UrlCreateFromPath(pathRp, buffer, ref bufferSize, 0);

         // Don't throw exception, but return null;
         var url = buffer.ToString();
         if (Utils.IsNullOrWhiteSpace(url))
            url = string.Empty;

         return lastError == Win32Errors.S_OK ? url : string.Empty;
      }


      /// <summary>Tests a URL to determine if it is a file URL.</summary>
      /// <param name="url">The URL.</param>
      /// <returns><c>true</c> if the URL is a file URL, or <c>false</c> otherwise.</returns>
      [SecurityCritical]
      internal static bool UrlIsFileUrl(string url)
      {
         return NativeMethods.UrlIs(url, UrlType.IsFileUrl);
      }


      /// <summary>Returns whether a URL is a URL that browsers typically do not include in navigation history.</summary>
      /// <param name="url">The URL.</param>
      /// <returns><c>true</c> if the URL is a URL that is not included in navigation history, or <c>false</c> otherwise.</returns>
      [SecurityCritical]
      internal static bool UrlIsNoHistory(string url)
      {
         return NativeMethods.UrlIs(url, UrlType.IsNoHistory);
      }


      /// <summary>Returns whether a URL is opaque.</summary>
      /// <param name="url">The URL.</param>
      /// <returns><c>true</c> if the URL is opaque, or <c>false</c> otherwise.</returns>
      [SecurityCritical]
      internal static bool UrlIsOpaque(string url)
      {
         return NativeMethods.UrlIs(url, UrlType.IsOpaque);
      }


      #region Internal Methods

      /// <summary>Searches for and retrieves a file or protocol association-related string from the registry.</summary>
      /// <param name="path">A path to a file.</param>
      /// <param name="attributes">One or more <see cref="AssociationAttributes"/> attributes. Only one "InitXXX" attribute can be used.</param>
      /// <param name="associationType">A <see cref="AssociationString"/> attribute.</param>
      /// <returns>The associated file- or protocol-related string from the registry or <c>string.Empty</c> if no association can be found.</returns>
      /// <exception cref="ArgumentNullException"/>
      [SecurityCritical]
      private static string GetFileAssociationCore(string path, AssociationAttributes attributes, AssociationString associationType)
      {
         if (Utils.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException("path");

         attributes = attributes | AssociationAttributes.NoTruncate | AssociationAttributes.RemapRunDll;

         uint bufferSize = NativeMethods.MaxPath;
         StringBuilder buffer;
         uint retVal;

         do
         {
            buffer = new StringBuilder((int)bufferSize);

            // AssocQueryString()
            // 2014-02-05: MSDN does not confirm LongPath usage but a Unicode version of this function exists.
            // 2015-07-17: This function does not support long paths.

            retVal = NativeMethods.AssocQueryString(attributes, associationType, path, null, buffer, out bufferSize);

            // No Exception is thrown, just return empty string on error.

            //switch (retVal)
            //{
            //   // 0x80070483: No application is associated with the specified file for this operation.
            //   case 2147943555:
            //   case Win32Errors.E_POINTER:
            //   case Win32Errors.S_OK:
            //      break;

            //   default:
            //      NativeError.ThrowException(retVal);
            //      break;
            //}

         } while (retVal == Win32Errors.E_POINTER);

         return buffer.ToString();
      }


      /// <summary>Retrieve information about an object in the file system, such as a file, folder, directory, or drive root.</summary>
      /// <returns>A <see cref="FileInfo"/> struct instance.</returns>
      /// <remarks>
      /// <para>You should call this function from a background thread.</para>
      /// <para>Failure to do so could cause the UI to stop responding.</para>
      /// <para>Unicode path are not supported.</para>
      /// </remarks>
      /// <param name="path">The path to the file system object which should not exceed the maximum path length in length. Both absolute and relative paths are valid.</param>
      /// <param name="attributes">A <see cref="System.IO.FileAttributes"/> attribute.</param>
      /// <param name="fileAttributes">A <see cref="FileAttributes"/> attribute.</param>
      /// <param name="checkInvalidPathChars">Checks that the path contains only valid path-characters.</param>
      /// <param name="continueOnException">
      /// <para><c>true</c> suppress any Exception that might be thrown as a result from a failure,</para>
      /// <para>such as ACLs protected directories or non-accessible reparse points.</para>
      /// </param>
      [SecurityCritical]
      internal static FileInfo GetFileInfoCore(string path, System.IO.FileAttributes attributes, FileAttributes fileAttributes, bool checkInvalidPathChars, bool continueOnException)
      {
         // Prevent possible crash.
         var fileInfo = new FileInfo
         {
            DisplayName = string.Empty,
            TypeName = string.Empty,
            IconIndex = 0
         };

         if (!Utils.IsNullOrWhiteSpace(path))
         {
            // ShGetFileInfo()
            // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.
            // 2015-07-17: This function does not support long paths.

            var shGetFileInfo = NativeMethods.ShGetFileInfo(Path.GetRegularPathCore(path, checkInvalidPathChars ? GetFullPathOptions.CheckInvalidPathChars : 0, false), attributes, out fileInfo, (uint)Marshal.SizeOf(fileInfo), fileAttributes);

            if (shGetFileInfo == IntPtr.Zero && !continueOnException)
               NativeError.ThrowException(Marshal.GetLastWin32Error(), path);
         }

         return fileInfo;
      }

      #endregion // Internal Methods

      #endregion // Methods
   }
}
