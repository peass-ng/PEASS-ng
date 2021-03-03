using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.math.raw;

namespace winPEAS._3rdParty.BouncyCastle.crypto.modes
{
    internal abstract class GcmUtilities
    {
        private const uint E1 = 0xe1000000;
        private const ulong E1UL = (ulong)E1 << 32;

        internal static byte[] OneAsBytes()
        {
            byte[] tmp = new byte[16];
            tmp[0] = 0x80;
            return tmp;
        }

        internal static uint[] OneAsUints()
        {
            uint[] tmp = new uint[4];
            tmp[0] = 0x80000000;
            return tmp;
        }

        internal static ulong[] OneAsUlongs()
        {
            ulong[] tmp = new ulong[2];
            tmp[0] = 1UL << 63;
            return tmp;
        }

        internal static byte[] AsBytes(uint[] x)
        {
            return Pack.UInt32_To_BE(x);
        }

        internal static void AsBytes(uint[] x, byte[] z)
        {
            Pack.UInt32_To_BE(x, z, 0);
        }

        internal static byte[] AsBytes(ulong[] x)
        {
            byte[] z = new byte[16];
            Pack.UInt64_To_BE(x, z, 0);
            return z;
        }

        internal static void AsBytes(ulong[] x, byte[] z)
        {
            Pack.UInt64_To_BE(x, z, 0);
        }

        internal static uint[] AsUints(byte[] bs)
        {
            uint[] output = new uint[4];
            Pack.BE_To_UInt32(bs, 0, output);
            return output;
        }

        internal static void AsUints(byte[] bs, uint[] output)
        {
            Pack.BE_To_UInt32(bs, 0, output);
        }

        internal static ulong[] AsUlongs(byte[] x)
        {
            ulong[] z = new ulong[2];
            Pack.BE_To_UInt64(x, 0, z);
            return z;
        }

        internal static void AsUlongs(byte[] x, ulong[] z)
        {
            Pack.BE_To_UInt64(x, 0, z);
        }

        internal static void AsUlongs(byte[] x, ulong[] z, int zOff)
        {
            Pack.BE_To_UInt64(x, 0, z, zOff, 2);
        }

        internal static void Copy(uint[] x, uint[] z)
        {
            z[0] = x[0];
            z[1] = x[1];
            z[2] = x[2];
            z[3] = x[3];
        }

        internal static void Copy(ulong[] x, ulong[] z)
        {
            z[0] = x[0];
            z[1] = x[1];
        }

        internal static void Copy(ulong[] x, int xOff, ulong[] z, int zOff)
        {
            z[zOff + 0] = x[xOff + 0];
            z[zOff + 1] = x[xOff + 1];
        }

        internal static void DivideP(ulong[] x, ulong[] z)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong m = (ulong)((long)x0 >> 63);
            x0 ^= (m & E1UL);
            z[0] = (x0 << 1) | (x1 >> 63);
            z[1] = (x1 << 1) | (ulong)(-(long)m);
        }

        internal static void DivideP(ulong[] x, int xOff, ulong[] z, int zOff)
        {
            ulong x0 = x[xOff + 0], x1 = x[xOff + 1];
            ulong m = (ulong)((long)x0 >> 63);
            x0 ^= (m & E1UL);
            z[zOff + 0] = (x0 << 1) | (x1 >> 63);
            z[zOff + 1] = (x1 << 1) | (ulong)(-(long)m);
        }

        internal static void Multiply(byte[] x, byte[] y)
        {
            ulong[] t1 = GcmUtilities.AsUlongs(x);
            ulong[] t2 = GcmUtilities.AsUlongs(y);
            GcmUtilities.Multiply(t1, t2);
            GcmUtilities.AsBytes(t1, x);
        }

        internal static void Multiply(uint[] x, uint[] y)
        {
            uint y0 = y[0], y1 = y[1], y2 = y[2], y3 = y[3];
            uint z0 = 0, z1 = 0, z2 = 0, z3 = 0;

            for (int i = 0; i < 4; ++i)
            {
                int bits = (int)x[i];
                for (int j = 0; j < 32; ++j)
                {
                    uint m1 = (uint)(bits >> 31); bits <<= 1;
                    z0 ^= (y0 & m1);
                    z1 ^= (y1 & m1);
                    z2 ^= (y2 & m1);
                    z3 ^= (y3 & m1);

                    uint m2 = (uint)((int)(y3 << 31) >> 8);
                    y3 = (y3 >> 1) | (y2 << 31);
                    y2 = (y2 >> 1) | (y1 << 31);
                    y1 = (y1 >> 1) | (y0 << 31);
                    y0 = (y0 >> 1) ^ (m2 & E1);
                }
            }

            x[0] = z0;
            x[1] = z1;
            x[2] = z2;
            x[3] = z3;
        }

        internal static void Multiply(ulong[] x, ulong[] y)
        {
            //ulong x0 = x[0], x1 = x[1];
            //ulong y0 = y[0], y1 = y[1];
            //ulong z0 = 0, z1 = 0, z2 = 0;

            //for (int j = 0; j < 64; ++j)
            //{
            //    ulong m0 = (ulong)((long)x0 >> 63); x0 <<= 1;
            //    z0 ^= (y0 & m0);
            //    z1 ^= (y1 & m0);

            //    ulong m1 = (ulong)((long)x1 >> 63); x1 <<= 1;
            //    z1 ^= (y0 & m1);
            //    z2 ^= (y1 & m1);

            //    ulong c = (ulong)((long)(y1 << 63) >> 8);
            //    y1 = (y1 >> 1) | (y0 << 63);
            //    y0 = (y0 >> 1) ^ (c & E1UL);
            //}

            //z0 ^= z2 ^ (z2 >>  1) ^ (z2 >>  2) ^ (z2 >>  7);
            //z1 ^=      (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);

            //x[0] = z0;
            //x[1] = z1;

            /*
             * "Three-way recursion" as described in "Batch binary Edwards", Daniel J. Bernstein.
             *
             * Without access to the high part of a 64x64 product x * y, we use a bit reversal to calculate it:
             *     rev(x) * rev(y) == rev((x * y) << 1) 
             */

            ulong x0 = x[0], x1 = x[1];
            ulong y0 = y[0], y1 = y[1];
            ulong x0r = Longs.Reverse(x0), x1r = Longs.Reverse(x1);
            ulong y0r = Longs.Reverse(y0), y1r = Longs.Reverse(y1);

            ulong h0 = Longs.Reverse(ImplMul64(x0r, y0r));
            ulong h1 = ImplMul64(x0, y0) << 1;
            ulong h2 = Longs.Reverse(ImplMul64(x1r, y1r));
            ulong h3 = ImplMul64(x1, y1) << 1;
            ulong h4 = Longs.Reverse(ImplMul64(x0r ^ x1r, y0r ^ y1r));
            ulong h5 = ImplMul64(x0 ^ x1, y0 ^ y1) << 1;

            ulong z0 = h0;
            ulong z1 = h1 ^ h0 ^ h2 ^ h4;
            ulong z2 = h2 ^ h1 ^ h3 ^ h5;
            ulong z3 = h3;

            z1 ^= z3 ^ (z3 >> 1) ^ (z3 >> 2) ^ (z3 >> 7);
            //          z2 ^=      (z3 << 63) ^ (z3 << 62) ^ (z3 << 57);
            z2 ^= (z3 << 62) ^ (z3 << 57);

            z0 ^= z2 ^ (z2 >> 1) ^ (z2 >> 2) ^ (z2 >> 7);
            z1 ^= (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);

            x[0] = z0;
            x[1] = z1;
        }

        internal static void MultiplyP(uint[] x)
        {
            uint x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3];
            uint m = (uint)((int)(x3 << 31) >> 31);
            x[0] = (x0 >> 1) ^ (m & E1);
            x[1] = (x1 >> 1) | (x0 << 31);
            x[2] = (x2 >> 1) | (x1 << 31);
            x[3] = (x3 >> 1) | (x2 << 31);
        }

        internal static void MultiplyP(uint[] x, uint[] z)
        {
            uint x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3];
            uint m = (uint)((int)(x3 << 31) >> 31);
            z[0] = (x0 >> 1) ^ (m & E1);
            z[1] = (x1 >> 1) | (x0 << 31);
            z[2] = (x2 >> 1) | (x1 << 31);
            z[3] = (x3 >> 1) | (x2 << 31);
        }

        internal static void MultiplyP(ulong[] x)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong m = (ulong)((long)(x1 << 63) >> 63);
            x[0] = (x0 >> 1) ^ (m & E1UL);
            x[1] = (x1 >> 1) | (x0 << 63);
        }

        internal static void MultiplyP(ulong[] x, ulong[] z)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong m = (ulong)((long)(x1 << 63) >> 63);
            z[0] = (x0 >> 1) ^ (m & E1UL);
            z[1] = (x1 >> 1) | (x0 << 63);
        }

        internal static void MultiplyP3(ulong[] x, ulong[] z)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong c = x1 << 61;
            z[0] = (x0 >> 3) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            z[1] = (x1 >> 3) | (x0 << 61);
        }

        internal static void MultiplyP3(ulong[] x, int xOff, ulong[] z, int zOff)
        {
            ulong x0 = x[xOff + 0], x1 = x[xOff + 1];
            ulong c = x1 << 61;
            z[zOff + 0] = (x0 >> 3) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            z[zOff + 1] = (x1 >> 3) | (x0 << 61);
        }

        internal static void MultiplyP4(ulong[] x, ulong[] z)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong c = x1 << 60;
            z[0] = (x0 >> 4) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            z[1] = (x1 >> 4) | (x0 << 60);
        }

        internal static void MultiplyP4(ulong[] x, int xOff, ulong[] z, int zOff)
        {
            ulong x0 = x[xOff + 0], x1 = x[xOff + 1];
            ulong c = x1 << 60;
            z[zOff + 0] = (x0 >> 4) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            z[zOff + 1] = (x1 >> 4) | (x0 << 60);
        }

        internal static void MultiplyP7(ulong[] x, ulong[] z)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong c = x1 << 57;
            z[0] = (x0 >> 7) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            z[1] = (x1 >> 7) | (x0 << 57);
        }

        internal static void MultiplyP7(ulong[] x, int xOff, ulong[] z, int zOff)
        {
            ulong x0 = x[xOff + 0], x1 = x[xOff + 1];
            ulong c = x1 << 57;
            z[zOff + 0] = (x0 >> 7) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            z[zOff + 1] = (x1 >> 7) | (x0 << 57);
        }

        internal static void MultiplyP8(uint[] x)
        {
            uint x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3];
            uint c = x3 << 24;
            x[0] = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            x[1] = (x1 >> 8) | (x0 << 24);
            x[2] = (x2 >> 8) | (x1 << 24);
            x[3] = (x3 >> 8) | (x2 << 24);
        }

        internal static void MultiplyP8(uint[] x, uint[] y)
        {
            uint x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3];
            uint c = x3 << 24;
            y[0] = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            y[1] = (x1 >> 8) | (x0 << 24);
            y[2] = (x2 >> 8) | (x1 << 24);
            y[3] = (x3 >> 8) | (x2 << 24);
        }

        internal static void MultiplyP8(ulong[] x)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong c = x1 << 56;
            x[0] = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            x[1] = (x1 >> 8) | (x0 << 56);
        }

        internal static void MultiplyP8(ulong[] x, ulong[] y)
        {
            ulong x0 = x[0], x1 = x[1];
            ulong c = x1 << 56;
            y[0] = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            y[1] = (x1 >> 8) | (x0 << 56);
        }

        internal static void MultiplyP8(ulong[] x, int xOff, ulong[] y, int yOff)
        {
            ulong x0 = x[xOff + 0], x1 = x[xOff + 1];
            ulong c = x1 << 56;
            y[yOff + 0] = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            y[yOff + 1] = (x1 >> 8) | (x0 << 56);
        }

        internal static void Square(ulong[] x, ulong[] z)
        {
            ulong[] t = new ulong[4];
            Interleave.Expand64To128Rev(x[0], t, 0);
            Interleave.Expand64To128Rev(x[1], t, 2);

            ulong z0 = t[0], z1 = t[1], z2 = t[2], z3 = t[3];

            z1 ^= z3 ^ (z3 >> 1) ^ (z3 >> 2) ^ (z3 >> 7);
            //          z2 ^=      (z3 << 63) ^ (z3 << 62) ^ (z3 << 57);
            z2 ^= (z3 << 62) ^ (z3 << 57);

            z0 ^= z2 ^ (z2 >> 1) ^ (z2 >> 2) ^ (z2 >> 7);
            z1 ^= (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);

            z[0] = z0;
            z[1] = z1;
        }

        internal static void Xor(byte[] x, byte[] y)
        {
            int i = 0;
            do
            {
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
            }
            while (i < 16);
        }

        internal static void Xor(byte[] x, byte[] y, int yOff)
        {
            int i = 0;
            do
            {
                x[i] ^= y[yOff + i]; ++i;
                x[i] ^= y[yOff + i]; ++i;
                x[i] ^= y[yOff + i]; ++i;
                x[i] ^= y[yOff + i]; ++i;
            }
            while (i < 16);
        }

        internal static void Xor(byte[] x, int xOff, byte[] y, int yOff, byte[] z, int zOff)
        {
            int i = 0;
            do
            {
                z[zOff + i] = (byte)(x[xOff + i] ^ y[yOff + i]); ++i;
                z[zOff + i] = (byte)(x[xOff + i] ^ y[yOff + i]); ++i;
                z[zOff + i] = (byte)(x[xOff + i] ^ y[yOff + i]); ++i;
                z[zOff + i] = (byte)(x[xOff + i] ^ y[yOff + i]); ++i;
            }
            while (i < 16);
        }

        internal static void Xor(byte[] x, byte[] y, int yOff, int yLen)
        {
            while (--yLen >= 0)
            {
                x[yLen] ^= y[yOff + yLen];
            }
        }

        internal static void Xor(byte[] x, int xOff, byte[] y, int yOff, int len)
        {
            while (--len >= 0)
            {
                x[xOff + len] ^= y[yOff + len];
            }
        }

        internal static void Xor(byte[] x, byte[] y, byte[] z)
        {
            int i = 0;
            do
            {
                z[i] = (byte)(x[i] ^ y[i]); ++i;
                z[i] = (byte)(x[i] ^ y[i]); ++i;
                z[i] = (byte)(x[i] ^ y[i]); ++i;
                z[i] = (byte)(x[i] ^ y[i]); ++i;
            }
            while (i < 16);
        }

        internal static void Xor(uint[] x, uint[] y)
        {
            x[0] ^= y[0];
            x[1] ^= y[1];
            x[2] ^= y[2];
            x[3] ^= y[3];
        }

        internal static void Xor(uint[] x, uint[] y, uint[] z)
        {
            z[0] = x[0] ^ y[0];
            z[1] = x[1] ^ y[1];
            z[2] = x[2] ^ y[2];
            z[3] = x[3] ^ y[3];
        }

        internal static void Xor(ulong[] x, ulong[] y)
        {
            x[0] ^= y[0];
            x[1] ^= y[1];
        }

        internal static void Xor(ulong[] x, int xOff, ulong[] y, int yOff)
        {
            x[xOff + 0] ^= y[yOff + 0];
            x[xOff + 1] ^= y[yOff + 1];
        }

        internal static void Xor(ulong[] x, ulong[] y, ulong[] z)
        {
            z[0] = x[0] ^ y[0];
            z[1] = x[1] ^ y[1];
        }

        internal static void Xor(ulong[] x, int xOff, ulong[] y, int yOff, ulong[] z, int zOff)
        {
            z[zOff + 0] = x[xOff + 0] ^ y[yOff + 0];
            z[zOff + 1] = x[xOff + 1] ^ y[yOff + 1];
        }

        private static ulong ImplMul64(ulong x, ulong y)
        {
            ulong x0 = x & 0x1111111111111111UL;
            ulong x1 = x & 0x2222222222222222UL;
            ulong x2 = x & 0x4444444444444444UL;
            ulong x3 = x & 0x8888888888888888UL;

            ulong y0 = y & 0x1111111111111111UL;
            ulong y1 = y & 0x2222222222222222UL;
            ulong y2 = y & 0x4444444444444444UL;
            ulong y3 = y & 0x8888888888888888UL;

            ulong z0 = (x0 * y0) ^ (x1 * y3) ^ (x2 * y2) ^ (x3 * y1);
            ulong z1 = (x0 * y1) ^ (x1 * y0) ^ (x2 * y3) ^ (x3 * y2);
            ulong z2 = (x0 * y2) ^ (x1 * y1) ^ (x2 * y0) ^ (x3 * y3);
            ulong z3 = (x0 * y3) ^ (x1 * y2) ^ (x2 * y1) ^ (x3 * y0);

            z0 &= 0x1111111111111111UL;
            z1 &= 0x2222222222222222UL;
            z2 &= 0x4444444444444444UL;
            z3 &= 0x8888888888888888UL;

            return z0 | z1 | z2 | z3;
        }
    }
}
