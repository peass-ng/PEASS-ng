using System;
using winPEAS._3rdParty.BouncyCastle.crypto.util;
using winPEAS._3rdParty.BouncyCastle.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	/**
     * Der IA5String object - this is an ascii string.
     */
	public class DerIA5String
		: DerStringBase
	{
		private readonly string str;

		/**
         * return a IA5 string from the passed in object
         *
         * @exception ArgumentException if the object cannot be converted.
         */
		public static DerIA5String GetInstance(
			object obj)
		{
			if (obj == null || obj is DerIA5String)
			{
				return (DerIA5String)obj;
			}

			throw new ArgumentException("illegal object in GetInstance: " + Platform.GetTypeName(obj));
		}

		/**
         * return an IA5 string from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */
		public static DerIA5String GetInstance(
			Asn1TaggedObject obj,
			bool isExplicit)
		{
			Asn1Object o = obj.GetObject();

			if (isExplicit || o is DerIA5String)
			{
				return GetInstance(o);
			}

			return new DerIA5String(((Asn1OctetString)o).GetOctets());
		}

		/**
         * basic constructor - with bytes.
         */
		public DerIA5String(
			byte[] str)
			: this(Strings.FromAsciiByteArray(str), false)
		{
		}

		/**
		* basic constructor - without validation.
		*/
		public DerIA5String(
			string str)
			: this(str, false)
		{
		}

		/**
		* Constructor with optional validation.
		*
		* @param string the base string to wrap.
		* @param validate whether or not to check the string.
		* @throws ArgumentException if validate is true and the string
		* contains characters that should not be in an IA5String.
		*/
		public DerIA5String(
			string str,
			bool validate)
		{
			if (str == null)
				throw new ArgumentNullException("str");
			if (validate && !IsIA5String(str))
				throw new ArgumentException("string contains illegal characters", "str");

			this.str = str;
		}

		public override string GetString()
		{
			return str;
		}

		public byte[] GetOctets()
		{
			return Strings.ToAsciiByteArray(str);
		}

		internal override void Encode(
			DerOutputStream derOut)
		{
			derOut.WriteEncoded(Asn1Tags.IA5String, GetOctets());
		}

		protected override int Asn1GetHashCode()
		{
			return this.str.GetHashCode();
		}

		protected override bool Asn1Equals(
			Asn1Object asn1Object)
		{
			DerIA5String other = asn1Object as DerIA5String;

			if (other == null)
				return false;

			return this.str.Equals(other.str);
		}

		/**
		 * return true if the passed in String can be represented without
		 * loss as an IA5String, false otherwise.
		 *
		 * @return true if in printable set, false otherwise.
		 */
		public static bool IsIA5String(
			string str)
		{
			foreach (char ch in str)
			{
				if (ch > 0x007f)
				{
					return false;
				}
			}

			return true;
		}
	}
}
