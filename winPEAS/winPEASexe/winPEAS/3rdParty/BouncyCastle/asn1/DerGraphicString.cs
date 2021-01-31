using System;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public class DerGraphicString
        : DerStringBase
    {
        private readonly byte[] mString;

        /**
         * return a Graphic String from the passed in object
         *
         * @param obj a DerGraphicString or an object that can be converted into one.
         * @exception IllegalArgumentException if the object cannot be converted.
         * @return a DerGraphicString instance, or null.
         */
        public static DerGraphicString GetInstance(object obj)
        {
            if (obj == null || obj is DerGraphicString)
            {
                return (DerGraphicString)obj;
            }

            if (obj is byte[])
            {
                try
                {
                    return (DerGraphicString)FromByteArray((byte[])obj);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("encoding error in GetInstance: " + e.ToString(), "obj");
                }
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj), "obj");
        }

        /**
         * return a Graphic String from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicit true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception IllegalArgumentException if the tagged object cannot
         *               be converted.
         * @return a DerGraphicString instance, or null.
         */
        public static DerGraphicString GetInstance(Asn1TaggedObject obj, bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerGraphicString)
            {
                return GetInstance(o);
            }

            return new DerGraphicString(((Asn1OctetString)o).GetOctets());
        }

        /**
         * basic constructor - with bytes.
         * @param string the byte encoding of the characters making up the string.
         */
        public DerGraphicString(byte[] encoding)
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
            derOut.WriteEncoded(Asn1Tags.GraphicString, mString);
        }

        protected override int Asn1GetHashCode()
        {
            return Arrays.GetHashCode(mString);
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerGraphicString other = asn1Object as DerGraphicString;

            if (other == null)
                return false;

            return Arrays.AreEqual(mString, other.mString);
        }
    }
}
