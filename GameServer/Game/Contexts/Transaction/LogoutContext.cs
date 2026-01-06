using GameServer.Database;
using GameServer.Game.Contexts.Interfaces;
using ServerCore.Job;

namespace GameServer.Game.Contexts.Transaction
{
    public class LogoutContext : BaseContext, IPlayerDbContext
    {
        public static LogoutContext Create() => new();
        private LogoutContext() { }

        private WriteOnce<PlayerDb> _playerDb = new();

        public PlayerDb PlayerDb
        {
            get => _playerDb.Value;
            set => _playerDb.Value = value;
        }
    }
}