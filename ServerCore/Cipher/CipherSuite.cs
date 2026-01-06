using System;

namespace ServerCore.Cipher
{
    public enum AppSide
    {
        Server,
        Client
    }

    public sealed class CipherSuite : IDisposable
    {
        private ICipher _sendCipher;
        private ICipher _recvCipher;

        private readonly AppSide _side;

        internal bool IsSendEnabled;
        internal bool IsRecvEnabled;

        internal CipherSuite(AppSide side)
        {
            _side = side;
            _sendCipher = new MockCipher();
            _recvCipher = new MockCipher();
        }

        internal void Init(byte[] key)
        {
            DisposeCiphers();
            var key1 = new ArraySegment<byte>(key, 0, 16).ToArray();
            var key2 = new ArraySegment<byte>(key, 16, 16).ToArray();
            if (_side == AppSide.Server)
            {
                _sendCipher = new AesCipher();
                _recvCipher = new AesCipher();
                _sendCipher.Init(key1);
                _recvCipher.Init(key2);
            }
            else
            {
                _sendCipher = new AesCipher();
                _recvCipher = new AesCipher();
                _sendCipher.Init(key2);
                _recvCipher.Init(key1);
            }
        }

        internal void Encrypt(ReadOnlySpan<byte> plainData, Span<byte> cipherData)
        {
            _sendCipher.Encrypt(plainData, cipherData);
        }

        internal void Decrypt(Span<byte> plainData)
        {
            _recvCipher.Decrypt(plainData);
        }


        private void DisposeCiphers()
        {
            (_sendCipher as IDisposable)?.Dispose();
            (_recvCipher as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            DisposeCiphers();
        }
    }
}
