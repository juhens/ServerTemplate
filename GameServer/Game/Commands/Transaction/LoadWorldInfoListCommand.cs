using GameServer.Database;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using GameServer.Game.Commands.Transaction.Contexts.Transaction;
using GameServer.Network;
using PacketGen;
using ServerCore;
using ServerCore.Packet;

namespace GameServer.Game.Commands.Transaction
{
    public static class LoadWorldInfoListCommand
    {
        public static void Execute(ClientSession session, C_RequestWorldInfoArray packet)
        {
            // 트랜잭션 충돌 방어
            if (!session.Transaction.TrySetState(TransactionState.Busy)) return;

            // 로그인 상태 체크
            if (!session.Routing.AccountDbIdRef.TryCapture(out var accountDbId))
            {
                session.Transaction.Failed("Not logged account");
                return;
            }

            Log.Info(typeof(LoadWorldInfoListCommand), "[Begin] Session:{0} AccountDbId{1}", session.RuntimeId, accountDbId);

            var ctx = session.Transaction.CreateContext<LoadWorldInfoListContext>();
            ctx.Session = session;
            ctx.AccountDbId = accountDbId;
            ctx.OnCompleted = OnLoadWorldInfoList;

            DbManager.Instance.LoadWorldInfoList(ctx);
        }

        private static void OnLoadWorldInfoList(LoadWorldInfoListContext ctx)
        {
            var session = ctx.Session;
            var response = new S_WorldInfoArray();
            switch (ctx.Result)
            {
                case TransactionResult.FailedLoadWorldInfoList:
                case TransactionResult.Disconnected:
                    session.Transaction.Failed($"OnLoadPlayerInfoList:{ctx.Result}");
                    return;
            }

            response.ServerResult = ServerResult.Success;

            var worldInfoList = new List<WorldInfo>();
            foreach (var worldInfoDto in ctx.WorldInfoList)
            {
                var world = Node.Instance.FindWorld(worldInfoDto.WorldStaticId);
                if (world is null) continue;
                var worldPlayerCount = world.SessionCount;

                var worldInfo = new WorldInfo();
                worldInfo.WorldStaticId = worldInfoDto.WorldStaticId;
                worldInfo.WorldName = worldInfoDto.WorldName;
                worldInfo.PlayerCount = worldPlayerCount;
                var channelInfoList = new List<ChannelInfo>();
                foreach (var channelInfoDto in worldInfoDto.ChannelInfoList)
                {
                    var channel = world.FindChannel(channelInfoDto.ChannelIndex);
                    if (channel is null) continue;
                    var channelPlayerCount = channel.SessionCount;
                    var channelInfo = new ChannelInfo();
                    channelInfo.ChannelIndex = channelInfoDto.ChannelIndex;
                    channelInfo.PlayerCount = channelPlayerCount;
                    channelInfoList.Add(channelInfo);
                }
                worldInfo.ChannelInfoArray = channelInfoList.ToArray();
                worldInfoList.Add(worldInfo);
            }
            response.WorldInfoArray = worldInfoList.ToArray();

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnLoadWorldInfoList:Disconnected");
            }
            else
            {
                Log.Info(typeof(LoadWorldInfoListCommand), "[ End ] Session:{0} AccountDbId{1}", session.RuntimeId, ctx.AccountDbId);
                session.Send(response.Encode());
                session.Transaction.ReleaseState();
            }
        }
    }
}
