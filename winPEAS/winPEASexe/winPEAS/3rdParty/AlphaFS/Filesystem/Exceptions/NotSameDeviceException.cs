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
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] The exception that is thrown when an attempt perform an operation across difference devices when this is not supported.</summary>
   [Serializable]
   public class NotSameDeviceException : System.IO.IOException
   {
      private static readonly int ErrorCode = Win32Errors.GetHrFromWin32Error(Win32Errors.ERROR_NOT_SAME_DEVICE);
      private static readonly string ErrorText = string.Format(CultureInfo.InvariantCulture, "({0}) {1}", Win32Errors.ERROR_NOT_SAME_DEVICE, new Win32Exception((int)Win32Errors.ERROR_NOT_SAME_DEVICE).Message.Trim().TrimEnd('.').Trim());


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="NotSameDeviceException"/> class.</summary>
      public NotSameDeviceException() : base(Resources.File_Or_Directory_Already_Exists, ErrorCode)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="NotSameDeviceException"/> class.</summary>
      /// <param name="message">The message.</param>
      public NotSameDeviceException(string message) : base(message, ErrorCode)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="NotSameDeviceException"/> class.</summary>
      /// <param name="message">The message.</param>
      /// <param name="innerException">The inner exception.</param>
      public NotSameDeviceException(string message, Exception innerException) : base(message, innerException)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="NotSameDeviceException"/> class.</summary>
      /// <param name="path">The path to the device.</param>
      /// <param name="isPath">Always set to true when using this constructor.</param>
      [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "isPath")]
      public NotSameDeviceException(string path, bool isPath) : base(string.Format(CultureInfo.InvariantCulture, "{0}: [{1}]", ErrorText, path), ErrorCode)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="NotSameDeviceException"/> class.</summary>
      /// <param name="info">The data for serializing or deserializing the object.</param>
      /// <param name="context">The source and destination for the object.</param>
      protected NotSameDeviceException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
   }
}
