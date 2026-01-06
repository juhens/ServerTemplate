using System;
using System.Buffers;

namespace ServerCore
{
    internal class RecvBuffer : IDisposable
    {
        private const int BufferSize = 128 * 1024;
        private const int MaxPacketSize = 64 * 1024;
        private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create(BufferSize, 16384);

        private byte[]? _buffer;
        private readonly int _capacity;

        private int _readPos;
        private int _writePos;

        // 이중 Dispose 방지
        private bool _disposed;

        public RecvBuffer()
        {
            _buffer = Pool.Rent(BufferSize);
            _capacity = _buffer.Length;
        }

        public int DataSize => _writePos - _readPos;
        private int FreeSize => _capacity - _writePos;

        public ArraySegment<byte> ReadSegment => new(_buffer!, _readPos, DataSize);
        public ArraySegment<byte> WriteSegment => new(_buffer!, _writePos, FreeSize);

        public void Trim()
        {
            if (_buffer == null) return;

            var dataSize = DataSize;
            if (dataSize == 0)
            {
                _readPos = 0;
                _writePos = 0;
                return;
            }

            if (FreeSize > MaxPacketSize)
                return;

            Array.Copy(_buffer, _readPos, _buffer, 0, dataSize);
            _readPos = 0;
            _writePos = dataSize;
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize) return false;
            _readPos += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize) return false;
            _writePos += numOfBytes;
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_buffer is null) return;

            Pool.Return(_buffer);
            _buffer = null;
        }
    }
}