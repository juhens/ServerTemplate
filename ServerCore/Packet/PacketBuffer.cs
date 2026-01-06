using System;

namespace ServerCore.Packet
{
    public static class PacketBufferHelper
    {
        private const int ChunkSize = 81920; // 80 KB

        [ThreadStatic]
        private static PacketBuffer? _currentBuffer;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (reserveSize > ChunkSize)
            {
                throw new ArgumentOutOfRangeException(nameof(reserveSize), $"Packet size ({reserveSize}) cannot exceed ChunkSize ({ChunkSize})");
            }

            if (_currentBuffer == null || _currentBuffer.FreeSize < reserveSize)
            {
                _currentBuffer = new PacketBuffer(ChunkSize);
            }
            return _currentBuffer.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            if (_currentBuffer == null)
            {
                // 실행 불가 시나리오
                throw new InvalidOperationException("[PacketBuffer] Critical Error: Buffer is null on Close.");
            }
            return _currentBuffer.Close(usedSize);
        }
    }

    public class PacketBuffer
    {
        private readonly byte[] _buffer;

        public PacketBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }

        private int _usedSize = 0;

        public int FreeSize => _buffer.Length - _usedSize;

        public ArraySegment<byte> Open(int reserveSize)
        {
            if (reserveSize > FreeSize) throw new Exception($"Buffer overflow. Request: {reserveSize}, Free: {FreeSize}");
            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            var result = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;
            return result;
        }
    }
}