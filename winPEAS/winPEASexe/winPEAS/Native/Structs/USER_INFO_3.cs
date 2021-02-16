using System;
using System.Runtime.InteropServices;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct USER_INFO_3
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string name;
        [MarshalAs(UnmanagedType.LPWStr)] public string password;
        public uint passwordAge;
        public uint priv;
        [MarshalAs(UnmanagedType.LPWStr)] public string home_dir;
        [MarshalAs(UnmanagedType.LPWStr)] public string comment;
        public uint flags;
        [MarshalAs(UnmanagedType.LPWStr)] public string script_path;
        public uint auth_flags;
        [MarshalAs(UnmanagedType.LPWStr)] public string full_name;
        [MarshalAs(UnmanagedType.LPWStr)] public string usr_comment;
        [MarshalAs(UnmanagedType.LPWStr)] public string parms;
        [MarshalAs(UnmanagedType.LPWStr)] public string workstations;
        public uint last_logon;
        public uint last_logoff;
        public uint acct_expires;
        public uint max_storage;
        public uint units_per_week;
        public IntPtr logon_hours;
        public uint bad_pw_count;
        public uint num_logons;
        [MarshalAs(UnmanagedType.LPWStr)] public string logon_server;
        public uint country_code;
        public uint code_page;
        public uint user_id;
        public uint primary_group_id;
        [MarshalAs(UnmanagedType.LPWStr)] public string profile;
        [MarshalAs(UnmanagedType.LPWStr)] public string home_dir_drive;
        public uint password_expired;
    }
}
