using GameServer.Database;

namespace GameServer.Game.Commands.Transaction.Contexts.Interfaces
{
    public interface IPlayerDbContext : IContext
    {
        public PlayerDb PlayerDb { get; set; }
    }
}
