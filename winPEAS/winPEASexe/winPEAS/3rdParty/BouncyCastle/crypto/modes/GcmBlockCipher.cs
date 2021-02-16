using System;
using winPEAS._3rdParty.BouncyCastle.crypto.modes.gcm;
using winPEAS._3rdParty.BouncyCastle.crypto.parameters;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.crypto.modes
{
    /// <summary>
    /// Implements the Galois/Counter mode (GCM) detailed in
    /// NIST Special Publication 800-38D.
    /// </summary>
    public class GcmBlockCipher
        : IAeadBlockCipher
    {
        private const int BlockSize = 16;

        private readonly IBlockCipher cipher;
        private readonly IGcmMultiplier multiplier;
        private IGcmExponentiator exp;

        // These fields are set by Init and not modified by processing
        private bool forEncryption;
        private bool initialised;
        private int macSize;
        private byte[] lastKey;
        private byte[] nonce;
        private byte[] initialAssociatedText;
        private byte[] H;
        private byte[] J0;

        // These fields are modified during processing
        private byte[] bufBlock;
        private byte[] macBlock;
        private byte[] S, S_at, S_atPre;
        private byte[] counter;
        private uint blocksRemaining;
        private int bufOff;
        private ulong totalLength;
        private byte[] atBlock;
        private int atBlockPos;
        private ulong atLength;
        private ulong atLengthPre;

        public GcmBlockCipher(
            IBlockCipher c)
            : this(c, null)
        {
        }

        public GcmBlockCipher(
            IBlockCipher c,
            IGcmMultiplier m)
        {
            if (c.GetBlockSize() != BlockSize)
                throw new ArgumentException("cipher required with a block size of " + BlockSize + ".");

            if (m == null)
            {
                m = new Tables4kGcmMultiplier();
            }

            this.cipher = c;
            this.multiplier = m;
        }

        public virtual string AlgorithmName
        {
            get { return cipher.AlgorithmName + "/GCM"; }
        }

        public IBlockCipher GetUnderlyingCipher()
        {
            return cipher;
        }

        public virtual int GetBlockSize()
        {
            return BlockSize;
        }

        /// <remarks>
        /// MAC sizes from 32 bits to 128 bits (must be a multiple of 8) are supported. The default is 128 bits.
        /// Sizes less than 96 are not recommended, but are supported for specialized applications.
        /// </remarks>
        public virtual void Init(
            bool forEncryption,
            ICipherParameters parameters)
        {
            this.forEncryption = forEncryption;
            this.macBlock = null;
            this.initialised = true;

            KeyParameter keyParam;
            byte[] newNonce = null;

            if (parameters is AeadParameters)
            {
                AeadParameters param = (AeadParameters)parameters;

                newNonce = param.GetNonce();
                initialAssociatedText = param.GetAssociatedText();

                int macSizeBits = param.MacSize;
                if (macSizeBits < 32 || macSizeBits > 128 || macSizeBits % 8 != 0)
                {
                    throw new ArgumentException("Invalid value for MAC size: " + macSizeBits);
                }

                macSize = macSizeBits / 8;
                keyParam = param.Key;
            }
            else if (parameters is ParametersWithIV)
            {
                ParametersWithIV param = (ParametersWithIV)parameters;

                newNonce = param.GetIV();
                initialAssociatedText = null;
                macSize = 16;
                keyParam = (KeyParameter)param.Parameters;
            }
            else
            {
                throw new ArgumentException("invalid parameters passed to GCM");
            }

            int bufLength = forEncryption ? BlockSize : (BlockSize + macSize);
            this.bufBlock = new byte[bufLength];

            if (newNonce == null || newNonce.Length < 1)
            {
                throw new ArgumentException("IV must be at least 1 byte");
            }

            if (forEncryption)
            {
                if (nonce != null && Arrays.AreEqual(nonce, newNonce))
                {
                    if (keyParam == null)
                    {
                        throw new ArgumentException("cannot reuse nonce for GCM encryption");
                    }
                    if (lastKey != null && Arrays.AreEqual(lastKey, keyParam.GetKey()))
                    {
                        throw new ArgumentException("cannot reuse nonce for GCM encryption");
                    }
                }
            }

            nonce = newNonce;
            if (keyParam != null)
            {
                lastKey = keyParam.GetKey();
            }

            // TODO Restrict macSize to 16 if nonce length not 12?

            // Cipher always used in forward mode
            // if keyParam is null we're reusing the last key.
            if (keyParam != null)
            {
                cipher.Init(true, keyParam);

                this.H = new byte[BlockSize];
                cipher.ProcessBlock(H, 0, H, 0);

                // if keyParam is null we're reusing the last key and the multiplier doesn't need re-init
                multiplier.Init(H);
                exp = null;
            }
            else if (this.H == null)
            {
                throw new ArgumentException("Key must be specified in initial init");
            }

            this.J0 = new byte[BlockSize];

            if (nonce.Length == 12)
            {
                Array.Copy(nonce, 0, J0, 0, nonce.Length);
                this.J0[BlockSize - 1] = 0x01;
            }
            else
            {
                gHASH(J0, nonce, nonce.Length);
                byte[] X = new byte[BlockSize];
                Pack.UInt64_To_BE((ulong)nonce.Length * 8UL, X, 8);
                gHASHBlock(J0, X);
            }

            this.S = new byte[BlockSize];
            this.S_at = new byte[BlockSize];
            this.S_atPre = new byte[BlockSize];
            this.atBlock = new byte[BlockSize];
            this.atBlockPos = 0;
            this.atLength = 0;
            this.atLengthPre = 0;
            this.counter = Arrays.Clone(J0);
            this.blocksRemaining = uint.MaxValue - 1; // page 8, len(P) <= 2^39 - 256, 1 block used by tag
            this.bufOff = 0;
            this.totalLength = 0;

            if (initialAssociatedText != null)
            {
                ProcessAadBytes(initialAssociatedText, 0, initialAssociatedText.Length);
            }
        }

        public virtual byte[] GetMac()
        {
            return macBlock == null
                ? new byte[macSize]
                : Arrays.Clone(macBlock);
        }

        public virtual int GetOutputSize(
            int len)
        {
            int totalData = len + bufOff;

            if (forEncryption)
            {
                return totalData + macSize;
            }

            return totalData < macSize ? 0 : totalData - macSize;
        }

        public virtual int GetUpdateOutputSize(
            int len)
        {
            int totalData = len + bufOff;
            if (!forEncryption)
            {
                if (totalData < macSize)
                {
                    return 0;
                }
                totalData -= macSize;
            }
            return totalData - totalData % BlockSize;
        }

        public virtual void ProcessAadByte(byte input)
        {
            CheckStatus();

            atBlock[atBlockPos] = input;
            if (++atBlockPos == BlockSize)
            {
                // Hash each block as it fills
                gHASHBlock(S_at, atBlock);
                atBlockPos = 0;
                atLength += BlockSize;
            }
        }

        public virtual void ProcessAadBytes(byte[] inBytes, int inOff, int len)
        {
            CheckStatus();

            for (int i = 0; i < len; ++i)
            {
                atBlock[atBlockPos] = inBytes[inOff + i];
                if (++atBlockPos == BlockSize)
                {
                    // Hash each block as it fills
                    gHASHBlock(S_at, atBlock);
                    atBlockPos = 0;
                    atLength += BlockSize;
                }
            }
        }

        private void InitCipher()
        {
            if (atLength > 0)
            {
                Array.Copy(S_at, 0, S_atPre, 0, BlockSize);
                atLengthPre = atLength;
            }

            // Finish hash for partial AAD block
            if (atBlockPos > 0)
            {
                gHASHPartial(S_atPre, atBlock, 0, atBlockPos);
                atLengthPre += (uint)atBlockPos;
            }

            if (atLengthPre > 0)
            {
                Array.Copy(S_atPre, 0, S, 0, BlockSize);
            }
        }

        public virtual int ProcessByte(
            byte input,
            byte[] output,
            int outOff)
        {
            CheckStatus();

            bufBlock[bufOff] = input;
            if (++bufOff == bufBlock.Length)
            {
                ProcessBlock(bufBlock, 0, output, outOff);
                if (forEncryption)
                {
                    bufOff = 0;
                }
                else
                {
                    Array.Copy(bufBlock, BlockSize, bufBlock, 0, macSize);
                    bufOff = macSize;
                }
                return BlockSize;
            }
            return 0;
        }

        public virtual int ProcessBytes(
            byte[] input,
            int inOff,
            int len,
            byte[] output,
            int outOff)
        {
            CheckStatus();

            Check.DataLength(input, inOff, len, "input buffer too short");

            int resultLen = 0;

            if (forEncryption)
            {
                if (bufOff != 0)
                {
                    while (len > 0)
                    {
                        --len;
                        bufBlock[bufOff] = input[inOff++];
                        if (++bufOff == BlockSize)
                        {
                            ProcessBlock(bufBlock, 0, output, outOff);
                            bufOff = 0;
                            resultLen += BlockSize;
                            break;
                        }
                    }
                }

                while (len >= BlockSize)
                {
                    ProcessBlock(input, inOff, output, outOff + resultLen);
                    inOff += BlockSize;
                    len -= BlockSize;
                    resultLen += BlockSize;
                }

                if (len > 0)
                {
                    Array.Copy(input, inOff, bufBlock, 0, len);
                    bufOff = len;
                }
            }
            else
            {
                for (int i = 0; i < len; ++i)
                {
                    bufBlock[bufOff] = input[inOff + i];
                    if (++bufOff == bufBlock.Length)
                    {
                        ProcessBlock(bufBlock, 0, output, outOff + resultLen);
                        Array.Copy(bufBlock, BlockSize, bufBlock, 0, macSize);
                        bufOff = macSize;
                        resultLen += BlockSize;
                    }
                }
            }

            return resultLen;
        }

        public int DoFinal(byte[] output, int outOff)
        {
            CheckStatus();

            if (totalLength == 0)
            {
                InitCipher();
            }

            int extra = bufOff;

            if (forEncryption)
            {
                Check.OutputLength(output, outOff, extra + macSize, "Output buffer too short");
            }
            else
            {
                if (extra < macSize)
                    throw new InvalidCipherTextException("data too short");

                extra -= macSize;

                Check.OutputLength(output, outOff, extra, "Output buffer too short");
            }

            if (extra > 0)
            {
                ProcessPartial(bufBlock, 0, extra, output, outOff);
            }

            atLength += (uint)atBlockPos;

            if (atLength > atLengthPre)
            {
                /*
                 *  Some AAD was sent after the cipher started. We determine the difference b/w the hash value
                 *  we actually used when the cipher started (S_atPre) and the final hash value calculated (S_at).
                 *  Then we carry this difference forward by multiplying by H^c, where c is the number of (full or
                 *  partial) cipher-text blocks produced, and adjust the current hash.
                 */

                // Finish hash for partial AAD block
                if (atBlockPos > 0)
                {
                    gHASHPartial(S_at, atBlock, 0, atBlockPos);
                }

                // Find the difference between the AAD hashes
                if (atLengthPre > 0)
                {
                    GcmUtilities.Xor(S_at, S_atPre);
                }

                // Number of cipher-text blocks produced
                long c = (long)(((totalLength * 8) + 127) >> 7);

                // Calculate the adjustment factor
                byte[] H_c = new byte[16];
                if (exp == null)
                {
                    exp = new BasicGcmExponentiator();
                    exp.Init(H);
                }
                exp.ExponentiateX(c, H_c);

                // Carry the difference forward
                GcmUtilities.Multiply(S_at, H_c);

                // Adjust the current hash
                GcmUtilities.Xor(S, S_at);
            }

            // Final gHASH
            byte[] X = new byte[BlockSize];
            Pack.UInt64_To_BE(atLength * 8UL, X, 0);
            Pack.UInt64_To_BE(totalLength * 8UL, X, 8);

            gHASHBlock(S, X);

            // T = MSBt(GCTRk(J0,S))
            byte[] tag = new byte[BlockSize];
            cipher.ProcessBlock(J0, 0, tag, 0);
            GcmUtilities.Xor(tag, S);

            int resultLen = extra;

            // We place into macBlock our calculated value for T
            this.macBlock = new byte[macSize];
            Array.Copy(tag, 0, macBlock, 0, macSize);

            if (forEncryption)
            {
                // Append T to the message
                Array.Copy(macBlock, 0, output, outOff + bufOff, macSize);
                resultLen += macSize;
            }
            else
            {
                // Retrieve the T value from the message and compare to calculated one
                byte[] msgMac = new byte[macSize];
                Array.Copy(bufBlock, extra, msgMac, 0, macSize);
                if (!Arrays.ConstantTimeAreEqual(this.macBlock, msgMac))
                    throw new InvalidCipherTextException("mac check in GCM failed");
            }

            Reset(false);

            return resultLen;
        }

        public virtual void Reset()
        {
            Reset(true);
        }

        private void Reset(
            bool clearMac)
        {
            cipher.Reset();

            // note: we do not reset the nonce.

            S = new byte[BlockSize];
            S_at = new byte[BlockSize];
            S_atPre = new byte[BlockSize];
            atBlock = new byte[BlockSize];
            atBlockPos = 0;
            atLength = 0;
            atLengthPre = 0;
            counter = Arrays.Clone(J0);
            blocksRemaining = uint.MaxValue - 1;
            bufOff = 0;
            totalLength = 0;

            if (bufBlock != null)
            {
                Arrays.Fill(bufBlock, 0);
            }

            if (clearMac)
            {
                macBlock = null;
            }

            if (forEncryption)
            {
                initialised = false;
            }
            else
            {
                if (initialAssociatedText != null)
                {
                    ProcessAadBytes(initialAssociatedText, 0, initialAssociatedText.Length);
                }
            }
        }

        private void ProcessBlock(byte[] buf, int bufOff, byte[] output, int outOff)
        {
            Check.OutputLength(output, outOff, BlockSize, "Output buffer too short");

            if (totalLength == 0)
            {
                InitCipher();
            }

            byte[] ctrBlock = new byte[BlockSize];
            GetNextCtrBlock(ctrBlock);

            if (forEncryption)
            {
                GcmUtilities.Xor(ctrBlock, buf, bufOff);
                gHASHBlock(S, ctrBlock);
                Array.Copy(ctrBlock, 0, output, outOff, BlockSize);
            }
            else
            {
                gHASHBlock(S, buf, bufOff);
                GcmUtilities.Xor(ctrBlock, 0, buf, bufOff, output, outOff);
            }

            totalLength += BlockSize;
        }

        private void ProcessPartial(byte[] buf, int off, int len, byte[] output, int outOff)
        {
            byte[] ctrBlock = new byte[BlockSize];
            GetNextCtrBlock(ctrBlock);

            if (forEncryption)
            {
                GcmUtilities.Xor(buf, off, ctrBlock, 0, len);
                gHASHPartial(S, buf, off, len);
            }
            else
            {
                gHASHPartial(S, buf, off, len);
                GcmUtilities.Xor(buf, off, ctrBlock, 0, len);
            }

            Array.Copy(buf, off, output, outOff, len);
            totalLength += (uint)len;
        }

        private void gHASH(byte[] Y, byte[] b, int len)
        {
            for (int pos = 0; pos < len; pos += BlockSize)
            {
                int num = System.Math.Min(len - pos, BlockSize);
                gHASHPartial(Y, b, pos, num);
            }
        }

        private void gHASHBlock(byte[] Y, byte[] b)
        {
            GcmUtilities.Xor(Y, b);
            multiplier.MultiplyH(Y);
        }

        private void gHASHBlock(byte[] Y, byte[] b, int off)
        {
            GcmUtilities.Xor(Y, b, off);
            multiplier.MultiplyH(Y);
        }

        private void gHASHPartial(byte[] Y, byte[] b, int off, int len)
        {
            GcmUtilities.Xor(Y, b, off, len);
            multiplier.MultiplyH(Y);
        }

        private void GetNextCtrBlock(byte[] block)
        {
            if (blocksRemaining == 0)
                throw new InvalidOperationException("Attempt to process too many blocks");

            blocksRemaining--;

            uint c = 1;
            c += counter[15]; counter[15] = (byte)c; c >>= 8;
            c += counter[14]; counter[14] = (byte)c; c >>= 8;
            c += counter[13]; counter[13] = (byte)c; c >>= 8;
            c += counter[12]; counter[12] = (byte)c;

            cipher.ProcessBlock(counter, 0, block, 0);
        }

        private void CheckStatus()
        {
            if (!initialised)
            {
                if (forEncryption)
                {
                    throw new InvalidOperationException("GCM cipher cannot be reused for encryption");
                }
                throw new InvalidOperationException("GCM cipher needs to be initialised");
            }
        }
    }
}
