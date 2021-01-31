using System;

namespace winPEAS._3rdParty.BouncyCastle.security
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
    [Serializable]
#endif
    public class SecurityUtilityException
        : Exception
    {
        /**
        * base constructor.
        */
        public SecurityUtilityException()
        {
        }

        /**
         * create a SecurityUtilityException with the given message.
         *
         * @param message the message to be carried with the exception.
         */
        public SecurityUtilityException(
            string message)
            : base(message)
        {
        }

        public SecurityUtilityException(
            string message,
            Exception exception)
            : base(message, exception)
        {
        }
    }
}
