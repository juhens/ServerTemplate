namespace GameServer.Logic.Dto
{
    public class WorldInfoDto
    {
        public required short WorldStaticId { get; init; }
        public required string WorldName { get; init; }
        public required List<ChannelInfoDto> ChannelInfoList { get; init; }
    }
}
