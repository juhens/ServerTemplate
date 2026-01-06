using GameServer.Database;

namespace GameServer.Game.Contexts.Interfaces
{
    public interface IPlayerDbContext
    {
        public PlayerDb PlayerDb { get; set; }
    }
}
