namespace LoginServer.Data
{
    // 요청 DTO
    public record LoginRequest(string Id, string Password);

    // 응답 DTO
    public record LoginResponse(string Token, string GameServerIp, int Port);

    // 에러 응답 DTO (선택 사항)
    public record ErrorResponse(string ErrorMessage);
}