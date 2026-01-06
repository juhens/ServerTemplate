using System;
using System.Security.Cryptography;
namespace ServerCore.Cipher
{
    internal static class KeyMaker
    {
        internal static byte[] CreateKey(long encryptionSeed)
        {
            var finalKey = new byte[32];
            Span<byte> workBuffer = stackalloc byte[32];
            Span<byte> hashOutput = stackalloc byte[32];

            if (!BitConverter.TryWriteBytes(workBuffer, encryptionSeed))
            {
                throw new InvalidOperationException("Critical Error: Failed to write encryption seed to stack buffer.");
            }

            long mutator = 0x5F3759DF;
            for (var i = 0; i < workBuffer.Length; i++)
            {
                var b = (byte)(workBuffer[i % 8] ^ (byte)i);
                b = (byte)((b << 3) | (b >> 5));
                mutator = (mutator * 214013L + 2531011L) & 0xFFFFFFFF;
                workBuffer[i] = (byte)(b ^ (byte)(mutator >> 16));
            }

            SHA256.HashData(workBuffer, hashOutput);

            for (var i = 0; i < finalKey.Length; i++)
            {
                finalKey[i] = (byte)(hashOutput[i] ^ hashOutput[i % 32]);
            }



            return finalKey;
        }

        internal static byte[] CreateKey(Guid encryptionSeed)
        {
            var finalKey = new byte[32];
            Span<byte> workBuffer = stackalloc byte[32];
            Span<byte> hashOutput = stackalloc byte[32];

            // 1. Guid 쓰기 (0~15)
            if (!encryptionSeed.TryWriteBytes(workBuffer))
            {
                throw new InvalidOperationException("Critical Error: Failed to write encryption seed to stack buffer.");
            }

            // 2. 남은 공간(16~31)을 Guid 뒤집은 값으로 채우기 (Obfuscation)
            var guidBytes = workBuffer[..16];
            var padding = workBuffer.Slice(16, 16);
            guidBytes.CopyTo(padding);
            padding.Reverse();

            // 3. 기존 Mutator 로직 그대로 적용 (해커 귀찮게 하기)
            long mutator = 0x5F3759DF;
            for (var i = 0; i < workBuffer.Length; i++)
            {
                var b = (byte)(workBuffer[i % 8] ^ (byte)i);
                b = (byte)((b << 3) | (b >> 5));
                mutator = (mutator * 214013L + 2531011L) & 0xFFFFFFFF;
                workBuffer[i] = (byte)(b ^ (byte)(mutator >> 16));
            }

            SHA256.HashData(workBuffer, hashOutput);

            for (var i = 0; i < finalKey.Length; i++)
            {
                finalKey[i] = (byte)(hashOutput[i] ^ hashOutput[i % 32]);
            }

            return finalKey;
        }
    }
}
