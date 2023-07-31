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
   internal static partial class NativeMethods
   {
      /// <summary>FINDEX_INFO_LEVELS Enumeration - Defines values that are used with the FindFirstFileEx function to specify the information level of the returned data.</summary>
      /// <remarks>
      ///   <para>Minimum supported client: Windows XP [desktop apps | Windows Store apps]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps | Windows Store apps]</para>
      /// </remarks>
      internal enum FINDEX_INFO_LEVELS
      {
         /// <summary>A standard set of attribute is returned in a <see cref="WIN32_FIND_DATA"/> structure.</summary>
         Standard = 0,

         /// <summary>The FindFirstFileEx function does not query the short file name, improving overall enumeration speed.</summary>
         /// <remarks>This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
         Basic = 1

         ///// <summary>This value is used for validation. Supported values are less than this value.</summary>
         //MaxLevel
      }
   }
}
