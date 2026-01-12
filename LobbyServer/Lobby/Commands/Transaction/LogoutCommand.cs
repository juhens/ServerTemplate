using GameServer.Game.Commands.Transaction.Contexts.Transaction;
using LobbyServer.Network;
using ServerCore;

namespace LobbyServer.Lobby.Commands.Transaction
{
    // 특별 커맨드
    // 1. Failed 금지, 예외나 실패시 핸들링하면서 무조건 끝까지 진행되게 해야함
    // 2. 자원회수 수동으로
    public static class LogoutCommand
    {
        public static void Execute(ClientSession session)
        {
            // 트랜잭션 충돌 방어
            if (!session.Transaction.TrySetLogout()) return;

            var accInfo = session.Routing.AccountDbIdRef.TryCapture(out var a) ? $"AccountDbId:{a}" : "";
            var playerInfo = session.Routing.PlayerRef.TryCapture(out var p) ? $"PlayerDbId:{p.PlayerDbId}) " : "";
            var worldInfo = session.Routing.WorldRef.TryCapture(out var w) ? $"World:{w.StaticId} " : "";
            var chInfo = session.Routing.ChannelRef.TryCapture(out var c) ? $"Channel:{c.Index} " : "";
            var zoneInfo = session.Routing.ZoneRef.TryCapture(out var z) ? $"Zone:{z.StaticId} " : "";

            Log.Info(typeof(LogoutCommand), "[Begin] Session:{0} {1}{2}{3}{4}{5}", 
                session.RuntimeId, accInfo, playerInfo, worldInfo, chInfo, zoneInfo);

            // 현재 로그아웃은 Failed 트랜잭션을 호출하지 않으니 마지막에 수동으로 해제!!!
            var ctx = session.Transaction.CreateContext<LogoutContext>();
            ctx.Session = session;
            
            if (session.Routing.ZoneRef.TryCapture(out var zone))
            {
                ctx.OnCompleted = OnZoneLeft;
                zone.Leave(ctx);
            }
            else
            {
                OnZoneLeft(ctx);
            }
        }

        private static void OnZoneLeft(LogoutContext ctx)
        {
            if (ctx.Session.Routing.ChannelRef.TryCapture(out var channel))
            {
                ctx.OnCompleted = OnChannelLeft;
                channel.Leave(ctx);
            }
            else
            {
                OnChannelLeft(ctx);
            }
        }

        private static void OnChannelLeft(LogoutContext ctx)
        {
            if (ctx.Session.Routing.WorldRef.TryCapture(out var world))
            {
                ctx.OnCompleted = OnWorldLeft;
                world.Leave(ctx);
            }
            else
            {
                OnWorldLeft(ctx);
            }
        }

        private static void OnWorldLeft(LogoutContext ctx)
        {
            if (ctx.Session.Routing.PlayerRef.TryCapture(out var player))
            {
                var playerDb = player.ToPlayerDb();
                ctx.PlayerDb = playerDb;
                ctx.OnCompleted = OnPlayerSaved;
                DbManager.Instance.SavePlayerWithDetach(ctx);
            }
            else
            {
                OnPlayerSaved(ctx);
            }
        }

        private static void OnPlayerSaved(LogoutContext ctx)
        {
            if (ctx.Session.Routing.AccountDbIdRef.TryCapture(out var accountDbId))
            {
                ctx.AccountDbId = accountDbId;
                ctx.OnCompleted = OnLogout;
                DbManager.Instance.Logout(ctx);
            }
            else
            {
                OnLogout(ctx);
            }
        }

        private static void OnLogout(LogoutContext ctx)
        {
            Log.Info(typeof(LogoutCommand), "[ End ] Session:{0}", ctx.Session.RuntimeId);
            ctx.Dispose();
        }
    }
}