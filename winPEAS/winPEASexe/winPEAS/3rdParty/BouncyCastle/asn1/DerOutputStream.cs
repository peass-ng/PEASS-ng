using System;
using System.IO;
using winPEAS._3rdParty.BouncyCastle.asn1.util;

namespace winPEAS._3rdParty.BouncyCastle.asn1
{
	public class DerOutputStream
	 : FilterStream
	{
		public DerOutputStream(Stream os)
			: base(os)
		{
		}

		private void WriteLength(
			int length)
		{
			if (length > 127)
			{
				int size = 1;
				uint val = (uint)length;

				while ((val >>= 8) != 0)
				{
					size++;
				}

				WriteByte((byte)(size | 0x80));

				for (int i = (size - 1) * 8; i >= 0; i -= 8)
				{
					WriteByte((byte)(length >> i));
				}
			}
			else
			{
				WriteByte((byte)length);
			}
		}

		internal void WriteEncoded(
			int tag,
			byte[] bytes)
		{
			WriteByte((byte)tag);
			WriteLength(bytes.Length);
			Write(bytes, 0, bytes.Length);
		}

		internal void WriteEncoded(
			int tag,
			byte first,
			byte[] bytes)
		{
			WriteByte((byte)tag);
			WriteLength(bytes.Length + 1);
			WriteByte(first);
			Write(bytes, 0, bytes.Length);
		}

		internal void WriteEncoded(
			int tag,
			byte[] bytes,
			int offset,
			int length)
		{
			WriteByte((byte)tag);
			WriteLength(length);
			Write(bytes, offset, length);
		}

		internal void WriteTag(
			int flags,
			int tagNo)
		{
			if (tagNo < 31)
			{
				WriteByte((byte)(flags | tagNo));
			}
			else
			{
				WriteByte((byte)(flags | 0x1f));
				if (tagNo < 128)
				{
					WriteByte((byte)tagNo);
				}
				else
				{
					byte[] stack = new byte[5];
					int pos = stack.Length;

					stack[--pos] = (byte)(tagNo & 0x7F);

					do
					{
						tagNo >>= 7;
						stack[--pos] = (byte)(tagNo & 0x7F | 0x80);
					}
					while (tagNo > 127);

					Write(stack, pos, stack.Length - pos);
				}
			}
		}

		internal void WriteEncoded(
			int flags,
			int tagNo,
			byte[] bytes)
		{
			WriteTag(flags, tagNo);
			WriteLength(bytes.Length);
			Write(bytes, 0, bytes.Length);
		}

		protected void WriteNull()
		{
			WriteByte(Asn1Tags.Null);
			WriteByte(0x00);
		}

		[Obsolete("Use version taking an Asn1Encodable arg instead")]
		public virtual void WriteObject(
			object obj)
		{
			if (obj == null)
			{
				WriteNull();
			}
			else if (obj is Asn1Object)
			{
				((Asn1Object)obj).Encode(this);
			}
			else if (obj is Asn1Encodable)
			{
				((Asn1Encodable)obj).ToAsn1Object().Encode(this);
			}
			else
			{
				throw new IOException("object not Asn1Object");
			}
		}

		public virtual void WriteObject(
			Asn1Encodable obj)
		{
			if (obj == null)
			{
				WriteNull();
			}
			else
			{
				obj.ToAsn1Object().Encode(this);
			}
		}

		public virtual void WriteObject(
			Asn1Object obj)
		{
			if (obj == null)
			{
				WriteNull();
			}
			else
			{
				obj.Encode(this);
			}
		}
	}
}
