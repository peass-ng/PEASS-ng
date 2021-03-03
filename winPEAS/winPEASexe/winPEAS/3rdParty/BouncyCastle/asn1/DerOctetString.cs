namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public class DerOctetString
       : Asn1OctetString
    {
        /// <param name="str">The octets making up the octet string.</param>
        public DerOctetString(
            byte[] str)
            : base(str)
        {
        }

        public DerOctetString(IAsn1Convertible obj)
            : this(obj.ToAsn1Object())
        {
        }

        public DerOctetString(Asn1Encodable obj)
            : base(obj.GetEncoded(Asn1Encodable.Der))
        {
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            derOut.WriteEncoded(Asn1Tags.OctetString, str);
        }

        internal static void Encode(
            DerOutputStream derOut,
            byte[] bytes,
            int offset,
            int length)
        {
            derOut.WriteEncoded(Asn1Tags.OctetString, bytes, offset, length);
        }
    }
}
