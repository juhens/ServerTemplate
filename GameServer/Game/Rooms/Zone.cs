using GameServer.Network;
using ServerCore;
using ServerCore.Job;

namespace GameServer.Game.Rooms
{
    public class Zone : RoomJobSerializer
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

        protected override bool OnEnter(ClientSession session)
        {
            if (!session.Routing.ZoneRef.TryAttach(this)) return false;
            Log.Debug(this, "Entered Zone:{0} Session:{1}", StaticId, session.RuntimeId);

            // TODO: Zone 입장 패킷 브로드캐스트 필요 시 여기서 호출
            // Broadcast(session, enterPacket);
            return true;
        }
        protected override bool OnLeave(ClientSession session)
        {
            if (!session.Routing.ZoneRef.TryDetach()) return false;
            Log.Debug(this, "Left Zone:{0} Session:{1}", StaticId, session.RuntimeId);
            return true;
        }
    }
}