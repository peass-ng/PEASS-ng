using System;
using System.IO;

namespace winPEAS._3rdParty.BouncyCastle.util.io
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
	[Serializable]
#endif
	public class StreamOverflowException
		: IOException
	{
		public StreamOverflowException()
			: base()
		{
		}

		public StreamOverflowException(
			string message)
			: base(message)
		{
		}

		public StreamOverflowException(
			string message,
			Exception exception)
			: base(message, exception)
		{
		}
	}
}
