using System;
using System.IO;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	/**
	* Base class for an application specific object
	*/
	public class DerApplicationSpecific
		: Asn1Object
	{
		private readonly bool isConstructed;
		private readonly int tag;
		private readonly byte[] octets;

		internal DerApplicationSpecific(
			bool isConstructed,
			int tag,
			byte[] octets)
		{
			this.isConstructed = isConstructed;
			this.tag = tag;
			this.octets = octets;
		}

		public DerApplicationSpecific(
			int tag,
			byte[] octets)
			: this(false, tag, octets)
		{
		}

		public DerApplicationSpecific(
			int tag,
			Asn1Encodable obj)
			: this(true, tag, obj)
		{
		}

		public DerApplicationSpecific(
			bool isExplicit,
			int tag,
			Asn1Encodable obj)
		{
			Asn1Object asn1Obj = obj.ToAsn1Object();

			byte[] data = asn1Obj.GetDerEncoded();

			this.isConstructed = Asn1TaggedObject.IsConstructed(isExplicit, asn1Obj);
			this.tag = tag;

			if (isExplicit)
			{
				this.octets = data;
			}
			else
			{
				int lenBytes = GetLengthOfHeader(data);
				byte[] tmp = new byte[data.Length - lenBytes];
				Array.Copy(data, lenBytes, tmp, 0, tmp.Length);
				this.octets = tmp;
			}
		}

		public DerApplicationSpecific(
			int tagNo,
			Asn1EncodableVector vec)
		{
			this.tag = tagNo;
			this.isConstructed = true;
			MemoryStream bOut = new MemoryStream();

			for (int i = 0; i != vec.Count; i++)
			{
				try
				{
					byte[] bs = vec[i].GetDerEncoded();
					bOut.Write(bs, 0, bs.Length);
				}
				catch (IOException e)
				{
					throw new InvalidOperationException("malformed object", e);
				}
			}
			this.octets = bOut.ToArray();
		}

		private int GetLengthOfHeader(
			byte[] data)
		{
			int length = data[1]; // TODO: assumes 1 byte tag

			if (length == 0x80)
			{
				return 2;      // indefinite-length encoding
			}

			if (length > 127)
			{
				int size = length & 0x7f;

				// Note: The invalid long form "0xff" (see X.690 8.1.3.5c) will be caught here
				if (size > 4)
				{
					throw new InvalidOperationException("DER length more than 4 bytes: " + size);
				}

				return size + 2;
			}

			return 2;
		}

		public bool IsConstructed()
		{
			return isConstructed;
		}

		public byte[] GetContents()
		{
			return octets;
		}

		public int ApplicationTag
		{
			get { return tag; }
		}

		/**
		 * Return the enclosed object assuming explicit tagging.
		 *
		 * @return  the resulting object
		 * @throws IOException if reconstruction fails.
		 */
		public Asn1Object GetObject()
		{
			return FromByteArray(GetContents());
		}

		/**
		 * Return the enclosed object assuming implicit tagging.
		 *
		 * @param derTagNo the type tag that should be applied to the object's contents.
		 * @return  the resulting object
		 * @throws IOException if reconstruction fails.
		 */
		public Asn1Object GetObject(
			int derTagNo)
		{
			if (derTagNo >= 0x1f)
				throw new IOException("unsupported tag number");

			byte[] orig = this.GetEncoded();
			byte[] tmp = ReplaceTagNumber(derTagNo, orig);

			if ((orig[0] & Asn1Tags.Constructed) != 0)
			{
				tmp[0] |= Asn1Tags.Constructed;
			}

			return FromByteArray(tmp);
		}

		internal override void Encode(
			DerOutputStream derOut)
		{
			int classBits = Asn1Tags.Application;
			if (isConstructed)
			{
				classBits |= Asn1Tags.Constructed;
			}

			derOut.WriteEncoded(classBits, tag, octets);
		}

		protected override bool Asn1Equals(
			Asn1Object asn1Object)
		{
			DerApplicationSpecific other = asn1Object as DerApplicationSpecific;

			if (other == null)
				return false;

			return this.isConstructed == other.isConstructed
				&& this.tag == other.tag
				&& Arrays.AreEqual(this.octets, other.octets);
		}

		protected override int Asn1GetHashCode()
		{
			return isConstructed.GetHashCode() ^ tag.GetHashCode() ^ Arrays.GetHashCode(octets);
		}

		private byte[] ReplaceTagNumber(
			int newTag,
			byte[] input)
		{
			int tagNo = input[0] & 0x1f;
			int index = 1;

			// with tagged object tag number is bottom 5 bits, or stored at the start of the content
			if (tagNo == 0x1f)
			{
				int b = input[index++];

				// X.690-0207 8.1.2.4.2
				// "c) bits 7 to 1 of the first subsequent octet shall not all be zero."
				if ((b & 0x7f) == 0) // Note: -1 will pass
					throw new IOException("corrupted stream - invalid high tag number found");

				while ((b & 0x80) != 0)
				{
					b = input[index++];
				}
			}

			int remaining = input.Length - index;
			byte[] tmp = new byte[1 + remaining];
			tmp[0] = (byte)newTag;
			Array.Copy(input, index, tmp, 1, remaining);
			return tmp;
		}
	}
}
