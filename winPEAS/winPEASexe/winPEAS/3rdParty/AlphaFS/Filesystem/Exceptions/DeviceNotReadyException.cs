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
using System.Globalization;
using System.Runtime.Serialization;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] The requested operation could not be completed because the device is not ready.</summary>
   [Serializable]
   public class DeviceNotReadyException : System.IO.IOException
   {
      private static readonly int ErrorCode = Win32Errors.GetHrFromWin32Error(Win32Errors.ERROR_NOT_READY);
      private static readonly string ErrorText = string.Format(CultureInfo.InvariantCulture, "({0}) {1}", Win32Errors.ERROR_NOT_READY, new Win32Exception((int) Win32Errors.ERROR_NOT_READY).Message.Trim().TrimEnd('.').Trim());


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DeviceNotReadyException"/> class.</summary>
      public DeviceNotReadyException() : base(string.Format(CultureInfo.InvariantCulture, "{0}.", ErrorText), ErrorCode)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DeviceNotReadyException"/> class.</summary>
      /// <param name="message">The message.</param>
      public DeviceNotReadyException(string message) : base(message, ErrorCode)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DeviceNotReadyException"/> class.</summary>
      /// <param name="path">The path to the file system object.</param>
      /// <param name="isPath">Always set to true when using this constructor.</param>
      [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "isPath")]
      public DeviceNotReadyException(string path, bool isPath) : base(string.Format(CultureInfo.InvariantCulture, "{0}: [{1}]", ErrorText, Path.GetCleanExceptionPath(path)), ErrorCode)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DeviceNotReadyException"/> class.</summary>
      /// <param name="path">The path to the device.</param>
      /// <param name="innerException">The inner exception.</param>
      public DeviceNotReadyException(string path, Exception innerException) : base(string.Format(CultureInfo.InvariantCulture, "{0}: [{1}]", ErrorText, Path.GetCleanExceptionPath(path)), innerException)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="DeviceNotReadyException"/> class.</summary>
      /// <param name="info">The data for serializing or deserializing the object.</param>
      /// <param name="context">The source and destination for the object.</param>
      protected DeviceNotReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
   }
}
