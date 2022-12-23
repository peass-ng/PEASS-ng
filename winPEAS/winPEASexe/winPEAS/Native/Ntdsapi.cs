using System;
using System.Runtime.InteropServices;
using winPEAS.Native.Enums;

namespace winPEAS.Native
{
    internal class Ntdsapi
    {
        [DllImport("ntdsapi.dll", CharSet = CharSet.Auto, PreserveSig = false)]
        internal static extern void DsBind(
            string DomainControllerName, // in, optional
            string DnsDomainName, // in, optional
            out IntPtr phDS);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Auto)]
        internal static extern uint DsCrackNames(
            IntPtr hDS,
            DS_NAME_FLAGS flags,
            DS_NAME_FORMAT formatOffered,
            DS_NAME_FORMAT formatDesired,
            uint cNames,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)] string[] rpNames,
            out IntPtr ppResult);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Auto)]
        internal static extern void DsFreeNameResult(IntPtr pResult /* DS_NAME_RESULT* */);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Auto)]
        internal static extern uint DsUnBind(ref IntPtr phDS);
    }
}
