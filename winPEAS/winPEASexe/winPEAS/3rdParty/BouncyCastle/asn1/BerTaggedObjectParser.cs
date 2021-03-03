using System;
using System.IO;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class BerTaggedObjectParser
		: Asn1TaggedObjectParser
	{
		private bool _constructed;
		private int _tagNumber;
		private Asn1StreamParser _parser;

		[Obsolete]
		internal BerTaggedObjectParser(
			int baseTag,
			int tagNumber,
			Stream contentStream)
			: this((baseTag & Asn1Tags.Constructed) != 0, tagNumber, new Asn1StreamParser(contentStream))
		{
		}

		internal BerTaggedObjectParser(
			bool constructed,
			int tagNumber,
			Asn1StreamParser parser)
		{
			_constructed = constructed;
			_tagNumber = tagNumber;
			_parser = parser;
		}

		public bool IsConstructed
		{
			get { return _constructed; }
		}

		public int TagNo
		{
			get { return _tagNumber; }
		}

		public IAsn1Convertible GetObjectParser(
			int tag,
			bool isExplicit)
		{
			if (isExplicit)
			{
				if (!_constructed)
					throw new IOException("Explicit tags must be constructed (see X.690 8.14.2)");

				return _parser.ReadObject();
			}

			return _parser.ReadImplicit(_constructed, tag);
		}

		public Asn1Object ToAsn1Object()
		{
			try
			{
				return _parser.ReadTaggedObject(_constructed, _tagNumber);
			}
			catch (IOException e)
			{
				throw new Asn1ParsingException(e.Message);
			}
		}
	}
}
