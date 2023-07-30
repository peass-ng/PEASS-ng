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
   /// <summary>Defines the controllable aspects of the Volume.DefineDosDevice() method.</summary>
   [Flags]
   public enum DosDeviceAttributes
   {
      /// <summary>DDD_EXACT_MATCH_ON_REMOVE
      /// <para>Default.</para>
      /// </summary>
      None = 0,

      /// <summary>DDD_RAW_TARGET_PATH
      /// <para>Uses the targetPath string as is. Otherwise, it is converted from an MS-DOS path to a path.</para>
      /// </summary>
      RawTargetPath = 1,

      /// <summary>DDD_REMOVE_DEFINITION
      /// <para>Removes the specified definition for the specified device.</para>
      /// <para>To determine which definition to remove, the function walks the list of mappings for the device, looking for a match of targetPath against a prefix of each mapping associated with this device.</para>
      /// <para>The first mapping that matches is the one removed, and then the function returns.</para>
      /// <para>If targetPath is null or a pointer to a null string, the function will remove the first mapping associated with the device and pop the most recent one pushed.If there is nothing left to pop, the device name will be removed.</para>
      /// <para>If this value is not specified, the string pointed to by the targetPath parameter will become the new mapping for this device.</para>
      /// </summary>
      RemoveDefinition = 2,

      /// <summary>DDD_EXACT_MATCH_ON_REMOVE
      /// <para>If this value is specified along with <see cref="RemoveDefinition"/>, the function will use an exact match to determine which mapping to remove.</para>
      /// <para>Use this value to ensure that you do not delete something that you did not define.</para>
      /// </summary>
      ExactMatchOnRemove = 4,

      /// <summary>DDD_NO_BROADCAST_SYSTEM
      /// <para>Do not broadcast the WM_SETTINGCHANGE message.</para>
      /// <para>By default, this message is broadcast to notify the shell and applications of the change.</para>
      /// </summary>
      NoBroadcastSystem = 8
   }
}