using System;
using System.Text;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    /**
    * Der UTF8String object.
    */
    public class DerUtf8String
        : DerStringBase
    {
        private readonly string str;

        /**
         * return an UTF8 string from the passed in object.
         *
         * @exception ArgumentException if the object cannot be converted.
         */
        public static DerUtf8String GetInstance(
            object obj)
        {
            if (obj == null || obj is DerUtf8String)
            {
                return (DerUtf8String)obj;
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
         * return an UTF8 string from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */
        public static DerUtf8String GetInstance(
            Asn1TaggedObject obj,
            bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerUtf8String)
            {
                return GetInstance(o);
            }

            return new DerUtf8String(Asn1OctetString.GetInstance(o).GetOctets());
        }

        /**
         * basic constructor - byte encoded string.
         */
        public DerUtf8String(
            byte[] str)
            : this(Encoding.UTF8.GetString(str, 0, str.Length))
        {
        }

        /**
         * basic constructor
         */
        public DerUtf8String(
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

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerUtf8String other = asn1Object as DerUtf8String;

            if (other == null)
                return false;

            return this.str.Equals(other.str);
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.Utf8String, Encoding.UTF8.GetBytes(str));
        }
    }
}
