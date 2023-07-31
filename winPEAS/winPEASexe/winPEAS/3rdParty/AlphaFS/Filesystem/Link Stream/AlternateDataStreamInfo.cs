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
using System.Globalization;
using System.Text;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Information about an alternate data stream.</summary>  
   /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.EnumerateAlternateDataStreams"/> 
   [Serializable]
   public struct AlternateDataStreamInfo : IEquatable<AlternateDataStreamInfo>
   {
      #region Fields

      [NonSerialized] private readonly string _fullPath;
      [NonSerialized] private readonly string _streamName;
      
      #endregion // Fields


      #region Constructor

      internal AlternateDataStreamInfo(string fullPath, NativeMethods.WIN32_FIND_STREAM_DATA findData)
      {
         _fullPath = fullPath;

         Size = findData.StreamSize;

         _streamName = ParseStreamName(findData.cStreamName);
      }

      #endregion // Constructor


      #region Properties

      /// <summary>Gets the full path to the stream.</summary>
      /// <remarks>
      ///   This is a path in long path format that can be passed to <see cref="O:Alphaleonis.Win32.Filesystem.File.Open"/> to open the stream if
      ///   <see cref="PathFormat.FullPath"/> or <see cref="PathFormat.LongFullPath"/> is specified.
      /// </remarks>
      /// <value>The full path to the stream in long path format.</value>
      public string FullPath
      {
         get { return string.Format(CultureInfo.InvariantCulture, "{0}{1}", _fullPath, !Utils.IsNullOrWhiteSpace(StreamName) ? Path.StreamSeparator + StreamName : string.Empty); }
      }
      

      /// <summary>Gets the size of the stream.</summary>      
      public long Size { get; private set; }


      /// <summary>Gets the name of the alternate data stream.</summary>
      /// <remarks>This value is an empty string for the default stream (:$DATA), and for any other data stream it contains the name of the stream.</remarks>
      /// <value>The name of the stream.</value>
      public string StreamName
      {
         get { return _streamName; }
      }

      #endregion // Properties


      #region Methods

      /// <summary>Returns the hash code for this instance.</summary>
      /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
      public override int GetHashCode()
      {
         return Utils.CombineHashCodesOf(StreamName, FullPath);
      }
      

      /// <summary>Determines whether the specified Object is equal to the current Object.</summary>
      /// <param name="other">Another <see cref="AlternateDataStreamInfo"/> instance to compare to.</param>
      /// <returns><c>true</c> if the specified Object is equal to the current Object; otherwise, <c>false</c>.</returns>
      public bool Equals(AlternateDataStreamInfo other)
      {
         return GetType() == other.GetType() &&
                Equals(StreamName, other.StreamName) &&
                Equals(FullPath, other.FullPath) &&
                Equals(Size, other.Size);
      }


      /// <summary>Indicates whether this instance and a specified object are equal.</summary>
      /// <param name="obj">The object to compare with the current instance.</param>
      /// <returns>
      ///   true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
      /// </returns>
      public override bool Equals(object obj)
      {
         return obj is AlternateDataStreamInfo && Equals((AlternateDataStreamInfo) obj);
      }


      // <summary>Implements the operator ==</summary>
      /// <param name="left">A.</param>
      /// <param name="right">B.</param>
      /// <returns>The result of the operator.</returns>
      public static bool operator ==(AlternateDataStreamInfo left, AlternateDataStreamInfo right)
      {
         return left.Equals(right);
      }


      /// <summary>Implements the operator !=</summary>
      /// <param name="left">A.</param>
      /// <param name="right">B.</param>
      /// <returns>The result of the operator.</returns>
      public static bool operator !=(AlternateDataStreamInfo left, AlternateDataStreamInfo right)
      {
         return !(left == right);
      }

      #endregion // Methods


      #region Private Methods

      private static string ParseStreamName(string streamName)
      {
         if (null == streamName || streamName.Length < 2)
            return string.Empty;

         if (streamName[0] != Path.StreamSeparatorChar)
            throw new ArgumentException(Resources.Invalid_Stream_Name, "streamName");

         
         var sb = new StringBuilder(streamName.Length);

         for (int i = 1, l = streamName.Length; i < l; i++)
         {
            if (streamName[i] == Path.StreamSeparatorChar)
               break;

            sb.Append(streamName[i]);
         }


         return sb.ToString();
      }

      #endregion // Private Methods
   }
}
