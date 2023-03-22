using System;
using System.IO;
using GuardNet;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class ReadOnceStream : Stream
    {
        private readonly Stream _innerStream;
        private bool _alreadyRead;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnceStream" /> class.
        /// </summary>
        public ReadOnceStream(Stream innerStream)
        {
            Guard.NotNull(innerStream, nameof(innerStream));
            Guard.For<ArgumentException>(() => !innerStream.CanRead, "Requires a readable stream to represents a read-once stream");

            _innerStream = innerStream;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_alreadyRead is false)
            {
                int result = _innerStream.Read(buffer, offset, count);

                // No need to do this multi-threaded as new instances are only called from the test.
                _alreadyRead = result == 0;

                return result;
            }

            throw new InvalidOperationException("Cannot read more than once in a read-once stream");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => throw new InvalidOperationException("Cannot reset the stream position in a read-once stream");
        }
    }
}