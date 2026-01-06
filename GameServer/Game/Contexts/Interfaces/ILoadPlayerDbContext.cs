using GameServer.Game.Rooms;

namespace GameServer.Game.Contexts.Interfaces
{
    public interface ILoadPlayerDbContext
    {
        public World World { get; set; }
        public short PlayerIndex { get; set; }
        public long PlayerDbId { get; set; }
    }
}
