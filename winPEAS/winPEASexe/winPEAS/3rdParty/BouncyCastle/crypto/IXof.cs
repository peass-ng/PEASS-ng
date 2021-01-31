namespace winPEAS._3rdParty.BouncyCastle.crypto
{
    /// <remarks>
    /// With FIPS PUB 202 a new kind of message digest was announced which supported extendable output, or variable digest sizes.
    /// This interface provides the extra method required to support variable output on a digest implementation.
    /// </remarks>
    public interface IXof
        : IDigest
    {
        /// <summary>
        /// Output the results of the final calculation for this digest to outLen number of bytes.
        /// </summary>
        /// <param name="output">output array to write the output bytes to.</param>
        /// <param name="outOff">offset to start writing the bytes at.</param>
        /// <param name="outLen">the number of output bytes requested.</param>
        /// <returns>the number of bytes written</returns>
        int DoFinal(byte[] output, int outOff, int outLen);

        /// <summary>
        /// Start outputting the results of the final calculation for this digest. Unlike DoFinal, this method
        /// will continue producing output until the Xof is explicitly reset, or signals otherwise.
        /// </summary>
        /// <param name="output">output array to write the output bytes to.</param>
        /// <param name="outOff">offset to start writing the bytes at.</param>
        /// <param name="outLen">the number of output bytes requested.</param>
        /// <returns>the number of bytes written</returns>
        int DoOutput(byte[] output, int outOff, int outLen);
    }
}
