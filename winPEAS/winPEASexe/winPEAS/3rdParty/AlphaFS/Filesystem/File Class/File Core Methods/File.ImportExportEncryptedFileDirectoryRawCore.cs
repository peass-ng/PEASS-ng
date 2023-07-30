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
using System.IO;
using System.Runtime.InteropServices;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      internal static void ImportExportEncryptedFileDirectoryRawCore(bool isExport, bool isFolder, Stream stream, string destinationPath, bool overwriteHidden, PathFormat pathFormat)
      {
         var destinationPathLp = Path.GetExtendedLengthPathCore(null, destinationPath, pathFormat, GetFullPathOptions.FullCheck | GetFullPathOptions.TrimEnd);

         var mode = isExport ? NativeMethods.EncryptedFileRawMode.CreateForExport : NativeMethods.EncryptedFileRawMode.CreateForImport;

         if (isFolder)
            mode = mode | NativeMethods.EncryptedFileRawMode.CreateForDir;

         if (overwriteHidden)
            mode = mode | NativeMethods.EncryptedFileRawMode.OverwriteHidden;


         // OpenEncryptedFileRaw()
         // 2015-08-02: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

         SafeEncryptedFileRawHandle context;
         var lastError = NativeMethods.OpenEncryptedFileRaw(destinationPathLp, mode, out context);

         try
         {
            if (lastError != Win32Errors.NO_ERROR)
               NativeError.ThrowException((int) lastError, isFolder, destinationPathLp);


            lastError = isExport
               ? NativeMethods.ReadEncryptedFileRaw((pbData, pvCallbackContext, length) =>
               {
                  try
                  {
                     var data = new byte[length];

                     Marshal.Copy(pbData, data, 0, (int) length);

                     stream.Write(data, 0, (int) length);
                  }
                  catch (Exception ex)
                  {
                     return Marshal.GetHRForException(ex) & NativeMethods.OverflowExceptionBitShift;
                  }

                  return (int) Win32Errors.NO_ERROR;

               }, IntPtr.Zero, context)


               : NativeMethods.WriteEncryptedFileRaw((IntPtr pbData, IntPtr pvCallbackContext, ref uint length) =>
               {
                  try
                  {
                     var data = new byte[length];

                     length = (uint) stream.Read(data, 0, (int) length);

                     if (length == 0)
                        return (int) Win32Errors.NO_ERROR;

                     Marshal.Copy(data, 0, pbData, (int) length);
                  }
                  catch (Exception ex)
                  {
                     return Marshal.GetHRForException(ex) & NativeMethods.OverflowExceptionBitShift;
                  }

                  return (int) Win32Errors.NO_ERROR;

               }, IntPtr.Zero, context);


            if (lastError != Win32Errors.NO_ERROR)
               NativeError.ThrowException((int) lastError, isFolder, destinationPathLp);
         }
         finally
         {
            if (null != context && !context.IsClosed)
               context.Dispose();
         }
      }
   }
}
