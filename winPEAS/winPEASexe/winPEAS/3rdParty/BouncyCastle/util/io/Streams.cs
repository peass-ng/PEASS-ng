using System.IO;

namespace winPEAS._3rdParty.BouncyCastle.util.io
{
	public sealed class Streams
	{
		private const int BufferSize = 512;

		private Streams()
		{
		}

		public static void Drain(Stream inStr)
		{
			byte[] bs = new byte[BufferSize];
			while (inStr.Read(bs, 0, bs.Length) > 0)
			{
			}
		}

		public static byte[] ReadAll(Stream inStr)
		{
			MemoryStream buf = new MemoryStream();
			PipeAll(inStr, buf);
			return buf.ToArray();
		}

		public static byte[] ReadAllLimited(Stream inStr, int limit)
		{
			MemoryStream buf = new MemoryStream();
			PipeAllLimited(inStr, limit, buf);
			return buf.ToArray();
		}

		public static int ReadFully(Stream inStr, byte[] buf)
		{
			return ReadFully(inStr, buf, 0, buf.Length);
		}

		public static int ReadFully(Stream inStr, byte[] buf, int off, int len)
		{
			int totalRead = 0;
			while (totalRead < len)
			{
				int numRead = inStr.Read(buf, off + totalRead, len - totalRead);
				if (numRead < 1)
					break;
				totalRead += numRead;
			}
			return totalRead;
		}

		public static void PipeAll(Stream inStr, Stream outStr)
		{
			byte[] bs = new byte[BufferSize];
			int numRead;
			while ((numRead = inStr.Read(bs, 0, bs.Length)) > 0)
			{
				outStr.Write(bs, 0, numRead);
			}
		}

		/// <summary>
		/// Pipe all bytes from <c>inStr</c> to <c>outStr</c>, throwing <c>StreamFlowException</c> if greater
		/// than <c>limit</c> bytes in <c>inStr</c>.
		/// </summary>
		/// <param name="inStr">
		/// A <see cref="Stream"/>
		/// </param>
		/// <param name="limit">
		/// A <see cref="System.Int64"/>
		/// </param>
		/// <param name="outStr">
		/// A <see cref="Stream"/>
		/// </param>
		/// <returns>The number of bytes actually transferred, if not greater than <c>limit</c></returns>
		/// <exception cref="IOException"></exception>
		public static long PipeAllLimited(Stream inStr, long limit, Stream outStr)
		{
			byte[] bs = new byte[BufferSize];
			long total = 0;
			int numRead;
			while ((numRead = inStr.Read(bs, 0, bs.Length)) > 0)
			{
				if ((limit - total) < numRead)
					throw new StreamOverflowException("Data Overflow");
				total += numRead;
				outStr.Write(bs, 0, numRead);
			}
			return total;
		}

		/// <exception cref="IOException"></exception>
		public static void WriteBufTo(MemoryStream buf, Stream output)
		{
			buf.WriteTo(output);
		}

		/// <exception cref="IOException"></exception>
		public static int WriteBufTo(MemoryStream buf, byte[] output, int offset)
		{
#if PORTABLE
            byte[] bytes = buf.ToArray();
            bytes.CopyTo(output, offset);
            return bytes.Length;
#else
			int size = (int)buf.Length;
			buf.WriteTo(new MemoryStream(output, offset, size, true));
			return size;
#endif
		}

		public static void WriteZeroes(Stream outStr, long count)
		{
			byte[] zeroes = new byte[BufferSize];
			while (count > BufferSize)
			{
				outStr.Write(zeroes, 0, BufferSize);
				count -= BufferSize;
			}
			outStr.Write(zeroes, 0, (int)count);
		}
	}
}
