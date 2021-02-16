using System;
using System.IO;

namespace winPEAS._3rdParty.BouncyCastle.util.io
{
    public abstract class BaseInputStream : Stream
    {
        private bool closed;

        public sealed override bool CanRead { get { return !closed; } }
        public sealed override bool CanSeek { get { return false; } }
        public sealed override bool CanWrite { get { return false; } }

#if PORTABLE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closed = true;
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            closed = true;
            base.Close();
        }
#endif

        public sealed override void Flush() { }
        public sealed override long Length { get { throw new NotSupportedException(); } }
        public sealed override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int pos = offset;
            try
            {
                int end = offset + count;
                while (pos < end)
                {
                    int b = ReadByte();
                    if (b == -1) break;
                    buffer[pos++] = (byte)b;
                }
            }
            catch (IOException)
            {
                if (pos == offset) throw;
            }
            return pos - offset;
        }

        public sealed override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public sealed override void SetLength(long value) { throw new NotSupportedException(); }
        public sealed override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }
}
