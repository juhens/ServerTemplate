using GameServer.Database;
using GameServer.Network;
using ServerCore.Infrastructure;

namespace GameServer.Game.Commands.Transaction.Contexts.Interfaces
{
    public interface IPlayerDbContext : IContext<ClientSession>
    {
        public PlayerDb PlayerDb { get; set; }
    }
}
