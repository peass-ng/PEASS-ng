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
   /// <summary>A bitfield of flags for specifying options for various internal operations that convert paths to full paths.</summary>
   [Flags]
   public enum GetFullPathOptions
   {
      /// <summary>No special options applies.</summary>
      None = 0,

      /// <summary>Remove any trailing whitespace from the path.</summary>
      TrimEnd = 1,

      /// <summary>Add a trailing directory separator to the path (if one does not already exist).</summary>
      AddTrailingDirectorySeparator = 2,

      /// <summary>Remove the trailing directory separator from the path (if one exists).</summary>
      RemoveTrailingDirectorySeparator = 4,

      /// <summary>Return full path as long full path (Unicode format). Not valid for <see cref="Path.GetRegularPath"/>.</summary>
      AsLongPath = 8,

      /// <summary>Prevents any exception from being thrown if a filesystem object does not exist. Not valid for <see cref="Path.GetRegularPath"/>.</summary>
      ContinueOnNonExist = 16,

      /// <summary>Check that the path contains only valid path-characters.</summary>
      CheckInvalidPathChars = 32,

      /// <summary>Also check for wildcard (? and *) characters.</summary>
      CheckAdditional = 64,

      /// <summary>Do not trim the trailing dot or space.</summary>
      KeepDotOrSpace = 128,

      /// <summary>Performs both <see cref="CheckInvalidPathChars"/> and <see cref="CheckAdditional"/> checks.</summary>
      FullCheck = CheckInvalidPathChars | CheckAdditional
   }
}
