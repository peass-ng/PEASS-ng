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
   /// <summary>Used by CopyFileXxx and MoveFileXxx. The <see cref="CopyMoveProgressRoutine"/> function should return one of the following values.</summary>
   public enum CopyMoveProgressResult 
   {
      /// <summary>PROGRESS_CONTINUE
      /// <para>Continue the copy/move operation.</para>
      /// </summary>
      Continue = 0,

      /// <summary>PROGRESS_CANCEL
      /// <para>Cancel the copy/move operation and delete the destination file.</para>
      /// </summary>
      Cancel = 1,

      /// <summary>PROGRESS_STOP
      /// <para>Stop the copy/move operation. It can be restarted at a later time.</para>
      /// </summary>
      Stop = 2,

      /// <summary>PROGRESS_QUIET
      /// <para>Continue the copy/move operation, but stop invoking <see cref="CopyMoveProgressRoutine"/> to report progress.</para>
      /// </summary>
      Quiet = 3
   }
}