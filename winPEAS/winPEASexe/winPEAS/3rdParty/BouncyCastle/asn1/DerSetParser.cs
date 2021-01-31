namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class DerSetParser
	: Asn1SetParser
	{
		private readonly Asn1StreamParser _parser;

		internal DerSetParser(
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
			return new DerSet(_parser.ReadVector(), false);
		}
	}
}
