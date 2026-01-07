using GameServer.Game.Dto;
using GameServer.Network;
using ServerCore;
using ServerCore.Job;

namespace GameServer.Game.Rooms
{
    public class Channel : RoomJobSerializer
    {
        public Channel(IJobScheduler jobScheduler, short index) : base(jobScheduler)
        {
            Index = index;
        }

        public readonly short Index;
        private readonly Dictionary<int /*staticId*/, Zone> _zones = [];

        public int TotalZoneSessionCount => _zones.Values.Sum(z => z.SessionCount);

        public void Initialize(object[] zoneData)
        {
            for (var i = 0; i < zoneData.Length; i++)
            {
                var zone = new Zone(JobScheduler, i);
                zone.Initialize(zoneData[i]);
                _zones.TryAdd(i, zone);
            }
        }
        public Zone? FindZone(int zoneStaticId)
        {
            return _zones.GetValueOrDefault(zoneStaticId);
        }

        protected override bool OnEnter(ClientSession session)
        {
            if (!session.Routing.ChannelRef.TryAttach(this)) return false;
            Log.Debug(this, "Entered Channel:{0} Session:{1}", Index, session.RuntimeId);
            return true;
        }
        protected override bool OnLeave(ClientSession session)
        {
            if (!session.Routing.ChannelRef.TryDetach()) return false;
            Log.Debug(this, "Left Channel:{0} Session:{1}", Index, session.RuntimeId);
            return true;
        }

        public ChannelInfoDto GetChannelInfo()
        {
            var channelInfo = new ChannelInfoDto
            {
                ChannelIndex = Index
            };
            return channelInfo;
        }
    }
}