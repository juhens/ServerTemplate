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

        protected override void OnEnter(ClientSession session)
        {
            session.Routing.ZoneRef.Attach(this);
            Log.Debug(this, "Entered Zone:{0} Session:{1}", StaticId, session.RuntimeId);

            // TODO: Zone 입장 패킷 브로드캐스트 필요 시 여기서 호출
            // Broadcast(session, enterPacket);
        }
        protected override void OnLeave(ClientSession session)
        {
            session.Routing.ZoneRef.Detach();
            Log.Debug(this, "Left Zone:{0} Session:{1}", StaticId, session.RuntimeId);
        }
    }
}