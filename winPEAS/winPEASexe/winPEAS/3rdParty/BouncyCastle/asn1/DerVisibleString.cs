using System;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    /**
    * Der VisibleString object.
    */
    public class DerVisibleString
        : DerStringBase
    {
        private readonly string str;

        /**
         * return a Visible string from the passed in object.
         *
         * @exception ArgumentException if the object cannot be converted.
         */
        public static DerVisibleString GetInstance(
            object obj)
        {
            if (obj == null || obj is DerVisibleString)
            {
                return (DerVisibleString)obj;
            }

            if (obj is Asn1OctetString)
            {
                return new DerVisibleString(((Asn1OctetString)obj).GetOctets());
            }

            if (obj is Asn1TaggedObject)
            {
                return GetInstance(((Asn1TaggedObject)obj).GetObject());
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
         * return a Visible string from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */
        public static DerVisibleString GetInstance(
            Asn1TaggedObject obj,
            bool explicitly)
        {
            return GetInstance(obj.GetObject());
        }

        /**
         * basic constructor - byte encoded string.
         */
        public DerVisibleString(
            byte[] str)
            : this(Strings.FromAsciiByteArray(str))
        {
        }

        /**
         * basic constructor
         */
        public DerVisibleString(
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

        public byte[] GetOctets()
        {
            return Strings.ToAsciiByteArray(str);
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.VisibleString, GetOctets());
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerVisibleString other = asn1Object as DerVisibleString;

            if (other == null)
                return false;

            return this.str.Equals(other.str);
        }

        protected override int Asn1GetHashCode()
        {
            return this.str.GetHashCode();
        }
    }
}
