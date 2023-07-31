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

#if !NET35
using System.Threading;
#endif

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] Represents the method that will handle an error raised during retrieving file system entries.</summary>
   /// <returns><c>true</c>, if the error has been fully handled and the caller may proceed, 
   /// <param name="errorCode">The error code.</param>
   /// <param name="errorMessage">The error message.</param>
   /// <param name="pathProcessed">The faulty path being processed.</param>
   /// <c>false</c> otherwise, in which case the caller will throw the corresponding exception.</returns>   
   public delegate bool ErrorHandler(int errorCode, string errorMessage, string pathProcessed);


   /// <summary>[AlphaFS] Specifies a set of custom filters to be used with enumeration methods of <see cref="Directory"/>, e.g., <see cref="Directory.EnumerateDirectories(string)"/>, <see cref="Directory.EnumerateFiles(string)"/>, or <see cref="Directory.EnumerateFileSystemEntries(string)"/>.</summary>
   /// <remarks>
   /// <see cref="DirectoryEnumerationFilters"/> allows scenarios in which files/directories being 
   /// enumerated by the methods of <see cref="Directory"/> class are accepted only if 
   /// they match the search pattern, attributes (see <see cref="DirectoryEnumerationOptions.SkipReparsePoints"/>),
   /// and optionally also the custom criteria tested in the method whose delegate is specified in <see cref="InclusionFilter"/>. 
   /// These criteria could be, e.g., file size exceeding some threshold, pathname matches a compex regular expression, etc.
   /// If the enumeration process is set to be recursive (see <see cref="DirectoryEnumerationOptions.Recursive"/>) and <see cref="RecursionFilter"/>
   /// is specified, the directory is traversed recursively only if it matches the custom criteria in <see cref="RecursionFilter"/> 
   /// method. This allows, for example, custom handling of junctions and symbolic links, e.g., detection of cycles.
   /// If any error occurs during the enumeration and the enumeration process is not set to ignore errors
   /// (see <see cref="DirectoryEnumerationOptions.ContinueOnException"/>), an exception is thrown unless
   /// the error is handled (filtered out) by the method specified in <see cref="ErrorFilter"/> (if specified).
   /// The method may, for example, consume the error by reporting it in a log, so that the enumeration continues
   /// as in the case of <see cref="DirectoryEnumerationOptions.ContinueOnException"/> option but the user will be informed about errors.
   /// </remarks>
   public class DirectoryEnumerationFilters
   {
      /// <summary>Gets or sets the filter that returns <c>true</c> if the input file system entry should be included in the enumeration.</summary>
      /// <value>The delegate to a filtering method.</value>      
      public Predicate<FileSystemEntryInfo> InclusionFilter { get; set; }


      /// <summary>Gets or sets the filter that returns <c>true</c> if the input directory should be recursively traversed.</summary>
      /// <value>The delegate to a filtering method.</value>
      public Predicate<FileSystemEntryInfo> RecursionFilter { get; set; }


      /// <summary>Gets or sets the filter that returns <c>true</c> if the input error should not be thrown.</summary>
      /// <value>The delegate to a filtering method.</value>
      public ErrorHandler ErrorFilter { get; set; }


      /// <summary>The number of retries, excluding the first attempt. Default is <c>0</c>.</summary>
      public int ErrorRetry { get; set; }


      /// <summary>The wait time in seconds between retries. Default is <c>10</c> seconds.</summary>
      public int ErrorRetryTimeout { get; set; }


#if !NET35
      /// <summary>Gets or sets the cancellation token to abort the enumeration.</summary>
      /// <value>A <see cref="CancellationToken"/> instance.</value>
      public CancellationToken CancellationToken { get; set; }
#endif
   }
}
