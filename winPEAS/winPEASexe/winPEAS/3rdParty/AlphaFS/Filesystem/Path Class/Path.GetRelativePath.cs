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
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>[AlphaFS] Gets the relative path from the <paramref name="startPath"/> path to the end path.</summary>
      /// <param name="startPath">The absolute or relative folder path.</param>
      /// <param name="selectedPath">The absolute or relative path containing the directory or file.</param>
      /// <returns>The relative path containing the directory or file, from the <paramref name="startPath"/> path to the end path.</returns>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="UriFormatException"/>
      /// <exception cref="InvalidOperationException"/>
      public static string GetRelativePath(string startPath, string selectedPath)
      {
         if (string.IsNullOrEmpty(startPath))
            throw new ArgumentNullException("startPath");

         if (string.IsNullOrEmpty(selectedPath))
            throw new ArgumentNullException("selectedPath");


         var fromDirectories = startPath.Split(DirectorySeparatorChar);
         var toDirectories = selectedPath.Split(DirectorySeparatorChar);

         var fromLength = fromDirectories.Length;
         var toLength = toDirectories.Length;
         var length = Math.Min(fromLength, toLength);
         var lastCommonRoot = -1;


         // Find common path root.

         for (var index = 0; index < length; index++)
         {
            if (string.Compare(fromDirectories[index], toDirectories[index], StringComparison.OrdinalIgnoreCase) != 0)
               break;

            lastCommonRoot = index;
         }


         if (lastCommonRoot == -1)
            return selectedPath;

         lastCommonRoot++;


         // Assemble relative path.

         var relativePath = new StringBuilder();

         
         // Add the "..\" suffix.

         for (var index = lastCommonRoot; index < fromLength; index++)
         {
            if (fromDirectories[index].Length > 0)
               relativePath.Append(ParentDirectoryPrefix + DirectorySeparator);
         }


         // Add folders.

         toLength--;

         for (var index = lastCommonRoot; index < toLength; index++)

            relativePath.Append(toDirectories[index] + DirectorySeparator);


         relativePath.Append(toDirectories[toLength]);

         return relativePath.ToString();
      }


      ///// <summary>[AlphaFS] Gets the relative path from the <paramref name="startPath"/> path to the end path.</summary>
      ///// <param name="startPath">The absolute or relative folder path.</param>
      ///// <param name="selectedPath">The absolute or relative path containing the directory or file.</param>
      ///// <returns>The relative path from the <paramref name="startPath"/> path to the end path.</returns>
      ///// <exception cref="ArgumentNullException"/>
      ///// <exception cref="UriFormatException"/>
      ///// <exception cref="InvalidOperationException"/>
      //public static string GetRelativePath(string startPath, string selectedPath)
      //{
      //   if (string.IsNullOrEmpty(startPath))
      //      throw new ArgumentNullException("startPath");

      //   if (string.IsNullOrEmpty(selectedPath))
      //      throw new ArgumentNullException("selectedPath");


      //   if (IsPathRooted(startPath) && IsPathRooted(selectedPath))
      //   {
      //      if (string.Compare(GetPathRoot(startPath), GetPathRoot(selectedPath), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) != 0)
      //         return selectedPath;
      //   }


      //   var fromUri = new Uri(AddTrailingDirectorySeparator(startPath, false), UriKind.RelativeOrAbsolute);
      //   var toUri = new Uri(AddTrailingDirectorySeparator(selectedPath, false), UriKind.RelativeOrAbsolute);


      //   if (fromUri.IsAbsoluteUri && toUri.IsAbsoluteUri)
      //   {
      //      if (fromUri.Scheme != toUri.Scheme)
      //         return selectedPath;
      //   }


      //   var relativeUri = toUri.IsAbsoluteUri ? fromUri.MakeRelativeUri(toUri) : toUri;
      //   var relativePath = Uri.UnescapeDataString(relativeUri.ToString());


      //   if (toUri.IsAbsoluteUri && string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
      //      relativePath = relativePath.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);


      //   return relativePath.TrimEnd(DirectorySeparatorChar, AltDirectorySeparatorChar);
      //}




      /// <summary>[AlphaFS] Gets the absolute path from the relative or absolute <paramref name="startPath"/> and the relative <paramref name="selectedPath"/>.</summary>
      /// <returns>The absolute path from the relative or absolute <paramref name="startPath"/> and the relative <paramref name="selectedPath"/>.</returns>
      /// <param name="startPath">The absolute folder path.</param>
      /// <param name="selectedPath">The selected path containing the directory or file.</param>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="UriFormatException"/>
      /// <exception cref="InvalidOperationException"/>
      public static string ResolveRelativePath(string startPath, string selectedPath)
      {
         return GetFullPath(Combine(IsPathRooted(startPath) ? startPath : DirectorySeparator + startPath, IsPathRooted(selectedPath) ? selectedPath : DirectorySeparator + selectedPath), GetFullPathOptions.FullCheck);
      }
   }
}
