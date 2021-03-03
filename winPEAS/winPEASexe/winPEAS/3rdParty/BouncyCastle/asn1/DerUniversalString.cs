using System;
using System.Text;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    /**
   * Der UniversalString object.
   */
    public class DerUniversalString
        : DerStringBase
    {
        private static readonly char[] table = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private readonly byte[] str;

        /**
         * return a Universal string from the passed in object.
         *
         * @exception ArgumentException if the object cannot be converted.
         */
        public static DerUniversalString GetInstance(
            object obj)
        {
            if (obj == null || obj is DerUniversalString)
            {
                return (DerUniversalString)obj;
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
         * return a Universal string from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */
        public static DerUniversalString GetInstance(
            Asn1TaggedObject obj,
            bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is DerUniversalString)
            {
                return GetInstance(o);
            }

            return new DerUniversalString(Asn1OctetString.GetInstance(o).GetOctets());
        }

        /**
         * basic constructor - byte encoded string.
         */
        public DerUniversalString(
            byte[] str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            this.str = str;
        }

        public override string GetString()
        {
            StringBuilder buffer = new StringBuilder("#");
            byte[] enc = GetDerEncoded();

            for (int i = 0; i != enc.Length; i++)
            {
                uint ubyte = enc[i];
                buffer.Append(table[(ubyte >> 4) & 0xf]);
                buffer.Append(table[enc[i] & 0xf]);
            }

            return buffer.ToString();
        }

        public byte[] GetOctets()
        {
            return (byte[])str.Clone();
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.UniversalString, this.str);
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerUniversalString other = asn1Object as DerUniversalString;

            if (other == null)
                return false;

            //			return this.GetString().Equals(other.GetString());
            return Arrays.AreEqual(this.str, other.str);
        }
    }
}
