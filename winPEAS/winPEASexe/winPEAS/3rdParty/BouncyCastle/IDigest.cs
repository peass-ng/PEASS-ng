namespace winPEAS._3rdParty.BouncyCastle
{
    /**
    * interface that a message digest conforms to.
    */
    public interface IDigest
    {
        /**
         * return the algorithm name
         *
         * @return the algorithm name
         */
        string AlgorithmName { get; }

        /**
         * return the size, in bytes, of the digest produced by this message digest.
         *
         * @return the size, in bytes, of the digest produced by this message digest.
         */
        int GetDigestSize();

        /**
         * return the size, in bytes, of the internal buffer used by this digest.
         *
         * @return the size, in bytes, of the internal buffer used by this digest.
         */
        int GetByteLength();

        /**
         * update the message digest with a single byte.
         *
         * @param inByte the input byte to be entered.
         */
        void Update(byte input);

        /**
         * update the message digest with a block of bytes.
         *
         * @param input the byte array containing the data.
         * @param inOff the offset into the byte array where the data starts.
         * @param len the length of the data.
         */
        void BlockUpdate(byte[] input, int inOff, int length);

        /**
         * Close the digest, producing the final digest value. The doFinal
         * call leaves the digest reset.
         *
         * @param output the array the digest is to be copied into.
         * @param outOff the offset into the out array the digest is to start at.
         */
        int DoFinal(byte[] output, int outOff);

        /**
         * reset the digest back to it's initial state.
         */
        void Reset();
    }
}
