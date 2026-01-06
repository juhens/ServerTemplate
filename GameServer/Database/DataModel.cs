namespace GameServer.Database
{
    public class PlayerDb
    {
        // FK
        public long AccountDbId { get; init; }
        // PK
        public long PlayerDbId { get; set; }
        // UNIQUE
        public string Nickname { get; init; } = string.Empty;

        public short WorldStaticId { get; init; }
        public short Index { get; set; }

        // 기타 게임 데이터 (레벨, 경험치 등)

    }
}