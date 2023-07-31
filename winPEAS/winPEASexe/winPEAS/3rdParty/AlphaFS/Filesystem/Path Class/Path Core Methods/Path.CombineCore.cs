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
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>Combines an array of strings into a path.</summary>
      /// <returns>The combined paths.</returns>
      /// <remarks>
      ///   <para>The parameters are not parsed if they have white space.</para>
      ///   <para>Therefore, if path2 includes white space (for example, " c:\\ "),</para>
      ///   <para>the Combine method appends path2 to path1 instead of returning only path2.</para>
      /// </remarks>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <param name="checkInvalidPathChars"><c>true</c> will not check <paramref name="paths"/> for invalid path characters.</param>
      /// <param name="paths">An array of parts of the path.</param>
      [SecurityCritical]
      internal static string CombineCore(bool checkInvalidPathChars, params string[] paths)
      {
         if (null == paths)
            throw new ArgumentNullException("paths");

         var capacity = 0;
         var num = 0;

         for (int index = 0, l = paths.Length; index < l; ++index)
         {
            if (null == paths[index])
               throw new ArgumentNullException("paths");

            if (paths[index].Length != 0)
            {
               if (IsPathRooted(paths[index], checkInvalidPathChars))
               {
                  num = index;
                  capacity = paths[index].Length;
               }

               else
                  capacity += paths[index].Length;


               var ch = paths[index][paths[index].Length - 1];

               if (!IsDVsc(ch, null))
                  ++capacity;
            }
         }


         var buffer = new StringBuilder(capacity);

         for (var index = num; index < paths.Length; ++index)
         {
            if (paths[index].Length != 0)
            {
               if (buffer.Length == 0)
                  buffer.Append(paths[index]);

               else
               {
                  var ch = buffer[buffer.Length - 1];

                  if (!IsDVsc(ch, null))
                     buffer.Append(DirectorySeparatorChar);

                  buffer.Append(paths[index]);
               }
            }
         }

         return buffer.ToString();
      }
   }
}
