using System;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    /**
   * Der T61String (also the teletex string) - 8-bit characters
   */
    public class DerT61String
        : DerStringBase
    {
        private readonly string str;

        /**
         * return a T61 string from the passed in object.
         *
         * @exception ArgumentException if the object cannot be converted.
         */
        public static DerT61String GetInstance(
            object obj)
        {
            if (obj == null || obj is DerT61String)
            {
                return (DerT61String)obj;
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
         * return an T61 string from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */
        public static DerT61String GetInstance(
            Asn1TaggedObject obj,
            bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerT61String)
            {
                return GetInstance(o);
            }

            return new DerT61String(Asn1OctetString.GetInstance(o).GetOctets());
        }

        /**
         * basic constructor - with bytes.
         */
        public DerT61String(
            byte[] str)
            : this(Strings.FromByteArray(str))
        {
        }

        /**
         * basic constructor - with string.
         */
        public DerT61String(
            string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            this.str = str;
        }

        public override string GetString()
        {
            return str;
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.T61String, GetOctets());
        }

        public byte[] GetOctets()
        {
            return Strings.ToByteArray(str);
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerT61String other = asn1Object as DerT61String;

            if (other == null)
                return false;

            return this.str.Equals(other.str);
        }
    }
}
