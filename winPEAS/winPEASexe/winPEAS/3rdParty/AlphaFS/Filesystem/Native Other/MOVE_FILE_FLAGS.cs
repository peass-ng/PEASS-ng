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
      public enum MOVE_FILE_FLAGS
      {
         /// <summary>No MoveOptions used, this fails when the file name already exists.</summary>
         None = 0,

         /// <summary>MOVE_FILE_REPLACE_EXISTSING
         /// <para>If the destination file name already exists, the function replaces its contents with the contents of the source file.</para>
         /// <para>This value cannot be used if lpNewFileName or lpExistingFileName names a directory.</para>
         /// <para>This value cannot be used if either source or destination names a directory.</para>
         /// </summary>
         MOVE_FILE_REPLACE_EXISTSING = 1,

         /// <summary>MOVE_FILE_COPY_ALLOWED
         /// <para>If the file is to be moved to a different volume, the function simulates the move by using the CopyFile and DeleteFile functions.</para>
         /// <para>This value cannot be used with <see cref="MOVE_FILE_FLAGS.MOVE_FILE_DELAY_UNTIL_REBOOT"/>.</para>
         /// </summary>
         MOVE_FILE_COPY_ALLOWED = 2,

         /// <summary>MOVE_FILE_DELAY_UNTIL_REBOOT
         /// <para>
         /// The system does not move the file until the operating system is restarted.
         /// The system moves the file immediately after AUTOCHK is executed, but before creating any paging files.
         /// </para>
         /// <para>
         /// Consequently, this parameter enables the function to delete paging files from previous startups.
         /// This value can only be used if the process is in the context of a user who belongs to the administrators group or the LocalSystem account.
         /// </para>
         /// <para>This value cannot be used with <see cref="MOVE_FILE_FLAGS.MOVE_FILE_COPY_ALLOWED"/>.</para>
         /// </summary>
         MOVE_FILE_DELAY_UNTIL_REBOOT = 4,


         /// <summary>MOVE_FILE_WRITE_THROUGH
         /// <para>The function does not return until the file has actually been moved on the disk.</para>
         /// <para>
         /// Setting this value guarantees that a move performed as a copy and delete operation is flushed to disk before the function returns.
         /// The flush occurs at the end of the copy operation.
         /// </para>
         /// <para>This value has no effect if <see cref="MOVE_FILE_FLAGS.MOVE_FILE_DELAY_UNTIL_REBOOT"/> is set.</para>
         /// </summary>
         MOVE_FILE_WRITE_THROUGH = 8,


         /// <summary>MOVE_FILE_CREATE_HARDLINK
         /// <para>Reserved for future use.</para>
         /// </summary>
         MOVE_FILE_CREATE_HARDLINK = 16,


         /// <summary>MOVE_FILE_FAIL_IF_NOT_TRACKABLE
         /// <para>The function fails if the source file is a link source, but the file cannot be tracked after the move.</para>
         /// <para>This situation can occur if the destination is a volume formatted with the FAT file system.</para>
         /// </summary>
         MOVE_FILE_FAIL_IF_NOT_TRACKABLE = 32
      }
   }
}
