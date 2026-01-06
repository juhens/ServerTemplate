using LoginServer.Data;

namespace LoginServer.Services
{
    public class LoginService
    {
        // 임시
        private const string GameServerIp = "127.0.0.1";
        private const int GameServerPort = 7777;

        // 로그인 로직 처리
        public LoginResponse? ProcessLogin(LoginRequest request)
        {
            // 더미  테스트용
            if (request.Id == request.Password)
            {
                // 더미 클라이언트 테스트용
                // 토큰은 ID와 동일한 값을 반환 (나중에 DbId로 사용될 값)
                var token = request.Id;

                return new LoginResponse(token, GameServerIp, GameServerPort);
            }

            // 로그인 실패 시 null 반환
            return null;
        }
    }
}