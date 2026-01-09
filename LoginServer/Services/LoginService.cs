using LoginServer.Data;
using StackExchange.Redis;

namespace LoginServer.Services
{
    public class LoginService
    {
        // 임시 설정
        private const string GameServerIp = "127.0.0.1";
        private const int GameServerPort = 7777;

        // Redis(Garnet) 데이터베이스 인터페이스
        private readonly IDatabase _cacheDb;

        // 생성자를 통해 의존성 주입 (Program.cs에서 등록한 연결을 여기서 받음)
        public LoginService(IConnectionMultiplexer redis)
        {
            _cacheDb = redis.GetDatabase();
        }

        // 로그인 로직 처리
        public async Task<LoginResponse?> ProcessLogin(LoginRequest request)
        {
            // TODO: DB연결전 임시.. 
            if (request.Id == request.Password)
            {
                var accountDbId = request.Id;
                var newToken = Guid.NewGuid().ToString();

                var sessionKey = $"session:account:{accountDbId}";

                // string? oldToken = dict[sessionKey];
                var oldToken = await _cacheDb.StringGetAsync(sessionKey);

                if (!oldToken.IsNullOrEmpty)
                {
                    // dict.Remove($"auth:token:{oldToken}");
                    await _cacheDb.KeyDeleteAsync($"auth:token:{oldToken}");
                    Console.WriteLine($"기존 토큰 만료: User {accountDbId}, OldToken {oldToken}");
                }

                // dict[$"auth:token:{newToken}"] = accountDbId; (유효기간 5분)
                await _cacheDb.StringSetAsync($"auth:token:{newToken}", accountDbId, TimeSpan.FromMinutes(5));
                // dict[sessionKey] = newToken; (유효기간 5분)
                await _cacheDb.StringSetAsync(sessionKey, newToken, TimeSpan.FromMinutes(5));

                Console.WriteLine($"토큰 발급: User {accountDbId}, NewToken {newToken}");

                return new LoginResponse(newToken, GameServerIp, GameServerPort);
            }

            return null;
        }
    }
}