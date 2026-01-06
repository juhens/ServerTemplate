using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Contexts.Transaction;
using GameServer.Network;
using ServerCore;

namespace GameServer.Game.Commands.Transaction
{
    public static class LogoutCommand
    {
        public static void Execute(ClientSession session)
        {
            // 트랜잭션 충돌 방어
            if (!session.Transaction.TrySetState(TransactionState.Logout)) return;

            var accInfo = session.Routing.AccountDbIdRef.TryCapture(out var a) ? $"AccountDbId:{a}" : "";
            var playerInfo = session.Routing.PlayerRef.TryCapture(out var p) ? $"PlayerDbId:{p.PlayerDbId}) " : "";
            var worldInfo = session.Routing.WorldRef.TryCapture(out var w) ? $"World:{w.StaticId} " : "";
            var chInfo = session.Routing.ChannelRef.TryCapture(out var c) ? $"Channel:{c.Index} " : "";
            var zoneInfo = session.Routing.ZoneRef.TryCapture(out var z) ? $"Zone:{z.StaticId} " : "";

            Log.Info(typeof(LogoutCommand), "[Begin] Logout Session:{0} {1}{2}{3}{4}{5}", 
                session.RuntimeId, accInfo, playerInfo, worldInfo, chInfo, zoneInfo);

            var ctx = LogoutContext.Create();

            if (session.Routing.ZoneRef.TryCapture(out var zone))
            {
                zone.Leave(session, ctx, OnZoneLeft);
                return;
            }

            OnZoneLeft(session, ctx);
        }

        private static void OnZoneLeft(ClientSession session, LogoutContext ctx)
        {
            if (session.Routing.ChannelRef.TryCapture(out var channel))
            {
                channel.Leave(session, ctx, OnChannelLeft);
                return;
            }

            OnChannelLeft(session, ctx);
        }

        private static void OnChannelLeft(ClientSession session, LogoutContext ctx)
        {
            if (session.Routing.WorldRef.TryCapture(out var world))
            {
                world.Leave(session, ctx, OnWorldLeft);
                return;
            }

            OnWorldLeft(session, ctx);
        }

        private static void OnWorldLeft(ClientSession session, LogoutContext ctx)
        {
            if (session.Routing.PlayerRef.TryCapture(out var player))
            {
                var playerDb = player.ToPlayerDb();
                ctx.PlayerDb = playerDb;
                DbManager.Instance.SavePlayerDb(session, ctx, OnPlayerSaved);
                return;
            }

            OnPlayerSaved(session, ctx);
        }

        private static void OnPlayerSaved(ClientSession session, LogoutContext ctx)
        {
            if (session.Routing.AccountDbIdRef.TryCapture(out var accountDbId))
            {
                ctx.AccountDbId = accountDbId;
                DbManager.Instance.Logout(session, ctx, OnLogout);
                return;
            }

            OnLogout(session, ctx);
        }

        private static void OnLogout(ClientSession session, LogoutContext ctx)
        {
            Log.Info(typeof(LogoutCommand), "[ End ] Session:{0}", session.RuntimeId);
        }
    }
}