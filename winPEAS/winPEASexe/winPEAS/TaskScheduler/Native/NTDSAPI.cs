using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32
{
	internal static partial class NativeMethods
	{
		private const string NTDSAPI = "ntdsapi.dll";

		/// <summary>
		/// Defines the errors returned by the status member of the DS_NAME_RESULT_ITEM structure. These are potential errors that may be encountered while a name is converted by the DsCrackNames function.
		/// </summary>
		public enum DS_NAME_ERROR : uint
		{
			/// <summary>The conversion was successful.</summary>
			DS_NAME_NO_ERROR = 0,

			///<summary>Generic processing error occurred.</summary>
			DS_NAME_ERROR_RESOLVING = 1,

			///<summary>The name cannot be found or the caller does not have permission to access the name.</summary>
			DS_NAME_ERROR_NOT_FOUND = 2,

			///<summary>The input name is mapped to more than one output name or the desired format did not have a single, unique value for the object found.</summary>
			DS_NAME_ERROR_NOT_UNIQUE = 3,

			///<summary>The input name was found, but the associated output format cannot be found. This can occur if the object does not have all the required attributes.</summary>
			DS_NAME_ERROR_NO_MAPPING = 4,

			///<summary>Unable to resolve entire name, but was able to determine in which domain object resides. The caller is expected to retry the call at a domain controller for the specified domain. The entire name cannot be resolved, but the domain that the object resides in could be determined. The pDomain member of the DS_NAME_RESULT_ITEM contains valid data when this error is specified.</summary>
			DS_NAME_ERROR_DOMAIN_ONLY = 5,

			///<summary>A syntactical mapping cannot be performed on the client without transmitting over the network.</summary>
			DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING = 6,

			///<summary>The name is from an external trusted forest.</summary>
			DS_NAME_ERROR_TRUST_REFERRAL = 7
		}

		/// <summary>
		/// Used to define how the name syntax will be cracked. These flags are used by the DsCrackNames function.
		/// </summary>
		[Flags]
		public enum DS_NAME_FLAGS
		{
			/// <summary>Indicate that there are no associated flags.</summary>
			DS_NAME_NO_FLAGS = 0x0,

			///<summary>Perform a syntactical mapping at the client without transferring over the network. The only syntactic mapping supported is from DS_FQDN_1779_NAME to DS_CANONICAL_NAME or DS_CANONICAL_NAME_EX.</summary>
			DS_NAME_FLAG_SYNTACTICAL_ONLY = 0x1,

			///<summary>Force a trip to the DC for evaluation, even if this could be locally cracked syntactically.</summary>
			DS_NAME_FLAG_EVAL_AT_DC = 0x2,

			///<summary>The call fails if the domain controller is not a global catalog server.</summary>
			DS_NAME_FLAG_GCVERIFY = 0x4,

			///<summary>Enable cross forest trust referral.</summary>
			DS_NAME_FLAG_TRUST_REFERRAL = 0x8
		}

		/// <summary>
		/// Provides formats to use for input and output names for the DsCrackNames function.
		/// </summary>
		public enum DS_NAME_FORMAT
		{
			///<summary>Indicates the name is using an unknown name type. This format can impact performance because it forces the server to attempt to match all possible formats. Only use this value if the input format is unknown.</summary>
			DS_UNKNOWN_NAME = 0,

			///<summary>Indicates that the fully qualified distinguished name is used. For example: "CN = someone, OU = Users, DC = Engineering, DC = Fabrikam, DC = Com"</summary>
			DS_FQDN_1779_NAME = 1,

			///<summary>Indicates a Windows NT 4.0 account name. For example: "Engineering\someone" The domain-only version includes two trailing backslashes (\\).</summary>
			DS_NT4_ACCOUNT_NAME = 2,

			///<summary>Indicates a user-friendly display name, for example, Jeff Smith. The display name is not necessarily the same as relative distinguished name (RDN).</summary>
			DS_DISPLAY_NAME = 3,

			///<summary>Indicates a GUID string that the IIDFromString function returns. For example: "{4fa050f0-f561-11cf-bdd9-00aa003a77b6}"</summary>
			DS_UNIQUE_ID_NAME = 6,

			///<summary>Indicates a complete canonical name. For example: "engineering.fabrikam.com/software/someone" The domain-only version includes a trailing forward slash (/).</summary>
			DS_CANONICAL_NAME = 7,

			///<summary>Indicates that it is using the user principal name (UPN). For example: "someone@engineering.fabrikam.com"</summary>
			DS_USER_PRINCIPAL_NAME = 8,

			///<summary>This element is the same as DS_CANONICAL_NAME except that the rightmost forward slash (/) is replaced with a newline character (\n), even in a domain-only case. For example: "engineering.fabrikam.com/software\nsomeone"</summary>
			DS_CANONICAL_NAME_EX = 9,

			///<summary>Indicates it is using a generalized service principal name. For example: "www/www.fabrikam.com@fabrikam.com"</summary>
			DS_SERVICE_PRINCIPAL_NAME = 10,

			///<summary>Indicates a Security Identifier (SID) for the object. This can be either the current SID or a SID from the object SID history. The SID string can use either the standard string representation of a SID, or one of the string constants defined in Sddl.h. For more information about converting a binary SID into a SID string, see SID Strings. The following is an example of a SID string: "S-1-5-21-397955417-626881126-188441444-501"</summary>
			DS_SID_OR_SID_HISTORY_NAME = 11,
		}

		/// <summary>
		/// Class that provides methods against a AD domain service.
		/// </summary>
		/// <seealso cref="System.IDisposable" />
		[SuppressUnmanagedCodeSecurity, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public class DomainService : IDisposable
		{
			IntPtr handle = IntPtr.Zero;

			/// <summary>
			/// Initializes a new instance of the <see cref="DomainService"/> class.
			/// </summary>
			/// <param name="domainControllerName">Name of the domain controller.</param>
			/// <param name="dnsDomainName">Name of the DNS domain.</param>
			/// <exception cref="System.ComponentModel.Win32Exception"></exception>
			public DomainService(string domainControllerName = null, string dnsDomainName = null)
			{
				DsBind(domainControllerName, dnsDomainName, out handle);
			}

			/// <summary>
			/// Converts a directory service object name from any format to the UPN. 
			/// </summary>
			/// <param name="name">The name to convert.</param>
			/// <returns>The corresponding UPN.</returns>
			/// <exception cref="System.Security.SecurityException">Unable to resolve user name.</exception>
			public string CrackName(string name)
			{
				var res = CrackNames(new string[] { name });
				if (res == null || res.Length == 0 || res[0].status != NativeMethods.DS_NAME_ERROR.DS_NAME_NO_ERROR)
					throw new SecurityException("Unable to resolve user name.");
				return res[0].pName;
			}

			/// <summary>
			/// Converts an array of directory service object names from one format to another. Name conversion enables client applications to map between the multiple names used to identify various directory service objects. 
			/// </summary>
			/// <param name="names">The names to convert.</param>
			/// <param name="flags">Values used to determine how the name syntax will be cracked.</param>
			/// <param name="formatOffered">Format of the input names.</param>
			/// <param name="formatDesired">Desired format for the output names.</param>
			/// <returns>An array of DS_NAME_RESULT_ITEM structures. Each element of this array represents a single converted name.</returns>
			public DS_NAME_RESULT_ITEM[] CrackNames(string[] names = null, DS_NAME_FLAGS flags = DS_NAME_FLAGS.DS_NAME_NO_FLAGS, DS_NAME_FORMAT formatOffered = DS_NAME_FORMAT.DS_UNKNOWN_NAME, DS_NAME_FORMAT formatDesired = DS_NAME_FORMAT.DS_USER_PRINCIPAL_NAME)
			{
				IntPtr pResult;
				uint err = DsCrackNames(handle, flags, formatOffered, formatDesired, (uint)(names?.Length ?? 0), names, out pResult);
				if (err != (uint)DS_NAME_ERROR.DS_NAME_NO_ERROR)
					throw new System.ComponentModel.Win32Exception((int)err);
				try
				{
					// Next convert the returned structure to managed environment
					DS_NAME_RESULT Result = (DS_NAME_RESULT)Marshal.PtrToStructure(pResult, typeof(DS_NAME_RESULT));
					return Result.Items;
				}
				finally
				{
					DsFreeNameResult(pResult);
				}
			}

			public void Dispose()
			{
				uint ret = DsUnBind(ref handle);
				System.Diagnostics.Debug.WriteLineIf(ret != 0, "Error unbinding :\t" + ret.ToString());
			}
		}

		[DllImport(NTDSAPI, CharSet = CharSet.Auto, PreserveSig = false)]
		public static extern void DsBind(
			string DomainControllerName, // in, optional
			string DnsDomainName, // in, optional
			out IntPtr phDS);

		[DllImport(NTDSAPI, CharSet = CharSet.Auto)]
		public static extern uint DsCrackNames(
			IntPtr hDS,
			DS_NAME_FLAGS flags,
			DS_NAME_FORMAT formatOffered,
			DS_NAME_FORMAT formatDesired,
			uint cNames,
			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)] string[] rpNames,
			out IntPtr ppResult);

		[DllImport(NTDSAPI, CharSet = CharSet.Auto)]
		public static extern void DsFreeNameResult(IntPtr pResult /* DS_NAME_RESULT* */);

		[DllImport(NTDSAPI, CharSet = CharSet.Auto)]
		public static extern uint DsUnBind(ref IntPtr phDS);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct DS_NAME_RESULT
		{
			public uint cItems;
			internal IntPtr rItems; // PDS_NAME_RESULT_ITEM

			public DS_NAME_RESULT_ITEM[] Items
			{
				get
				{
					if (rItems == IntPtr.Zero)
						return new DS_NAME_RESULT_ITEM[0];
					var ResultArray = new DS_NAME_RESULT_ITEM[cItems];
					Type strType = typeof(DS_NAME_RESULT_ITEM);
					int stSize = Marshal.SizeOf(strType);
					IntPtr curptr;
					for (uint i = 0; i < cItems; i++)
					{
						curptr = new IntPtr(rItems.ToInt64() + (i * stSize));
						ResultArray[i] = (DS_NAME_RESULT_ITEM)Marshal.PtrToStructure(curptr, strType);
					}
					return ResultArray;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct DS_NAME_RESULT_ITEM
		{
			public DS_NAME_ERROR status;
			public string pDomain;
			public string pName;

			public override string ToString()
			{
				if (status == DS_NAME_ERROR.DS_NAME_NO_ERROR)
					return pName;
				return string.Empty;
			}
		}
	}
}