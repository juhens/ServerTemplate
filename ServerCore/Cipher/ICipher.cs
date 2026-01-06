using System;

namespace ServerCore.Cipher
{
    public interface ICipher
    {
        public void Encrypt(ReadOnlySpan<byte> plainData, Span<byte> cipherData);
        public void Decrypt(Span<byte> data);
        public void Init(byte[] key);
    }
}
