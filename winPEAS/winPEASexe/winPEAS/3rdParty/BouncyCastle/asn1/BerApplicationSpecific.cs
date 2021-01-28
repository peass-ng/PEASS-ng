using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.asn1
{
	public class BerApplicationSpecific
		  : DerApplicationSpecific
	{
		public BerApplicationSpecific(
			int tagNo,
			Asn1EncodableVector vec)
			: base(tagNo, vec)
		{
		}
	}
}
