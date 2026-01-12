using GameServer.Game.Commands.Transaction.Contexts.Transaction;
using LobbyServer.Network;
using PacketGen;
using ServerCore;
using ServerCore.Infrastructure;
using ServerCore.Packet;

namespace LobbyServer.Lobby.Commands.Transaction
{
    public static class LoginCommand
    {
        public static void Execute(ClientSession session, C_Login loginPacket)
        {
            if (!session.Transaction.TrySetBusy()) return;

            Log.Info(typeof(LoginCommand), "[Begin] Session:{0}", session.RuntimeId);

            var ctx = session.Transaction.CreateContext<LoginContext>();
            ctx.Session = session;
            ctx.SessionToken = loginPacket.SessionToken;
            ctx.OnCompleted = OnAuth;

            DbManager.Instance.Auth(ctx);
        }
        private static void OnAuth(LoginContext ctx)
        {
            if (ctx.Result == TransactionResult.Success)
            {
                ctx.OnCompleted = OnLogin;
                DbManager.Instance.Login(ctx);
            }
            else
            {
                OnLogin(ctx);
            }
        }
        private static void OnLogin(LoginContext ctx)
        {
            var session = ctx.Session;
            var loginResponse = new S_LoginResponse();

            switch (ctx.Result)
            {
                // auth
                case TransactionResult.FailedDummyAuth:
                case TransactionResult.InvalidToken:
                    loginResponse.ServerResult = ServerResult.Failed;
                    session.Transaction.FailedWithLastMessage($"LoginCommand.Auth:{ctx.Result}", loginResponse.Encode());
                    return;
                // login
                case TransactionResult.BannedAccount:
                    loginResponse.ServerResult = ServerResult.Banned;
                    session.Transaction.FailedWithLastMessage($"LoginCommand.Login:{ctx.Result}", loginResponse.Encode());
                    return;
                case TransactionResult.DuplicateAuth:
                    loginResponse.ServerResult = ServerResult.DuplicateAuth;
                    var exitSession = ctx.ExitSession!;
                    var systemMsg = new S_SystemMessage();
                    systemMsg.Message = "다른 위치에서 로그인 되었습니다.";
                    exitSession.DisconnectWithLastMessage($"LoginCommand.Login:{ctx.Result}", systemMsg.Encode());
                    break;
                case TransactionResult.TryAgainLater:
                    loginResponse.ServerResult = ServerResult.TryAgainLater;
                    break;

                // common
                case TransactionResult.Disconnected:
                    session.Transaction.Failed($"LoginCommand:{ctx.Result}");
                    return;
                case TransactionResult.Success:

                    loginResponse.ServerResult = ServerResult.Success;
                    break;

                default:
                    loginResponse.ServerResult = ServerResult.UnknownError;
                    session.Transaction.FailedWithLastMessage($"LoginCommand.Login:{ctx.Result}", loginResponse.Encode());
                    return;
            }
            session.Send(loginResponse.Encode());
            Log.Info(typeof(LoginCommand), "[ End ] {0} Session:{1} Account:{2}", ctx.Result, session.RuntimeId, ctx.AccountDbId);
            session.Transaction.ReleaseState();
        }
    }
}
