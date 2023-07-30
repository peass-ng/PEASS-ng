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
using System.Runtime.Serialization;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] The exception that is thrown when an attempt to create a file or directory that already exists was made.</summary>
   [Serializable]
   public class TransactionException : SystemException
   {
      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionException"/> class.</summary>
      public TransactionException()
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionException"/> class.</summary>
      /// <param name="message">The message.</param>
      public TransactionException(string message) : base(message)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionException"/> class.</summary>
      /// <param name="message">The message.</param>
      /// <param name="innerException">The inner exception.</param>
      public TransactionException(string message, Exception innerException) : base(message, innerException)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionException"/> class.</summary>
      /// <param name="info">The data for serializing or deserializing the object.</param>
      /// <param name="context">The source and destination for the object.</param>
      protected TransactionException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
   }
}
