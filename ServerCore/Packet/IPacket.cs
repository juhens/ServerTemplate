namespace ServerCore.Packet
{
    public interface IStruct
    {
        void Serialize(ref PacketWriter w);
        void Deserialize(ref PacketReader r);
        int GetMaxByteCount();
    }

    public interface IPacket : IStruct
    {
        public ushort Protocol { get; }
    }
}
