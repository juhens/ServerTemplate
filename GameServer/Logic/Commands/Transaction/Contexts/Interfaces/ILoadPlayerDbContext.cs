using GameServer.Logic.Rooms;
using GameServer.Network;
using ServerCore.Infrastructure;

namespace GameServer.Logic.Commands.Transaction.Contexts.Interfaces
{
    public interface ILoadPlayerDbContext : IContext<ClientSession>
    {
        public World World { get; set; }
        public short PlayerIndex { get; set; }
        public long PlayerDbId { get; set; }
    }
}
