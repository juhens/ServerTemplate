using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ServerCore.Packet
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public ushort TotalSize;
        public ushort Checksum;

        private const uint HeaderKey = 0x9A3B5C7Du;
        private const int RotateShift = 5;

        // 단순 난독화임
        // TODO: 카운팅도 섞을것
        public void Encrypt()
        {
            var tempKey = HeaderKey;
            ref var keyBase = ref Unsafe.As<uint, byte>(ref tempKey);
            ref var dataBase = ref Unsafe.As<PacketHeader, byte>(ref this);

            var dataSize = Unsafe.SizeOf<PacketHeader>();
            const int keySize = sizeof(uint);

            for (var i = 0; i < dataSize; i++)
            {
                ref var dataByte = ref Unsafe.Add(ref dataBase, i);
                ref var keyByte = ref Unsafe.Add(ref keyBase, i % keySize);

                dataByte ^= keyByte;
                dataByte = (byte)((dataByte << RotateShift) | (dataByte >> (8 - RotateShift)));
            }
        }

        public void Decrypt()
        {
            var tempKey = HeaderKey;
            ref var keyBase = ref Unsafe.As<uint, byte>(ref tempKey);
            ref var dataBase = ref Unsafe.As<PacketHeader, byte>(ref this);

            var dataSize = Unsafe.SizeOf<PacketHeader>();
            const int keySize = sizeof(uint);

            for (var i = 0; i < dataSize; i++)
            {
                ref var dataByte = ref Unsafe.Add(ref dataBase, i);
                ref var keyByte = ref Unsafe.Add(ref keyBase, i % keySize);

                dataByte = (byte)((dataByte >> RotateShift) | (dataByte << (8 - RotateShift)));
                dataByte ^= keyByte;
            }
        }
    }
}