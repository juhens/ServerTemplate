using ServerCore.Cipher;
using ServerCore.Util;
using System;
using System.Runtime.CompilerServices;

namespace ServerCore.Packet
{
    public abstract class PacketSession : Session
    {
        protected PacketSession(AppSide appSide): base(appSide) {}
        private readonly int _headerSize = Unsafe.SizeOf<PacketHeader>();
        
        protected sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            // 메시지 프레이밍
            var processLen = 0;
            while (true)
            {
                if (buffer.Count < _headerSize) break;

                // 복제본
                var header = Unsafe.As<byte, PacketHeader>(ref buffer.Array![buffer.Offset]);
                header.Decrypt();

                if (buffer.Count < header.TotalSize) break;
                if (header.TotalSize < _headerSize) throw new Exception("Invalid packet size");

                var payloadSize = header.TotalSize - _headerSize;
                var payload = new ArraySegment<byte>(buffer.Array!, buffer.Offset + _headerSize, payloadSize);

                // 원본에 복호화 데이터 덮어씀
                if (CipherSuite.IsRecvEnabled) CipherSuite.Decrypt(payload);

                var crc32 = (ushort)Crc32C.Compute(payload.Array!, payload.Offset, payload.Count);
                if (header.Checksum != crc32) throw new Exception("Packet corrupted!");

                var protocolId = BitConverter.ToUInt16(payload.Array!, payload.Offset);
                var packetData = new ArraySegment<byte>(payload.Array!, payload.Offset + sizeof(ushort), payloadSize - sizeof(ushort));

                // 원본 버퍼에 복호화 데이터를 덮어썼기때문에
                // 반드시 동기적으로 역직렬화 완료되어야 함.
                OnRecvPacket(protocolId, packetData);

                processLen += header.TotalSize;
                buffer = new ArraySegment<byte>(buffer.Array!, buffer.Offset + header.TotalSize, buffer.Count - header.TotalSize);
            }
            return processLen;
        }
        protected abstract void OnRecvPacket(ushort protocolId, ArraySegment<byte> buffer);
    }
}
