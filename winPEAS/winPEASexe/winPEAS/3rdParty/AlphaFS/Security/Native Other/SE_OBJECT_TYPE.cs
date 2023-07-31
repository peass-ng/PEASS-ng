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

namespace Alphaleonis.Win32.Security
{
   /// <summary>The SE_OBJECT_TYPE enumeration contains values that correspond to the types of Windows objects that support security.
   /// The functions, such as GetSecurityInfo and SetSecurityInfo, that set and retrieve the security information of an object, use these values to indicate the type of object.
   /// </summary>
   /// <remarks>
   /// <para>Minimum supported client: Windows XP [desktop apps only]</para>
   /// <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
   /// </remarks>
   internal enum SE_OBJECT_TYPE
   {
      /// <summary>Unknown object type.</summary>
      SE_UNKNOWN_OBJECT_TYPE = 0,

      /// <summary>Indicates a file or directory. The name string that identifies a file or directory object can be in one of the following formats:
      ///   A relative path, such as FileName.dat or ..\FileName
      ///   An absolute path, such as FileName.dat, C:\DirectoryName\FileName.dat, or G:\RemoteDirectoryName\FileName.dat.
      ///   A UNC name, such as \\ComputerName\ShareName\FileName.dat.
      /// </summary>
      SE_FILE_OBJECT,

      /// <summary>Indicates a Windows service. A service object can be a local service, such as ServiceName, or a remote service, such as \\ComputerName\ServiceName.</summary>
      SE_SERVICE,

      /// <summary>Indicates a printer. A printer object can be a local printer, such as PrinterName, or a remote printer, such as \\ComputerName\PrinterName.</summary>
      SE_PRINTER,

      /// <summary>Indicates a registry key. A registry key object can be in the local registry, such as CLASSES_ROOT\SomePath or in a remote registry, such as \\ComputerName\CLASSES_ROOT\SomePath.
      /// The names of registry keys must use the following literal strings to identify the predefined registry keys: "CLASSES_ROOT", "CURRENT_USER", "MACHINE", and "USERS".
      /// </summary>
      SE_REGISTRY_KEY,

      /// <summary>Indicates a network share. A share object can be local, such as ShareName, or remote, such as \\ComputerName\ShareName.</summary>
      SE_LMSHARE,

      /// <summary>Indicates a local kernel object. The GetSecurityInfo and SetSecurityInfo functions support all types of kernel objects.
      /// The GetNamedSecurityInfo and SetNamedSecurityInfo functions work only with the following kernel objects: semaphore, event, mutex, waitable timer, and file mapping.</summary>
      SE_KERNEL_OBJECT,

      /// <summary>Indicates a window station or desktop object on the local computer. You cannot use GetNamedSecurityInfo and SetNamedSecurityInfo with these objects because the names of window stations or desktops are not unique.</summary>
      SE_WINDOW_OBJECT,

      /// <summary>Indicates a directory service object or a property set or property of a directory service object.
      /// The name string for a directory service object must be in X.500 form, for example: CN=SomeObject,OU=ou2,OU=ou1,DC=DomainName,DC=CompanyName,DC=com,O=internet</summary>
      SE_DS_OBJECT,

      /// <summary>Indicates a directory service object and all of its property sets and properties.</summary>
      SE_DS_OBJECT_ALL,

      /// <summary>Indicates a provider-defined object.</summary>
      SE_PROVIDER_DEFINED_OBJECT,

      /// <summary>Indicates a WMI object.</summary>
      SE_WMIGUID_OBJECT,

      /// <summary>Indicates an object for a registry entry under WOW64.</summary>
      SE_REGISTRY_WOW64_32KEY
   }
}
