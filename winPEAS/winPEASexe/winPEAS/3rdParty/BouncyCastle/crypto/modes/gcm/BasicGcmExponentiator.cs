using BrowserPass.BouncyCastle.crypto.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.crypto.modes.gcm
{
    public class BasicGcmExponentiator
        : IGcmExponentiator
    {
        private ulong[] x;

        public void Init(byte[] x)
        {
            this.x = GcmUtilities.AsUlongs(x);
        }

        public void ExponentiateX(long pow, byte[] output)
        {
            // Initial value is little-endian 1
            ulong[] y = GcmUtilities.OneAsUlongs();

            if (pow > 0)
            {
                ulong[] powX = Arrays.Clone(x);
                do
                {
                    if ((pow & 1L) != 0)
                    {
                        GcmUtilities.Multiply(y, powX);
                    }
                    GcmUtilities.Square(powX, powX);
                    pow >>= 1;
                }
                while (pow > 0);
            }

            GcmUtilities.AsBytes(y, output);
        }
    }
}
