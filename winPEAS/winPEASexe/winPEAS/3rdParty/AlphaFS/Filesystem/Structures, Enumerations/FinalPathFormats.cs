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

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Determines the format to convert a path to using <see cref="Path.GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle)"/>.</summary>
   [Flags]
   public enum FinalPathFormats
   {
      /// <summary>(FileNameNormalized / VolumeNameDos) Return the normalized drive name. This is the default.</summary>
      None = 0,

      /// <summary>Return the path with a volume GUID path instead of the drive name.</summary>
      VolumeNameGuid = 1,

      /// <summary>Return the path with the volume device path.</summary>
      VolumeNameNT = 2,

      /// <summary>Return the path with no drive information.</summary>
      VolumeNameNone = 4,

      /// <summary>Return the opened file name (not normalized).</summary>
      FileNameOpened = 8
   }
}