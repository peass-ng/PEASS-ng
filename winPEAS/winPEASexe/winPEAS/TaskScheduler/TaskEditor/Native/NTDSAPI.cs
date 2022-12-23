using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.TaskScheduler.TaskEditor.Native
{
    internal static partial class NativeMethods
    {
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
                Ntdsapi.DsBind(domainControllerName, dnsDomainName, out handle);
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
                if (res == null || res.Length == 0 || res[0].status != DS_NAME_ERROR.DS_NAME_NO_ERROR)
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
                uint err = Ntdsapi.DsCrackNames(handle, flags, formatOffered, formatDesired, (uint)(names?.Length ?? 0), names, out pResult);
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
                    Ntdsapi.DsFreeNameResult(pResult);
                }
            }

            public void Dispose()
            {
                uint ret = Ntdsapi.DsUnBind(ref handle);
                System.Diagnostics.Debug.WriteLineIf(ret != 0, "Error unbinding :\t" + ret.ToString());
            }
        }



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
