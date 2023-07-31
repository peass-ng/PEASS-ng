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
   /// <summary>The <see cref="BackupStreamInfo"/> structure contains stream header data.</summary>
   /// <seealso cref="BackupFileStream"/>
   public sealed class BackupStreamInfo
   {
      #region Fields

      private readonly long _streamSize;
      private readonly string _streamName;
      private readonly StreamId _streamId;
      private readonly StreamAttribute _streamAttribute;

      #endregion // Fields


      #region Constructor

      /// <summary>Initializes a new instance of the <see cref="BackupStreamInfo"/> class.</summary>
      /// <param name="streamId">The stream ID.</param>
      /// <param name="name">The name.</param>
      internal BackupStreamInfo(NativeMethods.WIN32_STREAM_ID streamId, string name)
      {
         _streamName = name;
         _streamSize = (long)streamId.Size;
         _streamAttribute = (StreamAttribute)streamId.dwStreamAttribute;
         _streamId = (StreamId)streamId.dwStreamId;
      }

      #endregion // Constructor


      #region Public Properties

      /// <summary>Gets the size of the data in the substream, in bytes.</summary>
      /// <value>The size of the data in the substream, in bytes.</value>
      public long Size
      {
         get { return _streamSize; }
      }


      /// <summary>Gets a string that specifies the name of the alternative data stream.</summary>
      /// <value>A string that specifies the name of the alternative data stream.</value>
      public string Name
      {
         get { return _streamName; }
      }


      /// <summary>Gets the type of the data in the stream.</summary>
      /// <value>The type of the data in the stream.</value>
      public StreamId StreamType
      {
         get { return _streamId; }
      }


      /// <summary>Gets the attributes of the data to facilitate cross-operating system transfer.</summary>
      /// <value>Attributes of the data to facilitate cross-operating system transfer.</value>
      public StreamAttribute Attribute
      {
         get { return _streamAttribute; }
      }

      #endregion // Public Properties
   }
}
