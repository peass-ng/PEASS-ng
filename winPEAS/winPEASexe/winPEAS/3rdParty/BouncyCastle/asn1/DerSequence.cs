using System.IO;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class DerSequence
	 : Asn1Sequence
	{
		public static readonly DerSequence Empty = new DerSequence();

		public static DerSequence FromVector(Asn1EncodableVector elementVector)
		{
			return elementVector.Count < 1 ? Empty : new DerSequence(elementVector);
		}

		/**
		 * create an empty sequence
		 */
		public DerSequence()
			: base()
		{
		}

		/**
		 * create a sequence containing one object
		 */
		public DerSequence(Asn1Encodable element)
			: base(element)
		{
		}

		public DerSequence(params Asn1Encodable[] elements)
			: base(elements)
		{
		}

		/**
		 * create a sequence containing a vector of objects.
		 */
		public DerSequence(Asn1EncodableVector elementVector)
			: base(elementVector)
		{
		}

		/*
		 * A note on the implementation:
		 * <p>
		 * As Der requires the constructed, definite-length model to
		 * be used for structured types, this varies slightly from the
		 * ASN.1 descriptions given. Rather than just outputing Sequence,
		 * we also have to specify Constructed, and the objects length.
		 */
		internal override void Encode(DerOutputStream derOut)
		{
			// TODO Intermediate buffer could be avoided if we could calculate expected length
			MemoryStream bOut = new MemoryStream();
			DerOutputStream dOut = new DerOutputStream(bOut);

			foreach (Asn1Encodable obj in this)
			{
				dOut.WriteObject(obj);
			}

			Platform.Dispose(dOut);

			byte[] bytes = bOut.ToArray();

			derOut.WriteEncoded(Asn1Tags.Sequence | Asn1Tags.Constructed, bytes);
		}
	}
}
