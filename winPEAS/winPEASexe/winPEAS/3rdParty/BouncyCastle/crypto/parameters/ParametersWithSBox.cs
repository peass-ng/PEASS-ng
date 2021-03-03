namespace winPEAS._3rdParty.BouncyCastle.crypto.parameters
{
	public class ParametersWithSBox : ICipherParameters
	{
		private ICipherParameters parameters;
		private byte[] sBox;

		public ParametersWithSBox(
			ICipherParameters parameters,
			byte[] sBox)
		{
			this.parameters = parameters;
			this.sBox = sBox;
		}

		public byte[] GetSBox() { return sBox; }

		public ICipherParameters Parameters { get { return parameters; } }
	}
}
