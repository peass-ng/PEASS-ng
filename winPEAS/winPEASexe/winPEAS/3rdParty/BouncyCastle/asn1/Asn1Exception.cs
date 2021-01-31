using System;
using System.IO;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
	[Serializable]
#endif
	public class Asn1Exception
		: IOException
	{
		public Asn1Exception()
			: base()
		{
		}

		public Asn1Exception(
			string message)
			: base(message)
		{
		}

		public Asn1Exception(
			string message,
			Exception exception)
			: base(message, exception)
		{
		}
	}
}
