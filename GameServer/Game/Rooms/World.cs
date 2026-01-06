using GameServer.Game.Dto;
using GameServer.Network;
using ServerCore;
using ServerCore.Job;

namespace GameServer.Game.Rooms
{
    public class World : RoomJobSerializer
    {
        public World(IJobScheduler executor, short staticId, string name) : base(executor)
        {
            StaticId = staticId;
            _name = name;
        }

        public readonly short StaticId;
        private readonly string _name;
        private readonly List<Channel> _channels = [];

        public int TotalChannelSessionCount => _channels.Sum(c => c.SessionCount);
        public int TotalZoneSessionCount => _channels.Sum(c => c.TotalZoneSessionCount);

        public void Initialize(short channelCount, object[] zoneData)
        {
            for (short i = 0; i < channelCount; i++)
            {
                var channel = new Channel(JobScheduler, index:i);
                channel.Initialize(zoneData);
                _channels.Add(channel);
            }
        }
        public Channel? FindChannel(short channelIdx)
        {
            if (channelIdx < 0 || channelIdx >= _channels.Count)
                return null;
            return _channels[channelIdx];
        }

        protected override void OnEnter(ClientSession session)
        {
            session.Routing.WorldRef.Attach(this);
            Log.Debug(this, "Entered World:{0} Session:{1}", StaticId, session.RuntimeId);
        }
        protected override void OnLeave(ClientSession session)
        {
            session.Routing.WorldRef.Detach();
            Log.Debug(this, "Left World:{0} Session:{1}", StaticId, session.RuntimeId);
        }

        public WorldInfoDto GetWorldInfo()
        {
            var worldInfo = new WorldInfoDto
            {
                WorldStaticId = StaticId,
                WorldName = _name,
                ChannelInfoList = []
            };

            foreach (var channel in _channels)
            {
                var channelInfo = channel.GetChannelInfo();
                worldInfo.ChannelInfoList.Add(channelInfo);
            }

            return worldInfo;
        }
    }
}
