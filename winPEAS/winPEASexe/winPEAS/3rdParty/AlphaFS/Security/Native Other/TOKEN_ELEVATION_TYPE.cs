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

namespace Alphaleonis.Win32.Security
{
   internal static partial class NativeMethods
   {
      /// <summary>The TOKEN_ELEVATION_TYPE enumeration indicates the elevation type of token being queried by the GetTokenInformation function.</summary>
      /// <remarks>
      /// <para>Minimum supported client: Windows Vista [desktop apps only]</para>
      /// <para>Minimum supported server: Windows Server 2008 [desktop apps only]</para>
      /// </remarks>
      internal enum TOKEN_ELEVATION_TYPE
      {
         ///// <summary>The token does not have a linked token: UAC is disabled or the process is started by a standard User (not a member of the Administrators group).</summary>
         //TokenElevationTypeDefault = 1,

         /// <summary>The token is an elevated token: UAC is enabled and User is elevated.</summary>
         TokenElevationTypeFull = 2,

         ///// <summary>The token is a limited token: UAC is enabled but User is not elevated.</summary>
         //TokenElevationTypeLimited = 3
      }
   }
}
