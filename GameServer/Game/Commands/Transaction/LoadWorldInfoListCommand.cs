using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Contexts.Transaction;
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
                session.Disconnect("Not logged account");
            }

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("LoadWorldInfoListCommand.Execute:Disconnected");
            }
            else
            {
                Log.Info(typeof(LoadWorldInfoListCommand), "[Begin] Session:{0} AccountDbId{1}", session.RuntimeId, accountDbId);
                var ctx = LoadWorldInfoListContext.Create();
                ctx.AccountDbId = accountDbId;

                DbManager.Instance.LoadWorldInfoList(session, ctx, OnLoadWorldInfoList);
            }
        }

        private static void OnLoadWorldInfoList(ClientSession session, LoadWorldInfoListContext ctx)
        {
            var response = new S_WorldInfoArray();
            if (ctx.Result != TransactionResult.Success)
            {
                response.ServerResult = ServerResult.UnknownError;
                session.Transaction.FailedWithLastMessage("OnLoadWorldInfoList: Failed load worldInfoList", response.Encode());
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
