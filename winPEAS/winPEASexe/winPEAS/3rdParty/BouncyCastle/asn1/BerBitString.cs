using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.asn1
{
    public class BerBitString
       : DerBitString
    {
        public BerBitString(byte[] data, int padBits)
            : base(data, padBits)
        {
        }

        public BerBitString(byte[] data)
            : base(data)
        {
        }

        public BerBitString(int namedBits)
            : base(namedBits)
        {
        }

        public BerBitString(Asn1Encodable obj)
            : base(obj)
        {
        }

        internal override void Encode(
            DerOutputStream derOut)
        {
            if (derOut is Asn1OutputStream || derOut is BerOutputStream)
            {
                derOut.WriteEncoded(Asn1Tags.BitString, (byte)mPadBits, mData);
            }
            else
            {
                base.Encode(derOut);
            }
        }
    }
}
