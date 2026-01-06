using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Rooms;
using ServerCore.Job;

namespace GameServer.Game.Contexts.Transaction
{
    public class EnterZoneContext : BaseContext, ILoadPlayerDbContext, IPlayerDbContext
    {
        private EnterZoneContext() { }
        public static EnterZoneContext Create()
        {
            return new EnterZoneContext();
        }

        private WriteOnce<World> _world = new();
        private WriteOnce<short> _playerIndex = new();
        private WriteOnce<long> _playerDbId = new();

        private WriteOnce<PlayerDb> _playerDb = new();

        private WriteOnce<Channel> _channel = new();
        private WriteOnce<Zone> _zone = new();

        public World World
        {
            get => _world.Value;
            set => _world.Value = value;
        }
        public Channel Channel
        {
            get => _channel.Value;
            set => _channel.Value = value; 
        }
        public short PlayerIndex
        {
            get => _playerIndex.Value;
            set => _playerIndex.Value = value;
        }
        public long PlayerDbId
        {
            get => _playerDbId.Value;
            set => _playerDbId.Value = value;
        }
        public PlayerDb PlayerDb
        {
            get => _playerDb.Value;
            set => _playerDb.Value = value;
        }
        public Zone Zone
        {
            get => _zone.Value;
            set => _zone.Value = value;
        }
    }
}