using System;

namespace winPEAS._3rdParty.BouncyCastle.crypto
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
    [Serializable]
#endif
    public class OutputLengthException
        : DataLengthException
    {
        public OutputLengthException()
        {
        }

        public OutputLengthException(
            string message)
            : base(message)
        {
        }

        public OutputLengthException(
            string message,
            Exception exception)
            : base(message, exception)
        {
        }
    }
}
