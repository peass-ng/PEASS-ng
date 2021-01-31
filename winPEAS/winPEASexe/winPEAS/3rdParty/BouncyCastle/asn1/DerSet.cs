using System.IO;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	/**
	   * A Der encoded set object
	   */
	public class DerSet
		: Asn1Set
	{
		public static readonly DerSet Empty = new DerSet();

		public static DerSet FromVector(Asn1EncodableVector elementVector)
		{
			return elementVector.Count < 1 ? Empty : new DerSet(elementVector);
		}

		internal static DerSet FromVector(Asn1EncodableVector elementVector, bool needsSorting)
		{
			return elementVector.Count < 1 ? Empty : new DerSet(elementVector, needsSorting);
		}

		/**
		 * create an empty set
		 */
		public DerSet()
			: base()
		{
		}

		/**
		 * @param obj - a single object that makes up the set.
		 */
		public DerSet(Asn1Encodable element)
			: base(element)
		{
		}

		public DerSet(params Asn1Encodable[] elements)
			: base(elements)
		{
			Sort();
		}

		/**
		 * @param v - a vector of objects making up the set.
		 */
		public DerSet(Asn1EncodableVector elementVector)
			: this(elementVector, true)
		{
		}

		internal DerSet(Asn1EncodableVector elementVector, bool needsSorting)
			: base(elementVector)
		{
			if (needsSorting)
			{
				Sort();
			}
		}

		/*
		 * A note on the implementation:
		 * <p>
		 * As Der requires the constructed, definite-length model to
		 * be used for structured types, this varies slightly from the
		 * ASN.1 descriptions given. Rather than just outputing Set,
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

			derOut.WriteEncoded(Asn1Tags.Set | Asn1Tags.Constructed, bytes);
		}
	}
}
