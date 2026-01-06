using System.Collections.Concurrent;

namespace GameServer.Database
{
    // EF Core의 DbContext를 흉내내는 Mock Context
    // 실제 도입 시: public class GameDbContext : DbContext
    namespace Server.Database
    {
        public class GameDbContext : IDisposable
        {
            private static readonly ConcurrentDictionary<long, List<PlayerDb>> MockPlayerDb = new();
            private static long _playerIdCounter = 0;

            public GameDbContext() { }
            public void SaveChanges() { }
            public void Dispose() { }

            public void AddPlayer(PlayerDb player)
            {
                if (player.PlayerDbId == 0)
                {
                    player.PlayerDbId = Interlocked.Increment(ref _playerIdCounter);
                }

                var playerList = MockPlayerDb.GetOrAdd(player.AccountDbId, _ => new List<PlayerDb>());

                // 리스트 자체는 스레드 안전하지 않으므로 lock 처리 (Mock이라도 기본 원칙 준수)
                lock (playerList)
                {
                    playerList.Add(player);
                }
            }

            // [변경 3] 반환 타입을 List로 변경 및 조회 로직 수정
            public List<PlayerDb> FindPlayers(long accountId, short worldStaticId)
            {
                if (MockPlayerDb.TryGetValue(accountId, out var list))
                {
                    // List<T>.FindAll 메서드를 사용하여 조건에 맞는 요소들로 구성된 새 리스트 반환
                    return list.FindAll(p => p.WorldStaticId == worldStaticId);
                }

                return [];
            }
        }
    }
}