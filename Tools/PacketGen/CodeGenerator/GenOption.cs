using System.Text.Json.Serialization;

namespace PacketGen.CodeGenerator
{
    public class GenOption
    {
        public string Version { get; set; } = "0.0.0.0";
        public bool IsProtocolRandomize { get; set; } = false;
        public string Namespace { get; set; } = "TempNameSpaceName";
        public string ServerExportPath { get; set; } = "";
        public string ClientExportPath { get; set; } = "";
    }

    [JsonSerializable(typeof(GenOption))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
