namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    /**
    * A Null object.
    */
    public abstract class Asn1Null
        : Asn1Object
    {
        internal Asn1Null()
        {
        }

        public override string ToString()
        {
            return "NULL";
        }
    }
}
