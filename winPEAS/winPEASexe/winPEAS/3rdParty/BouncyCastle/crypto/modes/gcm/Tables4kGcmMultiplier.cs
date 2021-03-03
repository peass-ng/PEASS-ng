using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.crypto.modes.gcm
{
    public class Tables4kGcmMultiplier
      : IGcmMultiplier
    {
        private byte[] H;
        private ulong[] T;

        public void Init(byte[] H)
        {
            if (T == null)
            {
                T = new ulong[512];
            }
            else if (Arrays.AreEqual(this.H, H))
            {
                return;
            }

            this.H = Arrays.Clone(H);

            // T[0] = 0

            // T[1] = H.p^7
            GcmUtilities.AsUlongs(this.H, T, 2);
            GcmUtilities.MultiplyP7(T, 2, T, 2);

            for (int n = 2; n < 256; n += 2)
            {
                // T[2.n] = T[n].p^-1
                GcmUtilities.DivideP(T, n, T, n << 1);

                // T[2.n + 1] = T[2.n] + T[1]
                GcmUtilities.Xor(T, n << 1, T, 2, T, (n + 1) << 1);
            }
        }

        public void MultiplyH(byte[] x)
        {
            //ulong[] z = new ulong[2];
            //GcmUtilities.Copy(T, x[15] << 1, z, 0);
            //for (int i = 14; i >= 0; --i)
            //{
            //    GcmUtilities.MultiplyP8(z);
            //    GcmUtilities.Xor(z, 0, T, x[i] << 1);
            //}
            //Pack.UInt64_To_BE(z, x, 0);

            int pos = x[15] << 1;
            ulong z0 = T[pos + 0], z1 = T[pos + 1];

            for (int i = 14; i >= 0; --i)
            {
                pos = x[i] << 1;

                ulong c = z1 << 56;
                z1 = T[pos + 1] ^ ((z1 >> 8) | (z0 << 56));
                z0 = T[pos + 0] ^ (z0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            }

            Pack.UInt64_To_BE(z0, x, 0);
            Pack.UInt64_To_BE(z1, x, 8);
        }
    }
}
