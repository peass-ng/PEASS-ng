using System;

namespace winPEAS._3rdParty.BouncyCastle
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
    [Serializable]
#endif
    public class CryptoException
        : Exception
    {
        public CryptoException()
        {
        }

        public CryptoException(
            string message)
            : base(message)
        {
        }

        public CryptoException(
            string message,
            Exception exception)
            : base(message, exception)
        {
        }
    }
}
