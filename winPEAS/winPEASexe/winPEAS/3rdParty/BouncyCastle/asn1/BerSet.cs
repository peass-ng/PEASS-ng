namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    public class BerSet
       : DerSet
    {
        public static new readonly BerSet Empty = new BerSet();

        public static new BerSet FromVector(Asn1EncodableVector elementVector)
        {
            return elementVector.Count < 1 ? Empty : new BerSet(elementVector);
        }

        internal static new BerSet FromVector(Asn1EncodableVector elementVector, bool needsSorting)
        {
            return elementVector.Count < 1 ? Empty : new BerSet(elementVector, needsSorting);
        }

        /**
         * create an empty sequence
         */
        public BerSet()
            : base()
        {
        }

        /**
         * create a set containing one object
         */
        public BerSet(Asn1Encodable element)
            : base(element)
        {
        }

        /**
         * create a set containing a vector of objects.
         */
        public BerSet(Asn1EncodableVector elementVector)
            : base(elementVector, false)
        {
        }

        internal BerSet(Asn1EncodableVector elementVector, bool needsSorting)
            : base(elementVector, needsSorting)
        {
        }

        internal override void Encode(DerOutputStream derOut)
        {
            if (derOut is Asn1OutputStream || derOut is BerOutputStream)
            {
                derOut.WriteByte(Asn1Tags.Set | Asn1Tags.Constructed);
                derOut.WriteByte(0x80);

                foreach (Asn1Encodable o in this)
                {
                    derOut.WriteObject(o);
                }

                derOut.WriteByte(0x00);
                derOut.WriteByte(0x00);
            }
            else
            {
                base.Encode(derOut);
            }
        }
    }
}
