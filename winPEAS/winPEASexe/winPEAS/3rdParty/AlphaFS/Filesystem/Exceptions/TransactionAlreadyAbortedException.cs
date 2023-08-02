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
   /// <summary>[AlphaFS] It is too late to perform the requested operation, since the Transaction has already been aborted.</summary>
   [Serializable]
   public class TransactionAlreadyAbortedException : TransactionException
   {
      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionAlreadyAbortedException"/> class.</summary>
      public TransactionAlreadyAbortedException()
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionAlreadyAbortedException"/> class.</summary>
      /// <param name="message">The message.</param>
      public TransactionAlreadyAbortedException(string message) : base(message)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionAlreadyAbortedException"/> class.</summary>
      /// <param name="message">The message.</param>
      /// <param name="innerException">The inner exception.</param>
      public TransactionAlreadyAbortedException(string message, Exception innerException) : base(message, innerException)
      {
      }


      /// <summary>[AlphaFS] Initializes a new instance of the <see cref="TransactionAlreadyAbortedException"/> class.</summary>
      /// <param name="info">The info.</param>
      /// <param name="context">The context.</param>
      protected TransactionAlreadyAbortedException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
   }
}
