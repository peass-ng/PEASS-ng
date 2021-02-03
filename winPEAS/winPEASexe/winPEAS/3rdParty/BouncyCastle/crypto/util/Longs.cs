using System;
using winPEAS._3rdParty.BouncyCastle.math.raw;

namespace winPEAS._3rdParty.BouncyCastle.crypto.util
{
    public abstract class Longs
    {
        public static long Reverse(long i)
        {
            i = (long)Bits.BitPermuteStepSimple((ulong)i, 0x5555555555555555UL, 1);
            i = (long)Bits.BitPermuteStepSimple((ulong)i, 0x3333333333333333UL, 2);
            i = (long)Bits.BitPermuteStepSimple((ulong)i, 0x0F0F0F0F0F0F0F0FUL, 4);
            return ReverseBytes(i);
        }

        //[CLSCompliant(false)]
        public static ulong Reverse(ulong i)
        {
            i = Bits.BitPermuteStepSimple(i, 0x5555555555555555UL, 1);
            i = Bits.BitPermuteStepSimple(i, 0x3333333333333333UL, 2);
            i = Bits.BitPermuteStepSimple(i, 0x0F0F0F0F0F0F0F0FUL, 4);
            return ReverseBytes(i);
        }

        public static long ReverseBytes(long i)
        {
            return RotateLeft((long)((ulong)i & 0xFF000000FF000000UL), 8) |
                   RotateLeft((long)((ulong)i & 0x00FF000000FF0000UL), 24) |
                   RotateLeft((long)((ulong)i & 0x0000FF000000FF00UL), 40) |
                   RotateLeft((long)((ulong)i & 0x000000FF000000FFUL), 56);
        }

        //[CLSCompliant(false)]
        public static ulong ReverseBytes(ulong i)
        {
            return RotateLeft(i & 0xFF000000FF000000UL, 8) |
                   RotateLeft(i & 0x00FF000000FF0000UL, 24) |
                   RotateLeft(i & 0x0000FF000000FF00UL, 40) |
                   RotateLeft(i & 0x000000FF000000FFUL, 56);
        }

        public static long RotateLeft(long i, int distance)
        {
            return (i << distance) ^ (long)((ulong)i >> -distance);
        }

        //[CLSCompliant(false)]
        public static ulong RotateLeft(ulong i, int distance)
        {
            return (i << distance) ^ (i >> -distance);
        }

        public static long RotateRight(long i, int distance)
        {
            return (long)((ulong)i >> distance) ^ (i << -distance);
        }

        //[CLSCompliant(false)]
        public static ulong RotateRight(ulong i, int distance)
        {
            return (i >> distance) ^ (i << -distance);
        }
    }
}
