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

using Alphaleonis.Win32.Security;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text;
using winPEAS._3rdParty.AlphaFS;
using SecurityNativeMethods = Alphaleonis.Win32.Security.NativeMethods;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>The <see cref="BackupFileStream"/> provides access to data associated with a specific file or directory, including security information and alternative data streams, for backup and restore operations.</summary>
   /// <remarks>This class uses the <see href="http://msdn.microsoft.com/en-us/library/aa362509(VS.85).aspx">BackupRead</see>, 
   /// <see href="http://msdn.microsoft.com/en-us/library/aa362510(VS.85).aspx">BackupSeek</see> and 
   /// <see href="http://msdn.microsoft.com/en-us/library/aa362511(VS.85).aspx">BackupWrite</see> functions from the Win32 API to provide access to the file or directory.
   /// </remarks>
   public sealed class BackupFileStream : Stream
   {
      private readonly bool _canRead;
      private readonly bool _canWrite;
      private readonly bool _processSecurity;

      [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
      private IntPtr _context = IntPtr.Zero;




      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path and creation mode.</summary>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <remarks>The file will be opened for exclusive access for both reading and writing.</remarks>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(string path, FileMode mode) : this(File.CreateFileCore(null, false, path, ExtendedFileAttributes.Normal, null, mode, FileSystemRights.Read | FileSystemRights.Write, FileShare.None, true, false, PathFormat.RelativePath), FileSystemRights.Read | FileSystemRights.Write)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode and access rights.</summary>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <remarks>The file will be opened for exclusive access.</remarks>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(string path, FileMode mode, FileSystemRights access) : this(File.CreateFileCore(null, false, path, ExtendedFileAttributes.Normal, null, mode, access, FileShare.None, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode, access rights and sharing permission.</summary>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <param name="share">A <see cref="FileShare"/> constant that determines how the file will be shared by processes.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(string path, FileMode mode, FileSystemRights access, FileShare share) : this(File.CreateFileCore(null, false, path, ExtendedFileAttributes.Normal, null, mode, access, share, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode, access rights and sharing permission, and additional file attributes.</summary>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <param name="share">A <see cref="FileShare"/> constant that determines how the file will be shared by processes.</param>
      /// <param name="attributes">A <see cref="ExtendedFileAttributes"/> constant that specifies additional file attributes.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(string path, FileMode mode, FileSystemRights access, FileShare share, ExtendedFileAttributes attributes) : this(File.CreateFileCore(null, false, path, attributes, null, mode, access, share, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode, access rights and sharing permission, additional file attributes, access control and audit security.</summary>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <param name="share">A <see cref="FileShare"/> constant that determines how the file will be shared by processes.</param>
      /// <param name="attributes">A <see cref="ExtendedFileAttributes"/> constant that specifies additional file attributes.</param>
      /// <param name="security">A <see cref="FileSecurity"/> constant that determines the access control and audit security for the file. This parameter This parameter may be <c>null</c>.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(string path, FileMode mode, FileSystemRights access, FileShare share, ExtendedFileAttributes attributes, FileSecurity security) : this(File.CreateFileCore(null, false, path, attributes, security, mode, access, share, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path and creation mode.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <remarks>The file will be opened for exclusive access for both reading and writing.</remarks>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(KernelTransaction transaction, string path, FileMode mode) : this(File.CreateFileCore(transaction, false, path, ExtendedFileAttributes.Normal, null, mode, FileSystemRights.Read | FileSystemRights.Write, FileShare.None, true, false, PathFormat.RelativePath), FileSystemRights.Read | FileSystemRights.Write)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode and access rights.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <remarks>The file will be opened for exclusive access.</remarks>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(KernelTransaction transaction, string path, FileMode mode, FileSystemRights access) : this(File.CreateFileCore(transaction, false, path, ExtendedFileAttributes.Normal, null, mode, access, FileShare.None, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode, access rights and sharing permission.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <param name="share">A <see cref="FileShare"/> constant that determines how the file will be shared by processes.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(KernelTransaction transaction, string path, FileMode mode, FileSystemRights access, FileShare share) : this(File.CreateFileCore(transaction, false, path, ExtendedFileAttributes.Normal, null, mode, access, share, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode, access rights and sharing permission, and additional file attributes.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <param name="share">A <see cref="FileShare"/> constant that determines how the file will be shared by processes.</param>
      /// <param name="attributes">A <see cref="ExtendedFileAttributes"/> constant that specifies additional file attributes.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(KernelTransaction transaction, string path, FileMode mode, FileSystemRights access, FileShare share, ExtendedFileAttributes attributes) : this(File.CreateFileCore(transaction, false, path, attributes, null, mode, access, share, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class with the specified path, creation mode, access rights and sharing permission, additional file attributes, access control and audit security.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">A relative or absolute path for the file that the current <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="mode">A <see cref="FileMode"/> constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file.</param>
      /// <param name="share">A <see cref="FileShare"/> constant that determines how the file will be shared by processes.</param>
      /// <param name="attributes">A <see cref="ExtendedFileAttributes"/> constant that specifies additional file attributes.</param>
      /// <param name="security">A <see cref="FileSecurity"/> constant that determines the access control and audit security for the file. This parameter This parameter may be <c>null</c>.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public BackupFileStream(KernelTransaction transaction, string path, FileMode mode, FileSystemRights access, FileShare share, ExtendedFileAttributes attributes, FileSecurity security) : this(File.CreateFileCore(transaction, false, path, attributes, security, mode, access, share, true, false, PathFormat.RelativePath), access)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="BackupFileStream"/> class for the specified file handle, with the specified read/write permission.</summary>
      /// <param name="handle">A file handle for the file that this <see cref="BackupFileStream"/> object will encapsulate.</param>
      /// <param name="access">A <see cref="FileSystemRights"/> constant that gets the <see cref="CanRead"/> and <see cref="CanWrite"/> properties of the <see cref="BackupFileStream"/> object.</param>
      [SecurityCritical]
      public BackupFileStream(SafeFileHandle handle, FileSystemRights access)
      {
         NativeMethods.IsValidHandle(handle);

         SafeFileHandle = handle;

         _canRead = (access & FileSystemRights.ReadData) != 0;
         _canWrite = (access & FileSystemRights.WriteData) != 0;
         _processSecurity = true;
      }


      /// <summary>When overridden in a derived class, gets the length in bytes of the stream.</summary>
      /// <value>This method always throws an exception.</value>
      /// <exception cref="NotSupportedException"/>
      public override long Length
      {
         get { throw new NotSupportedException(Resources.No_Stream_Seeking_Support); }
      }


      /// <summary>When overridden in a derived class, gets or sets the position within the current stream.</summary>
      /// <value>This method always throws an exception.</value>
      /// <exception cref="NotSupportedException"/>
      public override long Position
      {
         get { throw new NotSupportedException(Resources.No_Stream_Seeking_Support); }
         set { throw new NotSupportedException(Resources.No_Stream_Seeking_Support); }
      }


      /// <summary>When overridden in a derived class, sets the position within the current stream.</summary>
      /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
      /// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
      /// <returns>The new position within the current stream.</returns>
      /// <remarks><para><note><para>This stream does not support seeking using this method, and calling this method will always throw <see cref="NotSupportedException"/>. See <see cref="Skip"/> for an alternative way of seeking forward.</para></note></para></remarks>
      /// <exception cref="NotSupportedException"/>
      public override long Seek(long offset, SeekOrigin origin)
      {
         throw new NotSupportedException(Resources.No_Stream_Seeking_Support);
      }


      /// <summary>When overridden in a derived class, sets the length of the current stream.</summary>
      /// <param name="value">The desired length of the current stream in bytes.</param>
      /// <remarks>This method is not supported by the <see cref="BackupFileStream"/> class, and calling it will always generate a <see cref="NotSupportedException"/>.</remarks>
      /// <exception cref="NotSupportedException"/>
      public override void SetLength(long value)
      {
         throw new NotSupportedException(Resources.No_Stream_Seeking_Support);
      }
      
      


      /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
      /// <returns><c>true</c> if the stream supports reading, <c>false</c> otherwise.</returns>
      public override bool CanRead
      {
         get { return _canRead; }
      }

      /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>        
      /// <returns>This method always returns <c>false</c>.</returns>
      public override bool CanSeek
      {
         get { return false; }
      }

      /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
      /// <returns><c>true</c> if the stream supports writing, <c>false</c> otherwise.</returns>
      public override bool CanWrite
      {
         get { return _canWrite; }
      }

      /// <summary>Gets a <see cref="SafeFileHandle"/> object that represents the operating system file handle for the file that the current <see cref="BackupFileStream"/> object encapsulates.</summary>
      /// <value>A <see cref="SafeFileHandle"/> object that represents the operating system file handle for the file that 
      /// the current <see cref="BackupFileStream"/> object encapsulates.</value>
      private SafeFileHandle SafeFileHandle { get; set; }




      /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
      /// <remarks>This method will not backup the access-control list (ACL) data for the file or directory.</remarks>
      /// <param name="buffer">
      ///   An array of bytes. When this method returns, the buffer contains the specified byte array with the values between
      ///   <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the
      ///   current source.
      /// </param>
      /// <param name="offset">
      ///   The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.
      /// </param>
      /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
      /// <returns>
      ///   The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not
      ///   currently available, or zero (0) if the end of the stream has been reached.
      /// </returns>
      ///
      /// <exception cref="System.ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="System.ArgumentOutOfRangeException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ObjectDisposedException"/>
      public override int Read(byte[] buffer, int offset, int count)
      {
         return Read(buffer, offset, count, false);
      }


      /// <summary>When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
      /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values
      /// between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
      /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
      /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
      /// <param name="processSecurity">Indicates whether the function will backup the access-control list (ACL) data for the file or directory.</param>
      /// <returns>
      /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not
      /// currently available, or zero (0) if the end of the stream has been reached.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ObjectDisposedException"/>
      [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
      [SecurityCritical]
      public int Read(byte[] buffer, int offset, int count, bool processSecurity)
      {
         if (buffer == null)
            throw new ArgumentNullException("buffer");

         if (!CanRead)
            throw new NotSupportedException("Stream does not support reading");

         if (offset + count > buffer.Length)
            throw new ArgumentException("The sum of offset and count is larger than the size of the buffer.", "offset");

         if (offset < 0)
            throw new ArgumentOutOfRangeException("offset", offset, Resources.Negative_Offset);

         if (count < 0)
            throw new ArgumentOutOfRangeException("count", count, Resources.Negative_Count);


         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(count))
         {
            uint numberOfBytesRead;

            var success = NativeMethods.BackupRead(SafeFileHandle, safeBuffer, (uint) safeBuffer.Capacity, out numberOfBytesRead, false, processSecurity, ref _context);
            
            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError);


            // See File.GetAccessControlCore(): .CopyTo() does not work there?
            // 2017-06-13: Is .CopyTo() doing anything useful here?
            safeBuffer.CopyTo(buffer, offset, count);

            return (int) numberOfBytesRead;
         }
      }


      /// <summary>Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
      /// <overloads>
      /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
      /// </overloads>
      /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
      /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
      /// <param name="count">The number of bytes to be written to the current stream.</param>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="System.ArgumentNullException"/>
      /// <exception cref="System.ArgumentOutOfRangeException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ObjectDisposedException"/>
      /// <remarks>This method will not process the access-control list (ACL) data for the file or directory.</remarks>      
      public override void Write(byte[] buffer, int offset, int count)
      {
         Write(buffer, offset, count, false);
      }


      /// <summary>When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
      /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
      /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
      /// <param name="count">The number of bytes to be written to the current stream.</param>
      /// <param name="processSecurity">Specifies whether the function will restore the access-control list (ACL) data for the file or directory. 
      /// If this is <c>true</c> you need to specify <see cref="FileSystemRights.TakeOwnership"/> and <see cref="FileSystemRights.ChangePermissions"/> access when 
      /// opening the file or directory handle. If the handle does not have those access rights, the operating system denies 
      /// access to the ACL data, and ACL data restoration will not occur.</param>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="System.ArgumentOutOfRangeException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="ObjectDisposedException"/>
      [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
      [SecurityCritical]
      public void Write(byte[] buffer, int offset, int count, bool processSecurity)
      {
         if (buffer == null)
            throw new ArgumentNullException("buffer");

         if (offset < 0)
            throw new ArgumentOutOfRangeException("offset", offset, Resources.Negative_Offset);

         if (count < 0)
            throw new ArgumentOutOfRangeException("count", count, Resources.Negative_Count);

         if (offset + count > buffer.Length)
            throw new ArgumentException(Resources.Buffer_Not_Large_Enough, "offset");


         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(count))
         {
            safeBuffer.CopyFrom(buffer, offset, count);

            uint bytesWritten;

            var success = NativeMethods.BackupWrite(SafeFileHandle, safeBuffer, (uint)safeBuffer.Capacity, out bytesWritten, false, processSecurity, ref _context);
            
            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError);
         }
      }


      /// <summary>Clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
      public override void Flush()
      {
         var success = NativeMethods.FlushFileBuffers(SafeFileHandle);

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            NativeError.ThrowException(lastError);
      }


      /// <summary>Skips ahead the specified number of bytes from the current stream.</summary>
      /// <remarks><para>This method represents the Win32 API implementation of <see href="http://msdn.microsoft.com/en-us/library/aa362509(VS.85).aspx">BackupSeek</see>.</para>
      /// <para>
      /// Applications use the <see cref="Skip"/> method to skip portions of a data stream that cause errors. This function does not 
      /// seek across stream headers. For example, this function cannot be used to skip the stream name. If an application 
      /// attempts to seek past the end of a substream, the function fails, the return value indicates the actual number of bytes 
      /// the function seeks, and the file position is placed at the start of the next stream header.
      /// </para>
      /// </remarks>
      /// <param name="bytes">The number of bytes to skip.</param>
      /// <returns>The number of bytes actually skipped.</returns>
      [SecurityCritical]
      public long Skip(long bytes)
      {
         uint lowSought, highSought;

         var success = NativeMethods.BackupSeek(SafeFileHandle, NativeMethods.GetLowOrderDword(bytes), NativeMethods.GetHighOrderDword(bytes), out lowSought, out highSought, ref _context);

         var lastError = Marshal.GetLastWin32Error();
         if (!success && lastError != Win32Errors.ERROR_SEEK)
         {
            // Error Code 25 indicates a seek error, we just skip that here.
            NativeError.ThrowException(lastError);
         }

         return NativeMethods.ToLong(highSought, lowSought);
      }


      /// <summary>Gets a <see cref="FileSecurity"/> object that encapsulates the access control list (ACL) entries for the file described by the current <see cref="BackupFileStream"/> object.</summary>
      /// <exception cref="IOException"/>
      /// <returns>
      ///   A <see cref="FileSecurity"/> object that encapsulates the access control list (ACL) entries for the file described by the current
      ///   <see cref="BackupFileStream"/> object.
      /// </returns>
      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
      [SecurityCritical]
      public FileSecurity GetAccessControl()
      {
         IntPtr pSidOwner, pSidGroup, pDacl, pSacl;
         SafeGlobalMemoryBufferHandle pSecurityDescriptor;

         var lastError = (int) SecurityNativeMethods.GetSecurityInfo(SafeFileHandle, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION | SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION | SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.SACL_SECURITY_INFORMATION, out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);

         try
         {
            if (lastError != Win32Errors.ERROR_SUCCESS)
               NativeError.ThrowException(lastError);

            if (null != pSecurityDescriptor && pSecurityDescriptor.IsInvalid)
            {
               pSecurityDescriptor.Close();
               pSecurityDescriptor = null;

               throw new IOException(new Win32Exception((int) Win32Errors.ERROR_INVALID_SECURITY_DESCR).Message);
            }


            var length = SecurityNativeMethods.GetSecurityDescriptorLength(pSecurityDescriptor);
            var managedBuffer = new byte[length];

            
            // .CopyTo() does not work there?
            if (null != pSecurityDescriptor)
               pSecurityDescriptor.CopyTo(managedBuffer, 0, (int) length);


            var fs = new FileSecurity();
            fs.SetSecurityDescriptorBinaryForm(managedBuffer);

            return fs;
         }
         finally
         {
            if (null != pSecurityDescriptor)
               pSecurityDescriptor.Close();
         }
      }


      /// <summary>Applies access control list (ACL) entries described by a <see cref="FileSecurity"/> object to the file described by the current <see cref="BackupFileStream"/> object.</summary>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an ACL entry to apply to the current file.</param>
      [SecurityCritical]
      public void SetAccessControl(ObjectSecurity fileSecurity)
      {
         File.SetAccessControlCore(null, SafeFileHandle, fileSecurity, AccessControlSections.All, PathFormat.LongFullPath);
      }


      /// <summary>Prevents other processes from changing the <see cref="BackupFileStream"/> while permitting read access.</summary>
      /// <param name="position">The beginning of the range to lock. The value of this parameter must be equal to or greater than zero (0).</param>
      /// <param name="length">The range to be locked.</param>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="ObjectDisposedException"/>
      [SecurityCritical]
      public void Lock(long position, long length)
      {
         if (position < 0)
            throw new ArgumentOutOfRangeException("position", position, new Win32Exception((int) Win32Errors.ERROR_NEGATIVE_SEEK).Message);

         if (length < 0)
            throw new ArgumentOutOfRangeException("length", length, Resources.Negative_Lock_Length);


         var success = NativeMethods.LockFile(SafeFileHandle, NativeMethods.GetLowOrderDword(position), NativeMethods.GetHighOrderDword(position), NativeMethods.GetLowOrderDword(length), NativeMethods.GetHighOrderDword(length));

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            NativeError.ThrowException(lastError);
      }


      /// <summary>Allows access by other processes to all or part of a file that was previously locked.</summary>
      /// <param name="position">The beginning of the range to unlock.</param>
      /// <param name="length">The range to be unlocked.</param>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="ObjectDisposedException"/>
      [SecurityCritical]
      public void Unlock(long position, long length)
      {
         if (position < 0)
            throw new ArgumentOutOfRangeException("position", position, new Win32Exception((int) Win32Errors.ERROR_NEGATIVE_SEEK).Message);

         if (length < 0)
            throw new ArgumentOutOfRangeException("length", length, Resources.Negative_Lock_Length);


         var success = NativeMethods.UnlockFile(SafeFileHandle, NativeMethods.GetLowOrderDword(position), NativeMethods.GetHighOrderDword(position), NativeMethods.GetLowOrderDword(length), NativeMethods.GetHighOrderDword(length));

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            NativeError.ThrowException(lastError);
      }


      /// <summary>Reads a stream header from the current <see cref="BackupFileStream"/>.</summary>
      /// <returns>The stream header read from the current <see cref="BackupFileStream"/>, or <c>null</c> if the end-of-file 
      /// was reached before the required number of bytes of a header could be read.</returns>
      /// <exception cref="IOException"/>
      /// <remarks>The stream must be positioned at where an actual header starts for the returned object to represent valid 
      /// information.</remarks>
      [SecurityCritical]
      public BackupStreamInfo ReadStreamInfo()
      {
         var sizeOf = Marshal.SizeOf(typeof(NativeMethods.WIN32_STREAM_ID));

         using (var hBuf = new SafeGlobalMemoryBufferHandle(sizeOf))
         {
            uint numberOfBytesRead;

            var success = NativeMethods.BackupRead(SafeFileHandle, hBuf, (uint) sizeOf, out numberOfBytesRead, false, _processSecurity, ref _context);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError);


            if (numberOfBytesRead == 0)
               return null;


            if (numberOfBytesRead < sizeOf)
               throw new IOException(Resources.Read_Incomplete_Header);


            var streamID = hBuf.PtrToStructure<NativeMethods.WIN32_STREAM_ID>(0);
            var nameLength = (uint) Math.Min(streamID.dwStreamNameSize, hBuf.Capacity);


            success = NativeMethods.BackupRead(SafeFileHandle, hBuf, nameLength, out numberOfBytesRead, false, _processSecurity, ref _context);

            lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError);


            var name = hBuf.PtrToStringUni(0, (int) nameLength / UnicodeEncoding.CharSize);

            return new BackupStreamInfo(streamID, name);
         }
      }




      #region Disposable Members

      /// <summary>Releases unmanaged resources and performs other cleanup operations before the <see cref="BackupFileStream"/> is reclaimed by garbage collection.</summary>
      ~BackupFileStream()
      {
         Dispose(false);
      }


      /// <summary>Releases the unmanaged resources used by the <see cref="System.IO.Stream"/> and optionally releases the managed resources.</summary>
      /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            // If one of the constructors previously threw an exception,
            // than the object hasn't been initialized properly and call from finalize will fail.         

            if (null != SafeFileHandle && !SafeFileHandle.IsInvalid)
            {
               if (_context != IntPtr.Zero)
               {
                  try
                  {
                     uint temp;

                     // MSDN: To release the memory used by the data structure, call BackupRead with the bAbort parameter set to TRUE when the backup operation is complete.
                     var success = NativeMethods.BackupRead(SafeFileHandle, new SafeGlobalMemoryBufferHandle(), 0, out temp, true, false, ref _context);

                     var lastError = Marshal.GetLastWin32Error();
                     if (!success)
                        NativeError.ThrowException(lastError);
                  }
                  finally
                  {
                     _context = IntPtr.Zero;

                     NativeMethods.CloseSafeHandle(SafeFileHandle);
                  }
               }
            }
         }


         base.Dispose(disposing);
      }
      
      #endregion // Disposable Members
   }
}
