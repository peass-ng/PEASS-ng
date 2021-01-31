using System;
using System.Threading;
using winPEAS._3rdParty.BouncyCastle.crypto.prng;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.security
{
    public class SecureRandom
      : Random
    {
        private static long counter = Times.NanoTime();

#if NETCF_1_0 || PORTABLE
        private static object counterLock = new object();
        private static long NextCounterValue()
        {
            lock (counterLock)
            {
                return ++counter;
            }
        }

        private static readonly SecureRandom[] master = { null };
        private static SecureRandom Master
        {
            get
            {
                lock (master)
                {
                    if (master[0] == null)
                    {
                        SecureRandom sr = master[0] = GetInstance("SHA256PRNG", false);

                        // Even though Ticks has at most 8 or 14 bits of entropy, there's no harm in adding it.
                        sr.SetSeed(DateTime.Now.Ticks);

                        // 32 will be enough when ThreadedSeedGenerator is fixed.  Until then, ThreadedSeedGenerator returns low
                        // entropy, and this is not sufficient to be secure. http://www.bouncycastle.org/csharpdevmailarchive/msg00814.html
                        sr.SetSeed(new ThreadedSeedGenerator().GenerateSeed(32, true));
                    }

                    return master[0];
                }
            }
        }
#else
        private static long NextCounterValue()
        {
            return Interlocked.Increment(ref counter);
        }

        private static readonly SecureRandom master = new SecureRandom(new CryptoApiRandomGenerator());
        private static SecureRandom Master
        {
            get { return master; }
        }
#endif

        private static DigestRandomGenerator CreatePrng(string digestName, bool autoSeed)
        {
            IDigest digest = DigestUtilities.GetDigest(digestName);
            if (digest == null)
                return null;
            DigestRandomGenerator prng = new DigestRandomGenerator(digest);
            if (autoSeed)
            {
                prng.AddSeedMaterial(NextCounterValue());
                prng.AddSeedMaterial(GetNextBytes(Master, digest.GetDigestSize()));
            }
            return prng;
        }

        public static byte[] GetNextBytes(SecureRandom secureRandom, int length)
        {
            byte[] result = new byte[length];
            secureRandom.NextBytes(result);
            return result;
        }

        /// <summary>
        /// Create and auto-seed an instance based on the given algorithm.
        /// </summary>
        /// <remarks>Equivalent to GetInstance(algorithm, true)</remarks>
        /// <param name="algorithm">e.g. "SHA256PRNG"</param>
        public static SecureRandom GetInstance(string algorithm)
        {
            return GetInstance(algorithm, true);
        }

        /// <summary>
        /// Create an instance based on the given algorithm, with optional auto-seeding
        /// </summary>
        /// <param name="algorithm">e.g. "SHA256PRNG"</param>
        /// <param name="autoSeed">If true, the instance will be auto-seeded.</param>
        public static SecureRandom GetInstance(string algorithm, bool autoSeed)
        {
            string upper = Platform.ToUpperInvariant(algorithm);
            if (Platform.EndsWith(upper, "PRNG"))
            {
                string digestName = upper.Substring(0, upper.Length - "PRNG".Length);
                DigestRandomGenerator prng = CreatePrng(digestName, autoSeed);
                if (prng != null)
                {
                    return new SecureRandom(prng);
                }
            }

            throw new ArgumentException("Unrecognised PRNG algorithm: " + algorithm, "algorithm");
        }

        [Obsolete("Call GenerateSeed() on a SecureRandom instance instead")]
        public static byte[] GetSeed(int length)
        {
            return GetNextBytes(Master, length);
        }

        protected readonly IRandomGenerator generator;

        public SecureRandom()
            : this(CreatePrng("SHA256", true))
        {
        }

        /// <remarks>
        /// To replicate existing predictable output, replace with GetInstance("SHA1PRNG", false), followed by SetSeed(seed)
        /// </remarks>
        [Obsolete("Use GetInstance/SetSeed instead")]
        public SecureRandom(byte[] seed)
            : this(CreatePrng("SHA1", false))
        {
            SetSeed(seed);
        }

        /// <summary>Use the specified instance of IRandomGenerator as random source.</summary>
        /// <remarks>
        /// This constructor performs no seeding of either the <c>IRandomGenerator</c> or the
        /// constructed <c>SecureRandom</c>. It is the responsibility of the client to provide
        /// proper seed material as necessary/appropriate for the given <c>IRandomGenerator</c>
        /// implementation.
        /// </remarks>
        /// <param name="generator">The source to generate all random bytes from.</param>
        public SecureRandom(IRandomGenerator generator)
            : base(0)
        {
            this.generator = generator;
        }

        public virtual byte[] GenerateSeed(int length)
        {
            return GetNextBytes(Master, length);
        }

        public virtual void SetSeed(byte[] seed)
        {
            generator.AddSeedMaterial(seed);
        }

        public virtual void SetSeed(long seed)
        {
            generator.AddSeedMaterial(seed);
        }

        public override int Next()
        {
            return NextInt() & int.MaxValue;
        }

        public override int Next(int maxValue)
        {

            if (maxValue < 2)
            {
                if (maxValue < 0)
                    throw new ArgumentOutOfRangeException("maxValue", "cannot be negative");

                return 0;
            }

            int bits;

            // Test whether maxValue is a power of 2
            if ((maxValue & (maxValue - 1)) == 0)
            {
                bits = NextInt() & int.MaxValue;
                return (int)(((long)bits * maxValue) >> 31);
            }

            int result;
            do
            {
                bits = NextInt() & int.MaxValue;
                result = bits % maxValue;
            }
            while (bits - result + (maxValue - 1) < 0); // Ignore results near overflow

            return result;
        }

        public override int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                if (maxValue == minValue)
                    return minValue;

                throw new ArgumentException("maxValue cannot be less than minValue");
            }

            int diff = maxValue - minValue;
            if (diff > 0)
                return minValue + Next(diff);

            for (; ; )
            {
                int i = NextInt();

                if (i >= minValue && i < maxValue)
                    return i;
            }
        }

        public override void NextBytes(byte[] buf)
        {
            generator.NextBytes(buf);
        }

        public virtual void NextBytes(byte[] buf, int off, int len)
        {
            generator.NextBytes(buf, off, len);
        }

        private static readonly double DoubleScale = 1.0 / Convert.ToDouble(1L << 53);

        public override double NextDouble()
        {
            ulong x = (ulong)NextLong() >> 11;

            return Convert.ToDouble(x) * DoubleScale;
        }

        public virtual int NextInt()
        {
            byte[] bytes = new byte[4];
            NextBytes(bytes);
            return (int)Pack.BE_To_UInt32(bytes);
        }

        public virtual long NextLong()
        {
            byte[] bytes = new byte[8];
            NextBytes(bytes);
            return (long)Pack.BE_To_UInt64(bytes);
        }
    }
}
