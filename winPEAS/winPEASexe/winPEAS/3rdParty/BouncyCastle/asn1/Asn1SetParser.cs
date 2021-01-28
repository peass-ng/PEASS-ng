using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.asn1
{
	public interface Asn1SetParser
		 : IAsn1Convertible
	{
		IAsn1Convertible ReadObject();
	}
}
