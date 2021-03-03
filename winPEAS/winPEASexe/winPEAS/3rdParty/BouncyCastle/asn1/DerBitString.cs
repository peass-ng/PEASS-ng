using System;
using System.Diagnostics;
using System.Text;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.math;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public class DerBitString
     : DerStringBase
    {
        private static readonly char[] table
            = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        protected readonly byte[] mData;
        protected readonly int mPadBits;

        /**
		 * return a Bit string from the passed in object
		 *
		 * @exception ArgumentException if the object cannot be converted.
		 */
        public static DerBitString GetInstance(
            object obj)
        {
            if (obj == null || obj is DerBitString)
            {
                return (DerBitString)obj;
            }
            if (obj is byte[])
            {
                try
                {
                    return (DerBitString)FromByteArray((byte[])obj);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("encoding error in GetInstance: " + e.ToString());
                }
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
		 * return a Bit string from a tagged object.
		 *
		 * @param obj the tagged object holding the object we want
		 * @param explicitly true if the object is meant to be explicitly
		 *              tagged false otherwise.
		 * @exception ArgumentException if the tagged object cannot
		 *               be converted.
		 */
        public static DerBitString GetInstance(
            Asn1TaggedObject obj,
            bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerBitString)
            {
                return GetInstance(o);
            }

            return FromAsn1Octets(((Asn1OctetString)o).GetOctets());
        }

        /**
		 * @param data the octets making up the bit string.
		 * @param padBits the number of extra bits at the end of the string.
		 */
        public DerBitString(
            byte[] data,
            int padBits)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (padBits < 0 || padBits > 7)
                throw new ArgumentException("must be in the range 0 to 7", "padBits");
            if (data.Length == 0 && padBits != 0)
                throw new ArgumentException("if 'data' is empty, 'padBits' must be 0");

            this.mData = Arrays.Clone(data);
            this.mPadBits = padBits;
        }

        public DerBitString(
            byte[] data)
            : this(data, 0)
        {
        }

        public DerBitString(
            int namedBits)
        {
            if (namedBits == 0)
            {
                this.mData = new byte[0];
                this.mPadBits = 0;
                return;
            }

            int bits = BigInteger.BitLen(namedBits);
            int bytes = (bits + 7) / 8;

            Debug.Assert(0 < bytes && bytes <= 4);

            byte[] data = new byte[bytes];
            --bytes;

            for (int i = 0; i < bytes; i++)
            {
                data[i] = (byte)namedBits;
                namedBits >>= 8;
            }

            Debug.Assert((namedBits & 0xFF) != 0);

            data[bytes] = (byte)namedBits;

            int padBits = 0;
            while ((namedBits & (1 << padBits)) == 0)
            {
                ++padBits;
            }

            Debug.Assert(padBits < 8);

            this.mData = data;
            this.mPadBits = padBits;
        }

        public DerBitString(
            Asn1Encodable obj)
            : this(obj.GetDerEncoded())
        {
        }

        /**
         * Return the octets contained in this BIT STRING, checking that this BIT STRING really
         * does represent an octet aligned string. Only use this method when the standard you are
         * following dictates that the BIT STRING will be octet aligned.
         *
         * @return a copy of the octet aligned data.
         */
        public virtual byte[] GetOctets()
        {
            if (mPadBits != 0)
                throw new InvalidOperationException("attempt to get non-octet aligned data from BIT STRING");

            return Arrays.Clone(mData);
        }

        public virtual byte[] GetBytes()
        {
            byte[] data = Arrays.Clone(mData);

            // DER requires pad bits be zero
            if (mPadBits > 0)
            {
                data[data.Length - 1] &= (byte)(0xFF << mPadBits);
            }

            return data;
        }

        public virtual int PadBits
        {
            get { return mPadBits; }
        }

        /**
		 * @return the value of the bit string as an int (truncating if necessary)
		 */
        public virtual int IntValue
        {
            get
            {
                int value = 0, length = System.Math.Min(4, mData.Length);
                for (int i = 0; i < length; ++i)
                {
                    value |= (int)mData[i] << (8 * i);
                }
                if (mPadBits > 0 && length == mData.Length)
                {
                    int mask = (1 << mPadBits) - 1;
                    value &= ~(mask << (8 * (length - 1)));
                }
                return value;
            }
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            if (mPadBits > 0)
            {
                int last = mData[mData.Length - 1];
                int mask = (1 << mPadBits) - 1;
                int unusedBits = last & mask;

                if (unusedBits != 0)
                {
                    byte[] contents = Arrays.Prepend(mData, (byte)mPadBits);

                    /*
                     * X.690-0207 11.2.1: Each unused bit in the final octet of the encoding of a bit string value shall be set to zero.
                     */
                    contents[contents.Length - 1] = (byte)(last ^ unusedBits);

                    derOut.WriteEncoded(Asn1Tags.BitString, contents);
                    return;
                }
            }

            derOut.WriteEncoded(Asn1Tags.BitString, (byte)mPadBits, mData);
        }

        protected override int Asn1GetHashCode()
        {
            return mPadBits.GetHashCode() ^ Arrays.GetHashCode(mData);
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerBitString other = asn1Object as DerBitString;

            if (other == null)
                return false;

            return this.mPadBits == other.mPadBits
                && Arrays.AreEqual(this.mData, other.mData);
        }

        public override string GetString()
        {
            StringBuilder buffer = new StringBuilder("#");

            byte[] str = GetDerEncoded();

            for (int i = 0; i != str.Length; i++)
            {
                uint ubyte = str[i];
                buffer.Append(table[(ubyte >> 4) & 0xf]);
                buffer.Append(table[str[i] & 0xf]);
            }

            return buffer.ToString();
        }

        internal static DerBitString FromAsn1Octets(byte[] octets)
        {
            if (octets.Length < 1)
                throw new ArgumentException("truncated BIT STRING detected", "octets");

            int padBits = octets[0];
            byte[] data = Arrays.CopyOfRange(octets, 1, octets.Length);

            if (padBits > 0 && padBits < 8 && data.Length > 0)
            {
                int last = data[data.Length - 1];
                int mask = (1 << padBits) - 1;

                if ((last & mask) != 0)
                {
                    return new BerBitString(data, padBits);
                }
            }

            return new DerBitString(data, padBits);
        }
    }
}
