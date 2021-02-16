using System;
using System.IO;
using winPEAS._3rdParty.BouncyCastle.crypto.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1.util
{
    [Obsolete("Use Org.BouncyCastle.Utilities.IO.FilterStream")]
    public class FilterStream : Stream
    {
        [Obsolete("Use Org.BouncyCastle.Utilities.IO.FilterStream")]
        public FilterStream(Stream s)
        {
            this.s = s;
        }
        public override bool CanRead
        {
            get { return s.CanRead; }
        }
        public override bool CanSeek
        {
            get { return s.CanSeek; }
        }
        public override bool CanWrite
        {
            get { return s.CanWrite; }
        }
        public override long Length
        {
            get { return s.Length; }
        }
        public override long Position
        {
            get { return s.Position; }
            set { s.Position = value; }
        }
#if PORTABLE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Platform.Dispose(s);
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            Platform.Dispose(s);
            base.Close();
        }
#endif
        public override void Flush()
        {
            s.Flush();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return s.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            s.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return s.Read(buffer, offset, count);
        }
        public override int ReadByte()
        {
            return s.ReadByte();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            s.WriteByte(value);
        }
        protected readonly Stream s;
    }
}
