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

namespace Alphaleonis.Win32.Filesystem
{
   internal partial class NativeMethods
   {
      /// <summary>Indicates the operation to be performed when opening a file using the OpenEncryptedFileRaw.</summary>
      [Flags]
      internal enum EncryptedFileRawMode
      {
         /// <summary>(0) Open the file for export (backup).</summary>
         CreateForExport = 0,

         /// <summary>(1) The file is being opened for import (restore).</summary>
         CreateForImport = 1,

         /// <summary>(2) Import (restore) a directory containing encrypted files. This must be combined with one of the previous two flags to indicate the operation.</summary>
         CreateForDir = 2,

         /// <summary>(4) Overwrite a hidden file on import.</summary>
         OverwriteHidden = 4
      }
   }
}
