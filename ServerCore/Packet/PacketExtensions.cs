using System;
using System.Runtime.CompilerServices;
using ServerCore.Util;

namespace ServerCore.Packet
{
    public static class PacketExtensions
    {
        private static readonly int HeaderSize = Unsafe.SizeOf<PacketHeader>();

        public static ArraySegment<byte> Encode(this IPacket packet)
        {
            var maxPayloadSize = packet.GetMaxByteCount();
            var maxPacketSize = HeaderSize + Unsafe.SizeOf<ushort>() + maxPayloadSize;

            var buffer = PacketBufferHelper.Open(maxPacketSize);
            var writer = new PacketWriter(buffer);

            writer.Skip(HeaderSize);
            writer.WriteUInt16(packet.Protocol);
            packet.Serialize(ref writer);

            if (writer.Used > ushort.MaxValue)
            {
                throw new InvalidOperationException($"Protocol:{packet.Protocol} Packet size exceeded {ushort.MaxValue} bytes. Actual: {writer.Used}");
            }

            buffer = PacketBufferHelper.Close(writer.Used);

            ref var header = ref Unsafe.As<byte, PacketHeader>(ref buffer.Array![buffer.Offset]);

            header.TotalSize = (ushort)buffer.Count;
            header.Checksum = (ushort)Crc32C.Compute(buffer.Array!, buffer.Offset + HeaderSize, buffer.Count - HeaderSize);
            header.Encrypt();

            return buffer;
        }
    }
}