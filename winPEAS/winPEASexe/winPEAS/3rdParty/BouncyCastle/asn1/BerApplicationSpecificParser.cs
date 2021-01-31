namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class BerApplicationSpecificParser
	: IAsn1ApplicationSpecificParser
	{
		private readonly int tag;
		private readonly Asn1StreamParser parser;

		internal BerApplicationSpecificParser(
			int tag,
			Asn1StreamParser parser)
		{
			this.tag = tag;
			this.parser = parser;
		}

		public IAsn1Convertible ReadObject()
		{
			return parser.ReadObject();
		}

		public Asn1Object ToAsn1Object()
		{
			return new BerApplicationSpecific(tag, parser.ReadVector());
		}
	}
}
