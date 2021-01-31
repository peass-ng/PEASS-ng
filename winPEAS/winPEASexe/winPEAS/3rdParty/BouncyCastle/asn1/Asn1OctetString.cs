using System;
using System.IO;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.util.encoders;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public abstract class Asn1OctetString
       : Asn1Object, Asn1OctetStringParser
    {
        internal byte[] str;

        /**
         * return an Octet string from a tagged object.
         *
         * @param obj the tagged object holding the object we want.
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *              be converted.
         */
        public static Asn1OctetString GetInstance(
            Asn1TaggedObject obj,
            bool isExplicit)
        {
            Asn1Object o = obj.GetObject();

            if (isExplicit || o is Asn1OctetString)
            {
                return GetInstance(o);
            }

            return BerOctetString.FromSequence(Asn1Sequence.GetInstance(o));
        }

        /**
         * return an Octet string from the given object.
         *
         * @param obj the object we want converted.
         * @exception ArgumentException if the object cannot be converted.
         */
        public static Asn1OctetString GetInstance(object obj)
        {
            if (obj == null || obj is Asn1OctetString)
            {
                return (Asn1OctetString)obj;
            }
            else if (obj is byte[])
            {
                try
                {
                    return GetInstance(FromByteArray((byte[])obj));
                }
                catch (IOException e)
                {
                    throw new ArgumentException("failed to construct OCTET STRING from byte[]: " + e.Message);
                }
            }
            // TODO: this needs to be deleted in V2
            else if (obj is Asn1TaggedObject)
            {
                return GetInstance(((Asn1TaggedObject)obj).GetObject());
            }
            else if (obj is Asn1Encodable)
            {
                Asn1Object primitive = ((Asn1Encodable)obj).ToAsn1Object();

                if (primitive is Asn1OctetString)
                {
                    return (Asn1OctetString)primitive;
                }
            }

            throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
        }

        /**
         * @param string the octets making up the octet string.
         */
        internal Asn1OctetString(
            byte[] str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            this.str = str;
        }

        public Stream GetOctetStream()
        {
            return new MemoryStream(str, false);
        }

        public Asn1OctetStringParser Parser
        {
            get { return this; }
        }

        public virtual byte[] GetOctets()
        {
            return str;
        }

        protected override int Asn1GetHashCode()
        {
            return Arrays.GetHashCode(GetOctets());
        }

        protected override bool Asn1Equals(
            Asn1Object asn1Object)
        {
            DerOctetString other = asn1Object as DerOctetString;

            if (other == null)
                return false;

            return Arrays.AreEqual(GetOctets(), other.GetOctets());
        }

        public override string ToString()
        {
            return "#" + Hex.ToHexString(str);
        }
    }
}
