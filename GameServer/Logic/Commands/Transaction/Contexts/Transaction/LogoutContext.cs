using GameServer.Database;
using GameServer.Logic.Commands.Transaction.Contexts.Interfaces;
using GameServer.Network;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace GameServer.Logic.Commands.Transaction.Contexts.Transaction
{
    public class LogoutContext : BaseContext<ClientSession, LogoutContext>, IPlayerDbContext
    {
        private WriteOnce<PlayerDb> _playerDb;

        public PlayerDb PlayerDb
        {
            get => _playerDb.Value;
            set => _playerDb.Value = value;
        }

        protected override void OnInit()
        {
            _playerDb = new WriteOnce<PlayerDb>();
        }

        protected override void OnDispose()
        {
            _playerDb = default!;
        }
    }
}