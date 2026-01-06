using System.Collections.Concurrent;
using GameServer.Game.Dto;

namespace GameServer.Database
{
    // Redis와 같은 인메모리 캐시/세션 저장소를 흉내냅니다.
    // 실제 환경: LoginServer가 Redis에 {Token : PlayerDbId}를 저장했다고 가정.
    public static class MockRedis
    {
        // [Shadow Boxing]
        // 실제로는 Redis.StringGet(token) 호출


        // 인메모리 보관용
        private static readonly ConcurrentDictionary<int, WorldInfoDto> WorldInfoDict = [];


        public static long? GetUserIdByToken(string token)
        {
            // 테스트를 위해 토큰 문자열 자체가 ID라고 가정하고 파싱 시도
            // 예: "100" -> 100
            if (long.TryParse(token, out var dbId))
            {
                return dbId;
            }

            // 파싱 실패 시 (잘못된 토큰)
            return null;
        }

        public static void UpdateWorldInfoList(List<WorldInfoDto> worldInfoList)
        {
            foreach (var worldInfo in worldInfoList)
            {
                WorldInfoDict.TryAdd(worldInfo.WorldStaticId, worldInfo);
            }
        }

        public static List<WorldInfoDto> GetWorldInfoList()
        {
            var worldInfoList = new List<WorldInfoDto>();
            foreach (var worldInfo in WorldInfoDict.Values)
            {
                worldInfoList.Add(worldInfo);
            }

            return worldInfoList;
        }
    }
}