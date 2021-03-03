namespace winPEAS._3rdParty.BouncyCastle.crypto.modes.gcm
{
	public interface IGcmExponentiator
	{
		void Init(byte[] x);
		void ExponentiateX(long pow, byte[] output);
	}
}
