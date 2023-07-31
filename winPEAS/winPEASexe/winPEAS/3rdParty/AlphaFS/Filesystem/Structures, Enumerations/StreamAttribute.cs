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
   /// <summary>Attributes of data to facilitate cross-operating system transfer. This member can be one or more of the following values.</summary>
   [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
   [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
   public enum StreamAttribute
   {
      /// <summary>This backup stream has no special attributes.</summary>
      None = NativeMethods.STREAM_ATTRIBUTE.NONE,

      /// <summary>Attribute set if the stream contains data that is modified when read. Allows the backup application to know that verification of data will fail.</summary>
      ModifiedWhenRead = NativeMethods.STREAM_ATTRIBUTE.STREAM_MODIFIED_WHEN_READ,

      /// <summary>The backup stream contains security information. This attribute applies only to backup stream of type <see cref="StreamId.BackupSecurityData"/>.</summary>
      ContainsSecurity = NativeMethods.STREAM_ATTRIBUTE.STREAM_CONTAINS_SECURITY,

      /// <summary>Reserved.</summary>
      ContainsProperties = NativeMethods.STREAM_ATTRIBUTE.STREAM_CONTAINS_PROPERTIES,

      /// <summary>The backup stream is part of a sparse file stream. This attribute applies only to backup stream of type <see cref="StreamId.BackupData"/>, <see cref="StreamId.BackupAlternateData"/>, and <see cref="StreamId.BackupSparseBlock"/>.</summary>
      SparseAttribute = NativeMethods.STREAM_ATTRIBUTE.STREAM_SPARSE_ATTRIBUTE
   }
}
