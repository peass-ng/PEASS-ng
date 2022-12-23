using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace winPEAS.Helpers.AppLocker
{
    [Guid("B6FEA19E-32DD-4367-B5B7-2F5DA140E87D")]
    [TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
    [ComImport]
    public interface IAppIdPolicyHandler
    {
        // Token: 0x06000001 RID: 1
        [DispId(1)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetPolicy([MarshalAs(UnmanagedType.BStr)][In] string bstrLdapPath, [MarshalAs(UnmanagedType.BStr)][In] string bstrXmlPolicy);

        // Token: 0x06000002 RID: 2
        [DispId(2)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetPolicy([MarshalAs(UnmanagedType.BStr)][In] string bstrLdapPath);

        // Token: 0x06000003 RID: 3
        [DispId(3)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetEffectivePolicy();

        // Token: 0x06000004 RID: 4
        [DispId(4)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        int IsFileAllowed([MarshalAs(UnmanagedType.BStr)][In] string bstrXmlPolicy, [MarshalAs(UnmanagedType.BStr)][In] string bstrFilePath, [MarshalAs(UnmanagedType.BStr)][In] string bstrUserSid, out Guid pguidResponsibleRuleId);

        // Token: 0x06000005 RID: 5
        [DispId(5)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        int IsPackageAllowed([MarshalAs(UnmanagedType.BStr)][In] string bstrXmlPolicy, [MarshalAs(UnmanagedType.BStr)][In] string bstrPublisherName, [MarshalAs(UnmanagedType.BStr)][In] string bstrPackageName, [In] ulong ullPackageVersion, [MarshalAs(UnmanagedType.BStr)][In] string bstrUserSid, out Guid pguidResponsibleRuleId);
    }

    // Token: 0x02000003 RID: 3
    [CoClass(typeof(AppIdPolicyHandlerClass))]
    [Guid("B6FEA19E-32DD-4367-B5B7-2F5DA140E87D")]
    [ComImport]
    public interface AppIdPolicyHandler : IAppIdPolicyHandler
    {
    }

    // Token: 0x02000004 RID: 4
    [Guid("F1ED7D4C-F863-4DE6-A1CA-7253EFDEE1F3")]
    [ClassInterface(ClassInterfaceType.None)]
    [TypeLibType(TypeLibTypeFlags.FCanCreate)]
    [ComImport]
    public class AppIdPolicyHandlerClass : IAppIdPolicyHandler, AppIdPolicyHandler
    {

        // Token: 0x06000007 RID: 7
        [DispId(1)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public virtual extern void SetPolicy([MarshalAs(UnmanagedType.BStr)][In] string bstrLdapPath, [MarshalAs(UnmanagedType.BStr)][In] string bstrXmlPolicy);

        // Token: 0x06000008 RID: 8
        [DispId(2)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public virtual extern string GetPolicy([MarshalAs(UnmanagedType.BStr)][In] string bstrLdapPath);

        // Token: 0x06000009 RID: 9
        [DispId(3)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public virtual extern string GetEffectivePolicy();

        // Token: 0x0600000A RID: 10
        [DispId(4)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public virtual extern int IsFileAllowed([MarshalAs(UnmanagedType.BStr)][In] string bstrXmlPolicy, [MarshalAs(UnmanagedType.BStr)][In] string bstrFilePath, [MarshalAs(UnmanagedType.BStr)][In] string bstrUserSid, out Guid pguidResponsibleRuleId);

        // Token: 0x0600000B RID: 11
        [DispId(5)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public virtual extern int IsPackageAllowed([MarshalAs(UnmanagedType.BStr)][In] string bstrXmlPolicy, [MarshalAs(UnmanagedType.BStr)][In] string bstrPublisherName, [MarshalAs(UnmanagedType.BStr)][In] string bstrPackageName, [In] ulong ullPackageVersion, [MarshalAs(UnmanagedType.BStr)][In] string bstrUserSid, out Guid pguidResponsibleRuleId);
    }
}
