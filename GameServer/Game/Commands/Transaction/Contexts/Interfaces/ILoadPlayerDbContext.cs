using GameServer.Game.Rooms;

namespace GameServer.Game.Commands.Transaction.Contexts.Interfaces
{
    public interface ILoadPlayerDbContext : IContext
    {
        public World World { get; set; }
        public short PlayerIndex { get; set; }
        public long PlayerDbId { get; set; }
    }
}
