namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class BerSequence
		 : DerSequence
	{
		public static new readonly BerSequence Empty = new BerSequence();

		public static new BerSequence FromVector(Asn1EncodableVector elementVector)
		{
			return elementVector.Count < 1 ? Empty : new BerSequence(elementVector);
		}

		/**
		 * create an empty sequence
		 */
		public BerSequence()
			: base()
		{
		}

		/**
		 * create a sequence containing one object
		 */
		public BerSequence(Asn1Encodable element)
			: base(element)
		{
		}

		public BerSequence(params Asn1Encodable[] elements)
			: base(elements)
		{
		}

		/**
		 * create a sequence containing a vector of objects.
		 */
		public BerSequence(Asn1EncodableVector elementVector)
			: base(elementVector)
		{
		}

		internal override void Encode(DerOutputStream derOut)
		{
			if (derOut is Asn1OutputStream || derOut is BerOutputStream)
			{
				derOut.WriteByte(Asn1Tags.Sequence | Asn1Tags.Constructed);
				derOut.WriteByte(0x80);

				foreach (Asn1Encodable o in this)
				{
					derOut.WriteObject(o);
				}

				derOut.WriteByte(0x00);
				derOut.WriteByte(0x00);
			}
			else
			{
				base.Encode(derOut);
			}
		}
	}
}
