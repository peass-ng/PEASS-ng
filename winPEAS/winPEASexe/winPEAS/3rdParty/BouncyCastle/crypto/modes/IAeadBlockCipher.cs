using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.crypto.modes
{
    /// <summary>An IAeadCipher based on an IBlockCipher.</summary>
    public interface IAeadBlockCipher
        : IAeadCipher
    {
        /// <returns>The block size for this cipher, in bytes.</returns>
        int GetBlockSize();

        /// <summary>The block cipher underlying this algorithm.</summary>
		IBlockCipher GetUnderlyingCipher();
    }
}
