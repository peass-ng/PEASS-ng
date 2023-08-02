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
      /// <summary>Specifies control options that filter the device information elements that are added to the device information set.</summary>
      [Flags]
      internal enum SetupDiGetClassDevsExFlags
      {
         /// <summary>DIGCF_DEFAULT
         /// <para>Return only the device that is associated with the system default device interface, if one is set, for the specified device interface classes.</para>
         /// </summary>
         Default = 1, // only valid with DIGCF_DEVICEINTERFACE

         /// <summary>DIGCF_PRESENT
         /// <para>Return only devices that are currently present.</para>
         /// </summary>
         Present = 2,

         /// <summary>DIGCF_ALLCLASSES
         /// <para>Return a list of installed devices for the specified device setup classes or device interface classes.</para>
         /// </summary>
         AllClasses = 4,

         /// <summary>DIGCF_PROFILE
         /// <para>Return only devices that are a part of the current hardware profile.</para>
         /// </summary>
         Profile = 8,

         /// <summary>DIGCF_DEVICEINTERFACE
         /// <para>
         /// Return devices that support device interfaces for the specified device interface classes.
         /// This flag must be set in the Flags parameter if the Enumerator parameter specifies a Device Instance ID. 
         /// </para>
         /// </summary>
         DeviceInterface = 16
      }
   }
}