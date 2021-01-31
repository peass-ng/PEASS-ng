using System;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
	[Serializable]
#endif
	public class Asn1ParsingException
		: InvalidOperationException
	{
		public Asn1ParsingException()
			: base()
		{
		}

		public Asn1ParsingException(
			string message)
			: base(message)
		{
		}

		public Asn1ParsingException(
			string message,
			Exception exception)
			: base(message, exception)
		{
		}
	}
}
