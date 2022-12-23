using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using winPEAS.Native;
using winPEAS.Native.Structs;

namespace winPEAS.Info.UserInfo.Tenant
{
    internal class Tenant
    {
        public static TenantInfo GetTenantInfo()
        {
            //original code from https://github.com/ThomasKur/WPNinjas.Dsregcmd/blob/2cff7b273ad4d3fc705744f76c4bd0701b2c36f0/WPNinjas.Dsregcmd/DsRegCmd.cs

            string tenantId = null;
            var retValue = Netapi32.NetGetAadJoinInformation(tenantId, out var ptrJoinInfo);
            if (retValue == 0)
            {
                var joinInfo = (DSREG_JOIN_INFO)Marshal.PtrToStructure(ptrJoinInfo, typeof(DSREG_JOIN_INFO));
                var jType = (JoinType)joinInfo.joinType;
                var did = new Guid(joinInfo.DeviceId);
                var tid = new Guid(joinInfo.TenantId);

                var data = Convert.FromBase64String(joinInfo.UserSettingSyncUrl);
                var userSettingSyncUrl = Encoding.ASCII.GetString(data);
                var ptrUserInfo = joinInfo.pUserInfo;

                DSREG_USER_INFO? userInfo = null;
                var certificateResult = new List<X509Certificate2>();
                Guid? uid = null;

                if (ptrUserInfo != IntPtr.Zero)
                {
                    userInfo = (DSREG_USER_INFO)Marshal.PtrToStructure(ptrUserInfo, typeof(DSREG_USER_INFO));
                    uid = new Guid(userInfo?.UserKeyId);
                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);

                    foreach (var certificate in store.Certificates)
                    {
                        if (certificate.Subject.Equals($"CN={did}"))
                        {
                            certificateResult.Add(certificate);
                        }
                    }

                    Marshal.Release(ptrUserInfo);
                }

                Marshal.Release(ptrJoinInfo);
                Netapi32.NetFreeAadJoinInformation(ptrJoinInfo);

                return new TenantInfo(
                        jType,
                        did,
                        joinInfo.IdpDomain,
                        tid,
                        joinInfo.JoinUserEmail,
                        joinInfo.TenantDisplayName,
                        joinInfo.MdmEnrollmentUrl,
                        joinInfo.MdmTermsOfUseUrl,
                        joinInfo.MdmComplianceUrl,
                        userSettingSyncUrl,
                        certificateResult,
                        userInfo?.UserEmail,
                        uid,
                        userInfo?.UserKeyName
                    );
            }

            return null;
        }
    }
}
