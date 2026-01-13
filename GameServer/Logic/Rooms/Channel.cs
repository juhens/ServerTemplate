using GameServer.Logic.Dto;
using GameServer.Network;
using ServerCore;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace GameServer.Logic.Rooms
{
    public class Channel : GameRoom
    {
        public Channel(IJobScheduler jobScheduler, short index) : base(jobScheduler)
        {
            Index = index;
        }

        public readonly short Index;
        private readonly Dictionary<int /*staticId*/, Zone> _zones = [];
        private readonly Dictionary<long /*runtimeId*/, Zone> _instanceZones = [];

        public int TotalZoneSessionCount => _zones.Values.Sum(z => z.SessionCount) + _instanceZones.Values.Sum(z => z.SessionCount);

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

        protected override TransactionResult OnEnterGame(ClientSession session)
        {
            if (!session.Routing.ChannelRef.TryAttach(this)) return TransactionResult.FailedAttach;
            Log.Debug(this, "Entered Channel:{0} Session:{1}", Index, session.RuntimeId);
            return TransactionResult.Success;
        }
        protected override void OnLeaveGame(ClientSession session)
        {
            if (session.Routing.ChannelRef.TryDetach()) return;
            Log.Debug(this, "Left Channel:{0} Session:{1}", Index, session.RuntimeId);
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