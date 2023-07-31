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
   public static partial class Directory
   {
      private static void CopyFolderTimestamps(CopyMoveArguments cma)
      {
         // TODO 2018-01-09: Not 100% yet with local + UNC paths.
         var dstLp = cma.SourcePathLp.ReplaceIgnoreCase(cma.SourcePathLp, cma.DestinationPathLp);


         // Traverse the source folder, processing only folders.

         foreach (var fseiSource in EnumerateFileSystemEntryInfosCore<FileSystemEntryInfo>(true, cma.Transaction, cma.SourcePathLp, Path.WildcardStarMatchAll, null, null, cma.DirectoryEnumerationFilters, PathFormat.LongFullPath))

            File.CopyTimestampsCore(cma.Transaction, true, fseiSource.LongFullPath, Path.CombineCore(false, dstLp, fseiSource.FileName), false, PathFormat.LongFullPath);

         
         // Process the root directory, the given path.

         File.CopyTimestampsCore(cma.Transaction, true, cma.SourcePathLp, cma.DestinationPathLp, false, PathFormat.LongFullPath);


         // TODO: When enabled on Computer, FindFirstFile will change the last accessed time.
      }
   }
}
