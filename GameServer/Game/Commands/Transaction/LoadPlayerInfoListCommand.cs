using GameServer.Database;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using GameServer.Game.Commands.Transaction.Contexts.Transaction;
using GameServer.Network;
using PacketGen;
using ServerCore;
using ServerCore.Packet;

namespace GameServer.Game.Commands.Transaction
{
    public static class LoadPlayerInfoListCommand
    {
        public static void Execute(ClientSession session, C_RequestPlayerInfoArray packet)
        {
            // 트랜잭션 충돌 방어
            if (!session.Transaction.TrySetState(TransactionState.Busy)) return;

            // 로그인 상태 체크
            if (!session.Routing.AccountDbIdRef.TryCapture(out var accountDbId))
            {
                session.Transaction.Failed("Not logged account");
                return;
            }

            Log.Info(typeof(LoadPlayerInfoListCommand), "[Begin] Session:{0} AccountDbId:{1}", session.RuntimeId, accountDbId);

            var ctx = session.Transaction.CreateContext<LoadPlayerInfoListContext>();
            ctx.Session = session;
            ctx.AccountDbId = accountDbId;
            ctx.WorldStaticId = packet.WorldStaticId;
            ctx.OnCompleted = OnLoadPlayerInfoList;
            
            DbManager.Instance.LoadPlayerInfoList(ctx);
        }

        private static void OnLoadPlayerInfoList(LoadPlayerInfoListContext ctx)
        {
            var session = ctx.Session;

            switch (ctx.Result)
            {
                case TransactionResult.FailedLoadPlayerInfoList:
                case TransactionResult.Disconnected:
                    session.Transaction.Failed($"OnLoadPlayerInfoList:{ctx.Result}");
                    return;
            }

            var response = new S_PlayerInfoArray();
            response.ServerResult = ServerResult.Success;

            var playerInfoList = new List<PlayerInfo>();
            foreach (var playerDb in ctx.PlayerDbList)
            {
                var playerInfo = new PlayerInfo();
                playerInfo.Index = playerDb.Index;
                playerInfo.Nickname = playerDb.Nickname;
                playerInfoList.Add(playerInfo);
            }
            response.PlayerInfoArray = playerInfoList.ToArray();

            Log.Info(typeof(LoadPlayerInfoListCommand), "[ End ] Session:{0} AccountDbId{1}", session.RuntimeId, ctx.AccountDbId);
            session.Send(response.Encode());
            session.Transaction.ReleaseState();
        }
    }
}
