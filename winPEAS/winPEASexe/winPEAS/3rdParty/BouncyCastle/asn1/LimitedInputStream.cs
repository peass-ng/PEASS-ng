using System.IO;
using winPEAS._3rdParty.BouncyCastle.util.io;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
    internal abstract class LimitedInputStream
       : BaseInputStream
    {
        protected readonly Stream _in;
        private int _limit;

        internal LimitedInputStream(Stream inStream, int limit)
        {
            this._in = inStream;
            this._limit = limit;
        }

        internal virtual int Limit
        {
            get { return _limit; }
        }

        protected virtual void SetParentEofDetect(bool on)
        {
            if (_in is IndefiniteLengthInputStream)
            {
                ((IndefiniteLengthInputStream)_in).SetEofOn00(on);
            }
        }
    }
}
