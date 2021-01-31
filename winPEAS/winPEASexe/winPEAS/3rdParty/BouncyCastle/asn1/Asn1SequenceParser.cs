namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public interface Asn1SequenceParser
		: IAsn1Convertible
	{
		IAsn1Convertible ReadObject();
	}
}
