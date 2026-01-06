using System;
using System.Runtime.CompilerServices;

namespace ServerCore.Cipher
{
    public sealed class MockCipher : ICipher, IDisposable
    {
        public bool IsInit { get; private set; } = true;

        public void Init(byte[] key) { IsInit = true; }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decrypt(Span<byte> data) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encrypt(ReadOnlySpan<byte> plainData, Span<byte> cipherData)
        {
            // 실제 데이터 이동 비용을 측정하기 위해 CopyTo 수행
            plainData.CopyTo(cipherData);
        }
    }
}