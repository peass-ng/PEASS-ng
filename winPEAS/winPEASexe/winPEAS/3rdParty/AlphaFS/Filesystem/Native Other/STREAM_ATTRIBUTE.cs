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

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>WIN32_STREAM_ID structure attributes of data to facilitate cross-operating system transfer. This member can be one or more of the following values.</summary>
      internal enum STREAM_ATTRIBUTE
      {
         /// <summary>This backup stream has no special attributes.</summary>
         NONE = 0,

         /// <summary>Attribute set if the stream contains data that is modified when read. Allows the backup application to know that verification of data will fail.</summary>
         STREAM_MODIFIED_WHEN_READ = 1,

         /// <summary>The backup stream contains security information. This attribute applies only to backup stream of type <see cref="STREAM_ID.BACKUP_SECURITY_DATA"/>.</summary>
         STREAM_CONTAINS_SECURITY = 2,

         /// <summary>Reserved.</summary>
         STREAM_CONTAINS_PROPERTIES = 4,

         /// <summary>The backup stream is part of a sparse file stream. This attribute applies only to backup stream of type <see cref="STREAM_ID.BACKUP_DATA"/>, <see cref="STREAM_ID.BACKUP_ALTERNATE_DATA"/>, and <see cref="STREAM_ID.BACKUP_SPARSE_BLOCK"/>.</summary>
         STREAM_SPARSE_ATTRIBUTE = 8
      }
   }
}
