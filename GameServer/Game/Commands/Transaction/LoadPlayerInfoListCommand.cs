using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Contexts.Transaction;
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
                session.Disconnect("Not logged account");
            }

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("LoadPlayerInfoListCommand.Execute:Disconnected");
            }
            else
            {
                var ctx = LoadPlayerInfoListContext.Create();
                ctx.AccountDbId = accountDbId;
                ctx.WorldStaticId = packet.WorldStaticId;

                Log.Info(typeof(LoadPlayerInfoListCommand), "[Begin] Session:{0} AccountDbId:{1}", session.RuntimeId, ctx.AccountDbId);
                DbManager.Instance.LoadPlayerInfoList(session, ctx, OnLoadPlayerInfoList);
            }
        }

        private static void OnLoadPlayerInfoList(ClientSession session, LoadPlayerInfoListContext ctx)
        {
            var response = new S_PlayerInfoArray();
            if (ctx.Result != TransactionResult.Success)
            {
                response.ServerResult = ServerResult.UnknownError;
                session.Transaction.FailedWithLastMessage("OnLoadPlayerInfoList: Failed load playerInfoList", response.Encode());
                return;
            }
            
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

            // 마지막 체크
            if (session.Disconnected)
            {
                session.Transaction.Failed("OnLoadPlayerInfoList:Disconnected");
            }
            else
            {
                Log.Info(typeof(LoadPlayerInfoListCommand), "[ End ] Session:{0} AccountDbId{1}", session.RuntimeId, ctx.AccountDbId);
                session.Send(response.Encode());
                session.Transaction.ReleaseState();
            }
        }
    }
}
