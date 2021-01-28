using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.asn1
{
    /**
    * A Null object.
    */
    public abstract class Asn1Null
        : Asn1Object
    {
        internal Asn1Null()
        {
        }

        public override string ToString()
        {
            return "NULL";
        }
    }
}
