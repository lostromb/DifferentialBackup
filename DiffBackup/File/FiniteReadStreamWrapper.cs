
namespace DiffBackup.File
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple stream wrapper which allows for reading either the stream's natural
    /// read length, or a fixed maximum length of bytes, whichever comes first.
    /// Useful for getting a "preview" of a potentially long stream.
    /// </summary>
    internal class FiniteReadStreamWrapper : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _maxLengthBytes;

        public FiniteReadStreamWrapper(Stream innerStream, long maxLengthBytes)
        {
            _innerStream = innerStream;
            _maxLengthBytes = maxLengthBytes;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => Math.Min(_maxLengthBytes, _innerStream.Length);

        public override long Position
        {
            get => _innerStream.Position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int maxRead = Math.Min(count, (int)(_maxLengthBytes - _innerStream.Position));
            if (maxRead <= 0)
            {
                return 0;
            }

            return _innerStream.Read(buffer, offset, maxRead);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
