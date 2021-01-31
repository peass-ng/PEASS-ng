using System;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.math;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public class DerEnumerated
       : Asn1Object
    {
        private readonly byte[] bytes;
        private readonly int start;

        /**
         * return an integer from the passed in object
         *
         * @exception ArgumentException if the object cannot be converted.
         */
        public static DerEnumerated GetInstance(
            object obj)
        {
            if (obj == null || obj is DerEnumerated)
            {
                return (DerEnumerated)obj;
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
         * return an Enumerated from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */
        public static DerEnumerated GetInstance(
            Asn1TaggedObject obj,
            bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerEnumerated)
            {
                return GetInstance(o);
            }

            return FromOctetString(((Asn1OctetString)o).GetOctets());
        }

        public DerEnumerated(int val)
        {
            if (val < 0)
                throw new ArgumentException("enumerated must be non-negative", "val");

            this.bytes = BigInteger.ValueOf(val).ToByteArray();
            this.start = 0;
        }

        public DerEnumerated(long val)
        {
            if (val < 0L)
                throw new ArgumentException("enumerated must be non-negative", "val");

            this.bytes = BigInteger.ValueOf(val).ToByteArray();
            this.start = 0;
        }

        public DerEnumerated(BigInteger val)
        {
            if (val.SignValue < 0)
                throw new ArgumentException("enumerated must be non-negative", "val");

            this.bytes = val.ToByteArray();
            this.start = 0;
        }

        public DerEnumerated(byte[] bytes)
        {
            if (DerInteger.IsMalformed(bytes))
                throw new ArgumentException("malformed enumerated", "bytes");
            if (0 != (bytes[0] & 0x80))
                throw new ArgumentException("enumerated must be non-negative", "bytes");

            this.bytes = Arrays.Clone(bytes);
            this.start = DerInteger.SignBytesToSkip(bytes);
        }

        public BigInteger Value
        {
            get { return new BigInteger(bytes); }
        }

        public bool HasValue(BigInteger x)
        {
            return null != x
                // Fast check to avoid allocation
                && DerInteger.IntValue(bytes, start, DerInteger.SignExtSigned) == x.IntValue
                && Value.Equals(x);
        }

        public int IntValueExact
        {
            get
            {
                int count = bytes.Length - start;
                if (count > 4)
                    throw new ArithmeticException("ASN.1 Enumerated out of int range");

                return DerInteger.IntValue(bytes, start, DerInteger.SignExtSigned);
            }
        }

        internal override void Encode(DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.Enumerated, bytes);
        }

        protected override bool Asn1Equals(Asn1Object asn1Object)
        {
            DerEnumerated other = asn1Object as DerEnumerated;
            if (other == null)
                return false;

            return Arrays.AreEqual(this.bytes, other.bytes);
        }

        protected override int Asn1GetHashCode()
        {
            return Arrays.GetHashCode(bytes);
        }

        private static readonly DerEnumerated[] cache = new DerEnumerated[12];

        internal static DerEnumerated FromOctetString(byte[] enc)
        {
            if (enc.Length > 1)
                return new DerEnumerated(enc);
            if (enc.Length == 0)
                throw new ArgumentException("ENUMERATED has zero length", "enc");

            int value = enc[0];
            if (value >= cache.Length)
                return new DerEnumerated(enc);

            DerEnumerated possibleMatch = cache[value];
            if (possibleMatch == null)
            {
                cache[value] = possibleMatch = new DerEnumerated(enc);
            }
            return possibleMatch;
        }
    }
}
