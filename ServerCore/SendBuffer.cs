using System;
using System.Buffers;

namespace ServerCore
{
    public class SendBuffer : IDisposable
    {
        private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create(MaxBufferSize, 16384);

        private const int MaxBufferSize = 512 * 1024;     // 512KB
        private const int DefaultBufferSize = 128 * 1024; // 128KB (i7 8700 10000유저 20존 초당 5회 500^2 * 5 테스트)

        private byte[]? _buffer;
        private int _writtenCount;

        public SendBuffer()
        {
            _buffer = null;
            _writtenCount = 0;
        }

        public bool IsEmpty => _writtenCount == 0;

        public void Reserve(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            var requiredSize = _writtenCount + count;

            if (requiredSize > MaxBufferSize)
            {
                throw new InvalidOperationException($"Buffer overflow. Max: {MaxBufferSize}, Required: {requiredSize}");
            }

            if (_buffer == null)
            {
                var initialSize = Math.Max(DefaultBufferSize, RoundUpToPowerOf2(requiredSize));
                _buffer = Pool.Rent(initialSize);
            }
            else if (requiredSize > _buffer.Length)
            {
                ExpandBuffer(requiredSize);
            }
        }

        public void Append(ReadOnlySpan<byte> source)
        {
            Reserve(source.Length);
            source.CopyTo(_buffer.AsSpan(_writtenCount));
            _writtenCount += source.Length;
        }

        public Span<byte> Open(int size)
        {
            Reserve(size);
            return _buffer.AsSpan(_writtenCount, size);
        }

        public void Close(int size)
        {
            _writtenCount += size;
        }

        public void Clear()
        {
            _writtenCount = 0;
        }

        public void Dispose()
        {
            if (_buffer == null) return;

            Pool.Return(_buffer);
            _buffer = null;
            _writtenCount = 0;
        }

        private static int RoundUpToPowerOf2(int num)
        {
            if (num < 0) return 0;
            if (num == 0) return 1;

            var n = (uint)num - 1;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return (int)(n + 1);
        }

        private void ExpandBuffer(int requiredSize)
        {
            var newSize = RoundUpToPowerOf2(requiredSize);

            if (newSize > MaxBufferSize) newSize = MaxBufferSize;
            if (requiredSize > newSize) throw new InvalidOperationException($"Buffer overflow.");

            var newBuffer = Pool.Rent(newSize);

            if (_writtenCount > 0)
            {
                _buffer.AsSpan(0, _writtenCount).CopyTo(newBuffer);
            }

            Pool.Return(_buffer!);
            _buffer = newBuffer;
        }

        public ArraySegment<byte> GetUsedSegment()
        {
            if (_buffer == null) return ArraySegment<byte>.Empty;
            return new ArraySegment<byte>(_buffer, 0, _writtenCount);
        }
    }
}