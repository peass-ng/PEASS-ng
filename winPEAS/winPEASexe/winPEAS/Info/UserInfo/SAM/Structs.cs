using System;
using System.Runtime.InteropServices;

namespace winPEAS.Info.UserInfo.SAM
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DOMAIN_PASSWORD_INFORMATION
    {
        public short MinPasswordLength;
        public short PasswordHistoryLength;
        public PASSWORD_PROPERTIES PasswordProperties;
        private long _maxPasswordAge;
        private long _minPasswordAge;

        public TimeSpan MaxPasswordAge
        {
            get
            {
                return -new TimeSpan(_maxPasswordAge);
            }
            set
            {
                _maxPasswordAge = value.Ticks;
            }
        }

        public TimeSpan MinPasswordAge
        {
            get
            {
                return -new TimeSpan(_minPasswordAge);
            }
            set
            {
                _minPasswordAge = value.Ticks;
            }
        }
    }
}
