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
   /// <summary>Represents the encryption status of the specified file.</summary>
   public enum FileEncryptionStatus
   {
      /// <summary>The file can be encrypted.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Encryptable")]
      Encryptable = 0,

      /// <summary>The file is encrypted.</summary>
      Encrypted = 1,

      /// <summary>The file is a system file. System files cannot be encrypted.</summary>
      SystemFile = 2,

      /// <summary>The file is a root directory. Root directories cannot be encrypted.</summary>
      RootDirectory = 3,

      /// <summary>The file is a system directory. System directories cannot be encrypted.</summary>
      SystemDirectory = 4,

      /// <summary>The encryption status is unknown. The file may be encrypted.</summary>
      Unknown = 5,

      /// <summary>The file system does not support file encryption.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Filesystem")]
      NoFilesystemSupport = 6,

      /// <summary>Reserved for future use.</summary>
      UserDisallowed = 7,

      /// <summary>The file is a read-only file.</summary>
      ReadOnly = 8
   }
}