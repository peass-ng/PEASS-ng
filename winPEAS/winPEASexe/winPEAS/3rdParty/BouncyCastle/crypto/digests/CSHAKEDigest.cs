using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.crypto.digests
{
    /// <summary>
    /// Customizable SHAKE function.
    /// </summary>
    public class CShakeDigest : ShakeDigest
    {
        private static readonly byte[] padding = new byte[100];
        private readonly byte[] diff;

        /// <summary>
        /// Base constructor
        /// </summary>
        /// <param name="bitLength">bit length of the underlying SHAKE function, 128 or 256.</param>
        /// <param name="N">the function name string, note this is reserved for use by NIST. Avoid using it if not required.</param>
        /// <param name="S">the customization string - available for local use.</param>
        public CShakeDigest(int bitLength, byte[] N, byte[] S) : base(bitLength)
        {
            if ((N == null || N.Length == 0) && (S == null || S.Length == 0))
            {
                diff = null;
            }
            else
            {
                diff = Arrays.ConcatenateAll(XofUtilities.LeftEncode(rate / 8), encodeString(N), encodeString(S));
                DiffPadAndAbsorb();
            }
        }

        // bytepad in SP 800-185
        private void DiffPadAndAbsorb()
        {
            int blockSize = rate / 8;
            Absorb(diff, 0, diff.Length);

            int delta = diff.Length % blockSize;

            // only add padding if needed
            if (delta != 0)
            {
                int required = blockSize - delta;

                while (required > padding.Length)
                {
                    Absorb(padding, 0, padding.Length);
                    required -= padding.Length;
                }

                Absorb(padding, 0, required);
            }
        }

        private byte[] encodeString(byte[] str)
        {
            if (str == null || str.Length == 0)
            {
                return XofUtilities.LeftEncode(0);
            }

            return Arrays.Concatenate(XofUtilities.LeftEncode(str.Length * 8L), str);
        }

        public override string AlgorithmName
        {
            get { return "CSHAKE" + fixedOutputLength; }
        }

        public override int DoFinal(byte[] output, int outOff)
        {           
            return DoFinal(output, outOff,GetDigestSize());
        }

        public override int DoFinal(byte[] output, int outOff, int outLen)
        {
            int length = DoOutput(output, outOff, outLen);

            Reset();

            return length;
        }

        public override int DoOutput(byte[] output, int outOff, int outLen)
        {
            if (diff != null)
            {
                if (!squeezing)
                {
                    AbsorbBits(0x00, 2);
                }

                Squeeze(output, outOff, ((long)outLen) * 8);

                return outLen;
            }
            else
            {
                return base.DoOutput(output, outOff, outLen);
            }
        }

        public override void Reset()
        {
            base.Reset();

            if (diff != null)
            {
                DiffPadAndAbsorb();
            }
        }
    }
}
