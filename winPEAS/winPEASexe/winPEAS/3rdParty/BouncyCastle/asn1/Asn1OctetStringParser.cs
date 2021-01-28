using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.asn1
{
	public interface Asn1OctetStringParser
		 : IAsn1Convertible
	{
		Stream GetOctetStream();
	}
}
