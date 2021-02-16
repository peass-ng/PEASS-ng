namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public interface IAsn1ApplicationSpecificParser
         : IAsn1Convertible
    {
        IAsn1Convertible ReadObject();
    }
}
