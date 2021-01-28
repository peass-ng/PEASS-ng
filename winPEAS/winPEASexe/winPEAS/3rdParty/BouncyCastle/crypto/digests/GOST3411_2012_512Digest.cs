using System;
using System.IO;
using System;
using System.IO;

using BrowserPass.BouncyCastle;
using BrowserPass.BouncyCastle.crypto.util;
using BrowserPass.BouncyCastle.util;
using System;
using System.Diagnostics;
using BrowserPass.BouncyCastle.util.io;



namespace BrowserPass.BouncyCastle.Crypto.Digests
{
    public class Gost3411_2012_512Digest:Gost3411_2012Digest
    {
		private readonly static byte[] IV = {
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
	};

		public override string AlgorithmName
		{
			get { return "GOST3411-2012-512"; }
		}

        public Gost3411_2012_512Digest():base(IV)
        {
        }

		public Gost3411_2012_512Digest(Gost3411_2012_512Digest other) : base(IV)
		{
            Reset(other);
        }

        public override int GetDigestSize()
        {
            return 64;
        }

		public override IMemoable Copy()
		{
			return new Gost3411_2012_512Digest(this);
		}
    }
}
