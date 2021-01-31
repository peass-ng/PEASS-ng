using System.IO;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public interface Asn1OctetStringParser
		 : IAsn1Convertible
	{
		Stream GetOctetStream();
	}
}
