using System;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public class DerVideotexString
       : DerStringBase
    {
        private readonly byte[] mString;

        /**
         * return a Videotex String from the passed in object
         *
         * @param obj a DERVideotexString or an object that can be converted into one.
         * @exception IllegalArgumentException if the object cannot be converted.
         * @return a DERVideotexString instance, or null.
         */
        public static DerVideotexString GetInstance(object obj)
        {
            if (obj == null || obj is DerVideotexString)
            {
                return (DerVideotexString)obj;
            }

            if (obj is byte[])
            {
                try
                {
                    return (DerVideotexString)FromByteArray((byte[])obj);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("encoding error in GetInstance: " + e.ToString(), "obj");
                }
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj), "obj");
        }

        /**
         * return a Videotex String from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicit true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception IllegalArgumentException if the tagged object cannot
         *               be converted.
         * @return a DERVideotexString instance, or null.
         */
        public static DerVideotexString GetInstance(Asn1TaggedObject obj, bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerVideotexString)
            {
                return GetInstance(o);
            }

            return new DerVideotexString(((Asn1OctetString)o).GetOctets());
        }

        /**
         * basic constructor - with bytes.
         * @param string the byte encoding of the characters making up the string.
         */
        public DerVideotexString(byte[] encoding)
        {
            this.mString = Arrays.Clone(encoding);
        }

        public override string GetString()
        {
            return Strings.FromByteArray(mString);
        }

        public byte[] GetOctets()
        {
            return Arrays.Clone(mString);
        }

        internal override void Encode(DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.VideotexString, mString);
        }

        protected override int Asn1GetHashCode()
        {
            return Arrays.GetHashCode(mString);
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerVideotexString other = asn1Object as DerVideotexString;

            if (other == null)
                return false;

            return Arrays.AreEqual(mString, other.mString);
        }
    }
}
