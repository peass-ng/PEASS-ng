namespace winPEAS._3rdParty.BouncyCastle.crypto.modes.gcm
{
	public interface IGcmMultiplier
	{
		void Init(byte[] H);
		void MultiplyH(byte[] x);
	}
}
