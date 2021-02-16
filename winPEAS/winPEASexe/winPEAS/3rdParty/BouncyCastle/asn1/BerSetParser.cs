namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class BerSetParser
		: Asn1SetParser
	{
		private readonly Asn1StreamParser _parser;

		internal BerSetParser(
			Asn1StreamParser parser)
		{
			this._parser = parser;
		}

		public IAsn1Convertible ReadObject()
		{
			return _parser.ReadObject();
		}

		public Asn1Object ToAsn1Object()
		{
			return new BerSet(_parser.ReadVector(), false);
		}
	}
}
