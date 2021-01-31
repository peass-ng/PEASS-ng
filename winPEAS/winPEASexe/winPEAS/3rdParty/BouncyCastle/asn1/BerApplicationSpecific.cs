namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class BerApplicationSpecific
		  : DerApplicationSpecific
	{
		public BerApplicationSpecific(
			int tagNo,
			Asn1EncodableVector vec)
			: base(tagNo, vec)
		{
		}
	}
}
