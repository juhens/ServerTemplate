namespace GameServer.Logic.Dto
{
    public class PlayerInfoDto
    {
        public required short PlayerIndex { get; init; }
        public required string PlayerName { get; init; }
        public required short Level { get; init; }
        public required DateTime LastPlayedTime { get; init; }
    }
}
