using GameServer.Database;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using GameServer.Game.Rooms;
using GameServer.Network;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace GameServer.Game.Commands.Transaction.Contexts.Transaction
{
    public class EnterZoneContext : BaseContext<ClientSession, EnterZoneContext>, ILoadPlayerDbContext, IPlayerDbContext
    {
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

        protected override void OnInit()
        {
            _world = new WriteOnce<World>();
            _playerIndex = new WriteOnce<short>();
            _playerDbId = new WriteOnce<long>();
            _playerDb = new WriteOnce<PlayerDb>();
            _channel = new WriteOnce<Channel>();
            _zone = new WriteOnce<Zone>();
        }

        protected override void OnDispose()
        {
            _world = default!;
            _playerIndex = default!;
            _playerDbId = default!;
            _playerDb = default!;
            _channel = default!;
            _zone = default!;
        }
    }
}