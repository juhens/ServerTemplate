using GameServer.Database.Server.Database;

namespace GameServer.Database.Commands
{
    public static class DbCommand
    {
        // [변경 4] 반환 타입을 List<PlayerDb>로 변경
        public static List<PlayerDb> GetPlayerList(long accountDbId, short worldStaticId)
        {
            using var db = new GameDbContext();
            // FindPlayer -> FindPlayers로 변경된 메서드 호출
            return db.FindPlayers(accountDbId, worldStaticId);
        }

        public static PlayerDb CreatePlayer(long accountDbId, short worldStaticId, string nickname)
        {
            using var db = new GameDbContext();

            var player = new PlayerDb
            {
                AccountDbId = accountDbId,
                WorldStaticId = worldStaticId,
                Nickname = nickname,
            };

            db.AddPlayer(player);
            db.SaveChanges();

            return player;
        }

        public static void SavePlayer(PlayerDb playerDb)
        {
            // 리스트 내부의 객체 참조를 그대로 쓰고 있으므로 Mock에서는 별도 구현 불필요
            // 실제 DB라면 Update 쿼리 날리는 로직 필요
        }

        internal static object GetAccount(long accountDbId)
        {
            return null!;
        }
    }
}