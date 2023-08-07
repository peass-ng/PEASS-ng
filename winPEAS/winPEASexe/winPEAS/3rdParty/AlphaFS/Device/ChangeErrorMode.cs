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
   internal static partial class NativeMethods
   {
      /// <summary>Controls whether the system will handle the specified types of serious errors or whether the process will handle them.</summary>
      /// <remarks>Minimum supported client: Windows 2000 Professional</remarks>
      /// <remarks>Minimum supported server: Windows 2000 Server</remarks>      
      public sealed class ChangeErrorMode : IDisposable
      {
         private readonly ErrorMode _oldMode;

         /// <summary>ChangeErrorMode is for the Win32 SetThreadErrorMode() method, used to suppress possible pop-ups.</summary>
         /// <param name="mode">One of the <see cref="ErrorMode"/> values.</param>
         public ChangeErrorMode(ErrorMode mode)
         {
            if (IsAtLeastWindows7)
               SetThreadErrorMode(mode, out _oldMode);
            else
               _oldMode = SetErrorMode(mode);
         }

         void IDisposable.Dispose()
         {
            ErrorMode oldMode;

            if (IsAtLeastWindows7)
               SetThreadErrorMode(_oldMode, out oldMode);
            else
               SetErrorMode(_oldMode);
         }
      }
   }
}
