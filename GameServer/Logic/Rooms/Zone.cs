using GameServer.Network;
using ServerCore;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace GameServer.Logic.Rooms
{
    public class Zone : GameRoom
    {
        public Zone(IJobScheduler jobScheduler, int staticId) : base(jobScheduler)
        {
            StaticId = staticId;
        }

        public readonly int StaticId;

        public void Initialize(object v)
        {
            //TODO: Zone Init
        }

        protected override TransactionResult OnEnterGame(ClientSession session)
        {
            if (!session.Routing.ZoneRef.TryAttach(this)) return TransactionResult.FailedAttach;
            Log.Debug(this, "Entered Zone:{0} Session:{1}", StaticId, session.RuntimeId);

            // TODO: Zone 입장 패킷 브로드캐스트 필요 시 여기서 호출
            // Broadcast(session, enterPacket);
            return TransactionResult.Success;
        }
        protected override void OnLeaveGame(ClientSession session)
        {
            if (session.Routing.ZoneRef.TryDetach()) return;
            Log.Debug(this, "Left Zone:{0} Session:{1}", StaticId, session.RuntimeId);
        }
    }
}