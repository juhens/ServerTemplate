using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Contexts.Transaction;
using GameServer.Network;
using PacketGen;
using ServerCore;
using ServerCore.Packet;

namespace GameServer.Game.Commands.Transaction
{
    public static class LoginCommand
    {
        public static void Execute(ClientSession session, C_Login loginPacket)
        {
            // 트랜잭션 충돌 방어
            if (!session.Transaction.TrySetState(TransactionState.Busy)) return;

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("LoginCommand.Execute:Disconnected");
            }
            else
            {
                Log.Info(typeof(LoginCommand), "[Begin] Session:{0}", session.RuntimeId);
                var ctx = LoginContext.Create();
                ctx.SessionToken = loginPacket.SessionToken;

                DbManager.Instance.Login(session, ctx, OnLogin);
            }
        }

        private static void OnLogin(ClientSession session, LoginContext ctx)
        {
            var result = new S_LoginResponse();
            switch (ctx.Result)
            {
                case TransactionResult.DummyAuthFailed:
                    result.ServerResult = ServerResult.Failed;
                    session.Transaction.FailedWithLastMessage("LoginCommand.OnLogin:Dummy auth error", result.Encode());
                    return;
                case TransactionResult.InvalidToken:
                    result.ServerResult = ServerResult.Failed;
                    session.Transaction.FailedWithLastMessage("LoginCommand.OnLogin:InvalidToken", result.Encode());
                    return;
                case TransactionResult.BannedAccount:
                    result.ServerResult = ServerResult.Banned;
                    session.Transaction.FailedWithLastMessage("LoginCommand.OnLogin:InvalidToken", result.Encode());
                    break;
                case TransactionResult.DuplicateAuth:
                    var exitSession = ctx.ExitSession!;
                    var systemMsg = new S_SystemMessage();
                    systemMsg.Message = "다른 위치에서 로그인 되었습니다.";
                    exitSession.DisconnectWithLastMessage("LoginCommand.OnLogin::DuplicateAuth", systemMsg.Encode());
                    Reject(session, ctx);
                    return;
                case TransactionResult.TryAgainLater:
                    Reject(session, ctx);
                    return;
            }


            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnLogin:Disconnected");
            }
            else
            {
                Log.Info(typeof(LoginCommand), "[ End ] Session:{RuntimeId} Account:{AccountDbId}", session.RuntimeId, ctx.AccountDbId);
                result.ServerResult = ServerResult.Success;
                session.Send(result.Encode());
                session.Transaction.ReleaseState();
            }
        }


        private static void Reject(ClientSession session, LoginContext ctx)
        {
            if (session.Routing.AccountDbIdRef.TryCapture(out var accountDb))
            {
                DbManager.Instance.Logout(session, ctx, OnLogout);
                return;
            }
            OnLogout(session, ctx);
        }

        private static void OnLogout(ClientSession session, LoginContext ctx)
        {
            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnLogin:Disconnected");
            }
            else
            {
                Log.Info(typeof(LoginCommand), "[Reject] Session:{RuntimeId} Account:{AccountDbId}", session.RuntimeId, ctx.AccountDbId);
                var result = new S_LoginResponse();

                switch (ctx.Result)
                {
                    case TransactionResult.TryAgainLater:
                        result.ServerResult = ServerResult.TryAgainLater;
                        break;
                    case TransactionResult.DuplicateAuth:
                        result.ServerResult = ServerResult.DuplicateAuth;
                        break;
                    default:
                        result.ServerResult = ServerResult.UnknownError;
                        break;
                }
                session.Send(result.Encode());
                session.Transaction.ReleaseState();
            }
        }
    }
}
