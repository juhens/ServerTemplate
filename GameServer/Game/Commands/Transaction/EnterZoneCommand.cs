using GameServer.Database;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using GameServer.Game.Commands.Transaction.Contexts.Transaction;
using GameServer.Game.Objects;
using GameServer.Network;
using PacketGen;
using ServerCore;
using ServerCore.Packet;

namespace GameServer.Game.Commands.Transaction
{
    public static class EnterZoneCommand
    {
        public static void Execute(ClientSession session, C_EnterZone packet)
        {
            // 트랜잭션 충돌 방어
            if (!session.Transaction.TrySetState(TransactionState.Busy)) return;

            // 로그인 상태 체크
            if (!session.Routing.AccountDbIdRef.TryCapture(out var accountDbId))
            {
                session.Transaction.Failed("Not logged account");
                return;
            }

            var dstWorld = Node.Instance.FindWorld(packet.WorldStaticId);
            if (dstWorld is null)
            {
                session.Transaction.Failed("EnterZoneCommand.Execute:Invalid world");
                return;
            }
            var dstChannel = dstWorld.FindChannel(packet.ChannelIndex);
            if (dstChannel is null)
            {
                session.Transaction.Failed("EnterZoneCommand.Execute:Invalid channel");
                return;
            }

            Log.Info(typeof(EnterZoneCommand), "[Begin] Session:{0} AccountDbId{1}", session.RuntimeId, accountDbId);

            var ctx = session.Transaction.CreateContext<EnterZoneContext>();
            ctx.Session = session;
            ctx.AccountDbId = accountDbId;
            ctx.World = dstWorld;
            ctx.Channel = dstChannel;
            ctx.PlayerIndex = packet.PlayerIndex;
            ctx.OnCompleted = OnFindPlayer;
            DbManager.Instance.FindPlayer(ctx);
        }
        private static void OnFindPlayer(EnterZoneContext ctx)
        {
            if (ctx.Result == TransactionResult.Success)
            {
                ctx.OnCompleted = OnLoadPlayer;
                DbManager.Instance.LoadPlayer(ctx);
            }
            else
            {
                OnLoadPlayer(ctx);
            }
        }

        private static void OnLoadPlayer(EnterZoneContext ctx)
        {
            var session = ctx.Session;
            switch (ctx.Result)
            {
                // find player
                case TransactionResult.BadPlayerIndex:
                case TransactionResult.FailedFindPlayer:
                // load player
                case TransactionResult.BadPlayerDbId:
                case TransactionResult.FailedLoadPlayer:
                case TransactionResult.FailedAttach:
                    var response = new S_EnterZoneResult();
                    response.ServerResult = ServerResult.UnknownError;
                    session.Transaction.FailedWithLastMessage($"OnLoadPlayer:{ctx.Result}", response.Encode());
                    return;

                case TransactionResult.Success:
                    break;

                case TransactionResult.Disconnected:
                default:
                    session.Transaction.Failed($"OnLoadPlayer:{ctx.Result}");
                    return;
            }


            // TODO : 추후 ctx에서 빼올것
            // var zoneStaticId = ctx.PlayerDb.ZoneStaticId;
            var zoneStaticId = 0;
            var zone = ctx.Channel.FindZone(zoneStaticId);
            if (zone is null)
            {
                session.Transaction.Failed("OnLoadPlayer:Invalid zone");
                return;
            }

            ctx.Zone = zone;

            if (!session.Routing.PlayerRef.TryAttach(new Player(ctx.PlayerDb)))
            {
                session.Transaction.Failed("OnLoadPlayer:Failed attach");
                return;
            }

            ctx.OnCompleted = OnWorldEnter;
            ctx.World.Enter(ctx);

        }

        private static void OnWorldEnter(EnterZoneContext ctx)
        {
            var session = ctx.Session;

            switch (ctx.Result)
            {
                case TransactionResult.NotRoutedPlayer:
                case TransactionResult.DuplicateRuntimeId:
                case TransactionResult.DuplicateNickname:
                case TransactionResult.FailedOnEnter:
                case TransactionResult.Disconnected:
                    session.Transaction.Failed($"OnWorldEnter:{ctx.Result}");
                    return;
            }

            ctx.OnCompleted = OnChannelEnter;
            ctx.Channel.Enter(ctx);
        }
        private static void OnChannelEnter(EnterZoneContext ctx)
        {
            var session = ctx.Session;

            switch (ctx.Result)
            {
                case TransactionResult.NotRoutedPlayer:
                case TransactionResult.DuplicateRuntimeId:
                case TransactionResult.DuplicateNickname:
                case TransactionResult.FailedOnEnter:
                case TransactionResult.Disconnected:
                    session.Transaction.Failed($"OnChannelEnter:{ctx.Result}");
                    return;
            }

            ctx.OnCompleted = OnZoneEnter;
            ctx.Zone.Enter(ctx);
        }
        private static void OnZoneEnter(EnterZoneContext ctx)
        {
            var session = ctx.Session;

            switch (ctx.Result)
            {
                case TransactionResult.NotRoutedPlayer:
                case TransactionResult.DuplicateRuntimeId:
                case TransactionResult.DuplicateNickname:
                case TransactionResult.FailedOnEnter:
                case TransactionResult.Disconnected:
                    session.Transaction.Failed($"OnZoneEnter:{ctx.Result}");
                    return;
            }

            var response = new S_EnterZoneResult();
            response.ServerResult = ServerResult.Success;
            response.ZoneStaticId = ctx.Zone.StaticId;
            session.Send(response.Encode());
            session.Transaction.ReleaseState();
        }
    }
}
