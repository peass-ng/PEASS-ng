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
      /// <summary>The TOKEN_INFORMATION_CLASS enumeration contains values that specify the type of information being assigned to or retrieved from an access token.
      /// <para>The GetTokenInformation function uses these values to indicate the type of token information to retrieve.</para>
      /// <para>The SetTokenInformation function uses these values to set the token information.</para>
      /// </summary>
      /// <remarks>
      /// <para>Minimum supported client: Windows XP [desktop apps only]</para>
      /// <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      internal enum TOKEN_INFORMATION_CLASS
      {
         ///// <summary>The buffer receives a TOKEN_USER structure that contains the user account of the token.</summary>
         //TokenUser = 1,

         ///// <summary>The buffer receives a TOKEN_GROUPS structure that contains the group accounts associated with the token.</summary>
         //TokenGroups = 2,

         ///// <summary>The buffer receives a TOKEN_PRIVILEGES structure that contains the privileges of the token.</summary>
         //TokenPrivileges = 3,

         ///// <summary>The buffer receives a TOKEN_OWNER structure that contains the default owner security identifier (SID) for newly created objects.</summary>
         //TokenOwner = 4,

         ///// <summary>The buffer receives a TOKEN_PRIMARY_GROUP structure that contains the default primary group SID for newly created objects.</summary>
         //TokenPrimaryGroup = 5,

         ///// <summary>The buffer receives a TOKEN_DEFAULT_DACL structure that contains the default DACL for newly created objects.</summary>
         //TokenDefaultDacl = 6,

         ///// <summary>The buffer receives a TOKEN_SOURCE structure that contains the source of the token. TOKEN_QUERY_SOURCE access is needed to retrieve this information.</summary>
         //TokenSource = 7,

         ///// <summary>The buffer receives a TOKEN_TYPE value that indicates whether the token is a primary or impersonation token.</summary>
         //TokenType = 8,

         ///// <summary>The buffer receives a SECURITY_IMPERSONATION_LEVEL value that indicates the impersonation level of the token. If the access token is not an impersonation token, the function fails.</summary>
         //TokenImpersonationLevel = 9,

         ///// <summary>The buffer receives a TOKEN_STATISTICS structure that contains various token statistics.</summary>
         //TokenStatistics = 10,

         ///// <summary>The buffer receives a TOKEN_GROUPS structure that contains the list of restricting SIDs in a restricted token.</summary>
         //TokenRestrictedSids = 11,

         ///// <summary>The buffer receives a DWORD value that indicates the Terminal Services session identifier that is associated with the token.</summary>
         //TokenSessionId = 12,

         ///// <summary>The buffer receives a TOKEN_GROUPS_AND_PRIVILEGES structure that contains the user SID, the group accounts, the restricted SIDs, and the authentication ID associated with the token.</summary>
         //TokenGroupsAndPrivileges = 13,

         ///// <summary>Reserved.</summary>
         //TokenSessionReference = 14,

         ///// <summary>The buffer receives a DWORD value that is nonzero if the token includes the SANDBOX_INERT flag.</summary>
         //TokenSandBoxInert = 15,

         ///// <summary>Reserved.</summary>
         //TokenAuditPolicy = 16,

         ///// <summary>The buffer receives a TOKEN_ORIGIN value.</summary>
         //TokenOrigin = 17,

         /// <summary>The buffer receives a TOKEN_ELEVATION_TYPE value that specifies the elevation level of the token.</summary>
         TokenElevationType = 18,

         ///// <summary>The buffer receives a TOKEN_LINKED_TOKEN structure that contains a handle to another token that is linked to this token.</summary>
         //TokenLinkedToken = 19,

         ///// <summary>The buffer receives a TOKEN_ELEVATION structure that specifies whether the token is elevated.</summary>
         //TokenElevation = 20,

         ///// <summary>The buffer receives a DWORD value that is nonzero if the token has ever been filtered.</summary>
         //TokenHasRestrictions = 21,

         ///// <summary>The buffer receives a TOKEN_ACCESS_INFORMATION structure that specifies security information contained in the token.</summary>
         //TokenAccessInformation = 22,

         ///// <summary>The buffer receives a DWORD value that is nonzero if virtualization is allowed for the token.</summary>
         //TokenVirtualizationAllowed = 23,

         ///// <summary>The buffer receives a DWORD value that is nonzero if virtualization is enabled for the token.</summary>
         //TokenVirtualizationEnabled = 24,

         ///// <summary>The buffer receives a TOKEN_MANDATORY_LABEL structure that specifies the token's integrity level.</summary>
         //TokenIntegrityLevel = 25,

         ///// <summary>The buffer receives a DWORD value that is nonzero if the token has the UIAccess flag set.</summary>
         //TokenUIAccess = 26,

         ///// <summary>The buffer receives a TOKEN_MANDATORY_POLICY structure that specifies the token's mandatory integrity policy.</summary>
         //TokenMandatoryPolicy = 27,

         ///// <summary>The buffer receives a TOKEN_GROUPS structure that specifies the token's logon SID.</summary>
         //TokenLogonSid = 28,

         ///// <summary>The buffer receives a DWORD value that is nonzero if the token is an app container token.</summary>
         //TokenIsAppContainer = 29,

         ///// <summary>The buffer receives a TOKEN_GROUPS structure that contains the capabilities associated with the token.</summary>
         //TokenCapabilities = 30,

         ///// <summary>The buffer receives a TOKEN_APPCONTAINER_INFORMATION structure that contains the AppContainerSid associated with the token.</summary>
         //TokenAppContainerSid = 31,

         ///// <summary>The buffer receives a DWORD value that includes the app container number for the token. For tokens that are not app container tokens, this value is zero.</summary>
         //TokenAppContainerNumber = 32,

         ///// <summary>The buffer receives a CLAIM_SECURITY_ATTRIBUTES_INFORMATION structure that contains the user claims associated with the token.</summary>
         //TokenUserClaimAttributes = 33,

         ///// <summary>The buffer receives a CLAIM_SECURITY_ATTRIBUTES_INFORMATION structure that contains the device claims associated with the token.</summary>
         //TokenDeviceClaimAttributes = 34,

         ///// <summary>This value is reserved.</summary>
         //TokenRestrictedUserClaimAttributes = 35,

         ///// <summary>This value is reserved.</summary>
         //TokenRestrictedDeviceClaimAttributes = 36,

         ///// <summary>The buffer receives a TOKEN_GROUPS structure that contains the device groups that are associated with the token.</summary>
         //TokenDeviceGroups = 37,

         ///// <summary>The buffer receives a TOKEN_GROUPS structure that contains the restricted device groups that are associated with the token.</summary>
         //TokenRestrictedDeviceGroups = 38,

         ///// <summary>This value is reserved.</summary>
         //TokenSecurityAttributes = 39,

         ///// <summary>This value is reserved.</summary>
         //TokenIsRestricted = 40, 

         ///// <summary>The maximum value for this enumeration.</summary>
         //MaxTokenInfoClass = 41
      }
   }
}
