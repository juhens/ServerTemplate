using System;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Numerics;

namespace ServerCore.Cipher
{
    public sealed class AesCipher : ICipher, IDisposable
    {
        private Aes? _aes;
        private ICryptoTransform? _encryptor;

        // L1 Cache (32KB~48KB) 고려: 4KB 버퍼 사용
        private const int BlockSize = 16;
        private const int BatchBlockCount = 256;
        private const int BufferSize = BlockSize * BatchBlockCount;
        private const int StackAllocThreshold = 1024;

        private readonly byte[] _counterBuffer = new byte[BufferSize];
        private readonly byte[] _keyStreamBuffer = new byte[BufferSize];
        private readonly byte[] _currentCounter = new byte[BlockSize];

        private int _bufferOffset = BufferSize;
        private bool IsInit { get; set; }

        private static ReadOnlySpan<byte> DefaultIv => new byte[]
        {
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
            0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00
        };

        public void Init(byte[] key)
        {
            if (IsInit)
            {
                _encryptor?.Dispose();
                _aes?.Dispose();
            }

            _aes = Aes.Create();
            _aes.Key = key;
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
            _encryptor = _aes.CreateEncryptor();

            DefaultIv.CopyTo(_currentCounter);
            _bufferOffset = BufferSize;
            IsInit = true;
        }

        public void Encrypt(Span<byte> data) => Transform(data);
        public void Decrypt(Span<byte> data) => Transform(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encrypt(ReadOnlySpan<byte> plainData, Span<byte> cipherData)
        {
            if (!IsInit) return;

            var len = plainData.Length;
            if (cipherData.Length < len) throw new ArgumentException("Output buffer is too short.");

            if (len <= StackAllocThreshold)
            {
                // 작은 데이터는 스택 할당 (빠름)
                Span<byte> stackKey = stackalloc byte[len];
                if (_bufferOffset + len > BufferSize) RefillKeyStream();

                Unsafe.CopyBlockUnaligned(
                    ref MemoryMarshal.GetReference(stackKey),
                    ref _keyStreamBuffer[_bufferOffset],
                    (uint)len
                );
                _bufferOffset += len;

                XorBlockVectorized(
                    ref MemoryMarshal.GetReference(plainData),
                    ref MemoryMarshal.GetReference(cipherData),
                    ref MemoryMarshal.GetReference(stackKey),
                    len
                );
            }
            else
            {
                // 큰 데이터는 루프 처리
                ref var srcRef = ref MemoryMarshal.GetReference(plainData);
                ref var dstRef = ref MemoryMarshal.GetReference(cipherData);
                var currentOffset = 0;

                while (len > 0)
                {
                    if (_bufferOffset >= BufferSize) RefillKeyStream();
                    var remaining = BufferSize - _bufferOffset;
                    var processSize = len < remaining ? len : remaining;

                    XorBlockVectorized(
                        ref Unsafe.Add(ref srcRef, currentOffset),
                        ref Unsafe.Add(ref dstRef, currentOffset),
                        ref _keyStreamBuffer[_bufferOffset],
                        processSize
                    );

                    _bufferOffset += processSize;
                    currentOffset += processSize;
                    len -= processSize;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Transform(Span<byte> data)
        {
            if (!IsInit) return;
            var len = data.Length;

            if (len <= StackAllocThreshold)
            {
                Span<byte> stackKey = stackalloc byte[len];
                if (_bufferOffset + len > BufferSize) RefillKeyStream();

                Unsafe.CopyBlockUnaligned(
                    ref MemoryMarshal.GetReference(stackKey),
                    ref _keyStreamBuffer[_bufferOffset],
                    (uint)len
                );
                _bufferOffset += len;

                XorBlockVectorized(
                    ref MemoryMarshal.GetReference(data),
                    ref MemoryMarshal.GetReference(stackKey),
                    len
                );
            }
            else
            {
                ref var dataRef = ref MemoryMarshal.GetReference(data);
                var currentDataOffset = 0;

                while (len > 0)
                {
                    if (_bufferOffset >= BufferSize) RefillKeyStream();
                    var remaining = BufferSize - _bufferOffset;
                    var processSize = len < remaining ? len : remaining;

                    XorBlockVectorized(
                        ref Unsafe.Add(ref dataRef, currentDataOffset),
                        ref _keyStreamBuffer[_bufferOffset],
                        processSize
                    );
                    _bufferOffset += processSize;
                    currentDataOffset += processSize;
                    len -= processSize;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefillKeyStream()
        {
            ref var counterBufRef = ref MemoryMarshal.GetReference(_counterBuffer.AsSpan());
            ref var currentCounterRef = ref MemoryMarshal.GetReference(_currentCounter.AsSpan());

            for (var i = 0; i < BatchBlockCount; i++)
            {
                Unsafe.CopyBlockUnaligned(
                    ref Unsafe.Add(ref counterBufRef, i * BlockSize),
                    ref currentCounterRef,
                    BlockSize
                );
                IncrementCurrentCounter();
            }

            _encryptor!.TransformBlock(_counterBuffer, 0, BufferSize, _keyStreamBuffer, 0);
            _bufferOffset = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementCurrentCounter()
        {
            ref var counterStart = ref MemoryMarshal.GetReference(_currentCounter.AsSpan());
            ref var lowRef = ref Unsafe.Add(ref counterStart, 8);
            var low = BinaryPrimitives.ReadUInt64BigEndian(MemoryMarshal.CreateReadOnlySpan(ref lowRef, 8));
            low++;
            BinaryPrimitives.WriteUInt64BigEndian(MemoryMarshal.CreateSpan(ref lowRef, 8), low);

            if (low == 0)
            {
                var high = BinaryPrimitives.ReadUInt64BigEndian(MemoryMarshal.CreateReadOnlySpan(ref counterStart, 8));
                high++;
                BinaryPrimitives.WriteUInt64BigEndian(MemoryMarshal.CreateSpan(ref counterStart, 8), high);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void XorBlockVectorized(ref byte dataRef, ref byte keyRef, int len)
        {
            var i = 0;
            if (Vector.IsHardwareAccelerated)
            {
                var vectorSize = Vector<byte>.Count;
                while (len - i >= vectorSize)
                {
                    var vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref dataRef, i));
                    var vKey = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref keyRef, i));
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref dataRef, i), vData ^ vKey);
                    i += vectorSize;
                }
            }
            while (i < len)
            {
                Unsafe.Add(ref dataRef, i) ^= Unsafe.Add(ref keyRef, i);
                i++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void XorBlockVectorized(ref byte srcRef, ref byte dstRef, ref byte keyRef, int len)
        {
            var i = 0;
            if (Vector.IsHardwareAccelerated)
            {
                var vectorSize = Vector<byte>.Count;
                while (len - i >= vectorSize)
                {
                    var vSrc = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref srcRef, i));
                    var vKey = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref keyRef, i));
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref dstRef, i), vSrc ^ vKey);
                    i += vectorSize;
                }
            }
            while (i < len)
            {
                var s = Unsafe.Add(ref srcRef, i);
                var k = Unsafe.Add(ref keyRef, i);
                Unsafe.Add(ref dstRef, i) = (byte)(s ^ k);
                i++;
            }
        }

        public void Dispose()
        {
            _encryptor?.Dispose();
            _aes?.Dispose();
            IsInit = false;
        }
    }
}