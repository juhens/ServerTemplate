using GameServer.Database;
using GameServer.Game.Commands.Transaction.Contexts;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using ServerCore.Job;

namespace GameServer.Game.Commands.Transaction.Contexts.Transaction
{
    public class LogoutContext : BaseContext<LogoutContext>, IPlayerDbContext
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