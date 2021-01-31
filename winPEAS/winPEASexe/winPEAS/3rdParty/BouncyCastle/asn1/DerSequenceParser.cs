namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class DerSequenceParser
		 : Asn1SequenceParser
	{
		private readonly Asn1StreamParser _parser;

		internal DerSequenceParser(
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
			return new DerSequence(_parser.ReadVector());
		}
	}
}
