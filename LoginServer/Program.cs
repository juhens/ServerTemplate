using System.Text.Json.Serialization;
using LoginServer.Data;
using LoginServer.Services;

namespace LoginServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = null;
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });
            
            builder.Services.AddSingleton<LoginService>();

            var app = builder.Build();

            // POST /login 엔드포인트 설정
            app.MapPost("/login", (LoginRequest request, LoginService loginService) =>
            {
                // LoginService를 통해 로그인 처리
                var response = loginService.ProcessLogin(request);

                if (response != null)
                {
                    return Results.Ok(response);
                }

                // 로그인 실패 시 401 Unauthorized 반환
                return Results.Unauthorized();
            });

            app.Run();
        }
    }

    [JsonSerializable(typeof(LoginRequest))]
    [JsonSerializable(typeof(LoginResponse))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}