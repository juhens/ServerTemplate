using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Contexts.Transaction;
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
                session.Disconnect("Not logged account");
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

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("EnterZoneCommand.Execute:Disconnected");
            }
            else
            {
                Log.Info(typeof(EnterZoneCommand), "[Begin] Session:{0} AccountDbId{1}", session.RuntimeId, accountDbId);
                var ctx = EnterZoneContext.Create();
                ctx.AccountDbId = accountDbId;
                ctx.World = dstWorld;
                ctx.Channel = dstChannel;
                ctx.PlayerIndex = packet.PlayerIndex;

                DbManager.Instance.LoadPlayer(session, ctx, OnLoadPlayer);
            }
        }
        private static void OnLoadPlayer(ClientSession session, EnterZoneContext ctx)
        {
            if (ctx.Result != TransactionResult.Success)
            {
                var response = new S_EnterZoneResult();
                response.ServerResult = ServerResult.UnknownError;
                session.Transaction.FailedWithLastMessage("OnLoadPlayer: Failed load player", response.Encode());
                return;
            }

            var zoneStaticId = 0;
            var zone = ctx.Channel.FindZone(zoneStaticId);
            if (zone is null)
            {
                var response = new S_EnterZoneResult();
                response.ServerResult = ServerResult.UnknownError;
                session.Transaction.FailedWithLastMessage($"OnLoadPlayer: Not found zone:{zoneStaticId}", response.Encode());
                return;
            }
            ctx.Zone = zone;
            var player = new Player(ctx.PlayerDb);

            if (!session.Routing.PlayerRef.TryAttach(player))
            {
                ctx.Result = TransactionResult.FailedAttach;
            }

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnLoadPlayer:Disconnected");
            }
            else
            {
                ctx.World.Enter(session, ctx, OnWorldEnter);
            }
        }
        private static void OnWorldEnter(ClientSession session, EnterZoneContext ctx)
        {
            if (ctx.Result != TransactionResult.Success)
            {
                session.Transaction.Failed($"OnWorldEnter:{ctx.Result}");
                return;
            }

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnWorldEnter:Disconnected");
            }
            else
            {
                ctx.Channel.Enter(session, ctx, OnChannelEnter);
            }
        }
        private static void OnChannelEnter(ClientSession session, EnterZoneContext ctx)
        {
            if (ctx.Result != TransactionResult.Success)
            {
                session.Transaction.Failed($"OnChannelEnter:{ctx.Result}");
                return;
            }

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnChannelEnter:Disconnected");
            }
            else
            {
                ctx.Zone.Enter(session, ctx, OnZoneEnter);
            }
        }
        private static void OnZoneEnter(ClientSession session, EnterZoneContext ctx)
        {
            if (ctx.Result != TransactionResult.Success)
            {
                session.Transaction.Failed($"OnZoneEnter:{ctx.Result}");
                return;
            }

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnZoneEnter:Disconnected");
            }
            else
            {
                var response = new S_EnterZoneResult();
                response.ServerResult = ServerResult.Success;
                response.ZoneStaticId = ctx.Zone.StaticId;
                session.Send(response.Encode());
                session.Transaction.ReleaseState();
            }
        }


        // TODO: 추후 채널꽉참 구현시 필요
        private static void Reject(ClientSession session, EnterZoneContext ctx)
        {
            if (session.Routing.ZoneRef.TryCapture(out var zone))
            {
                zone.Leave(session, ctx, OnZoneLeave);
                return;
            }

            OnZoneLeave(session, ctx);
        }
        private static void OnZoneLeave(ClientSession session, EnterZoneContext ctx)
        {
            if (session.Routing.ChannelRef.TryCapture(out var channel))
            {
                channel.Leave(session, ctx, OnChannelLeave);
                return;
            }

            OnChannelLeave(session, ctx);
        }
        private static void OnChannelLeave(ClientSession session, EnterZoneContext ctx)
        {
            if (session.Routing.WorldRef.TryCapture(out var world))
            {
                world.Leave(session, ctx, OnWorldLeave);
                return;
            }

            OnWorldLeave(session, ctx);
        }
        private static void OnWorldLeave(ClientSession session, EnterZoneContext ctx)
        {
            if (session.Routing.PlayerRef.TryCapture(out var player))
            {
                DbManager.Instance.DetachPlayer(session, ctx, OnDetachPlayer);
                return;
            }

            OnDetachPlayer(session, ctx);
        }
        private static void OnDetachPlayer(ClientSession session, EnterZoneContext ctx)
        {
            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnZoneEnter.OnDetachPlayer:Disconnected");
            }
            else
            {
                var response = new S_EnterZoneResult();
                response.ServerResult = ServerResult.Failed;
                session.Send(response.Encode());
                session.Transaction.ReleaseState();
            }
        }
    }
}
