using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace GameServer.Cache
{
    // Value 용
    public interface IValue
    {

    }


    // Object 용
    // 아래 컴파일 오류 방지용 더미임
    public class Dummy{}



    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(Dummy))]
    internal partial class CacheModelJsonContext : JsonSerializerContext
    {
    }

    public class ChannelInfoCm
    {
        public string Ip { get; init; } = string.Empty;
        public ushort Port { get; init; }
        public short Index { get; init; }
        public int SessionCount { get; init; }
        public DateTime LastUpdated { get; init; }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChannelInfoCm2
    {
        public short Index;
        public int SessionCount;
        public DateTime LastUpdated;
    }

}
