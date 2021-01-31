namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public interface Asn1TaggedObjectParser
		 : IAsn1Convertible
	{
		int TagNo { get; }

		IAsn1Convertible GetObjectParser(int tag, bool isExplicit);
	}
}
