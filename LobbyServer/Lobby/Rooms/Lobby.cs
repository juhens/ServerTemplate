using LobbyServer.Network;
using ServerCore;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace LobbyServer.Lobby.Rooms
{
    public class Lobby : BaseRoom<ClientSession>
    {
        public Lobby(IJobScheduler jobScheduler) : base(jobScheduler)
        {
        }

        protected override bool TryGetRuntimeId(ClientSession session, out long runtimeId)
        {
            runtimeId = 0;
            // 단순 라우팅 확인용
            if (!session.Routing.AccountDbIdRef.TryCapture(out _)) return false;
            runtimeId = session.RuntimeId;
            return true;
        }

        protected override TransactionResult OnEnter(ClientSession session)
        {
            if (!session.Routing.LobbyRef.TryAttach(this)) return TransactionResult.FailedAttach;
            Log.Debug(this, "Entered Session:{0}",session.RuntimeId);
            return TransactionResult.Success;
        }

        protected override void OnLeave(ClientSession session)
        {
            if (session.Routing.LobbyRef.TryDetach()) return;
            Log.Debug(this, "Left Session:{0}", session.RuntimeId);
        }
    }
}
