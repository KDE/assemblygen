namespace QtCore
{
	using System;
	using System.IO;

	public class StreamWrapper : QIODevice
	{
		private Stream m_stream;

		public StreamWrapper(Stream stream, bool open = true)
			: base()
		{
			m_stream = stream;
			if (!open) return;
			if (stream.CanRead && stream.CanWrite)
				Open(OpenModeFlag.ReadWrite);
			else if (stream.CanRead)
				Open(OpenModeFlag.ReadOnly);
			else if (stream.CanWrite)
				Open(OpenModeFlag.WriteOnly);
		}

		protected override unsafe long ReadData(Pointer<sbyte> data, long maxsize)
		{
			int max = (maxsize > int.MaxValue) ? int.MaxValue : (int) maxsize;
			byte[] buffer = new byte[max];
			int read = m_stream.Read(buffer, 0, max);
			for (int i = 0; i < max; i++)
			{
				data[i] = (sbyte) buffer[i];
			}
			return read;
		}

		protected override long WriteData(string data, long maxsize)
		{
			int max = (maxsize > int.MaxValue) ? int.MaxValue : (int) maxsize;
			for (int i = 0; i < max; i++)
			{
				m_stream.WriteByte((byte) data[i]);
			}
			return max;
		}

		public override bool Seek(long pos)
		{
			base.Seek(pos);
			m_stream.Seek(pos, SeekOrigin.Begin);
			return true;
		}

		public override long Size
		{
			get
			{
				if (!m_stream.CanSeek) return base.Size;
				return m_stream.Length;
			}
		}

		public override void Close()
		{
			m_stream.Close();
			base.Close();
		}
	}
}
