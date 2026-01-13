using GameServer.Common;

namespace GameServer.Logic.Objects
{
    public enum GameObjType
    {
        None = 0,
        Player = 1,
        Monster = 2,
        Npc = 3,
        Item = 4,
    }

    public abstract class GameObj : IRuntimeId
    {
        public long RuntimeId { get; init; }
        public abstract GameObjType GameObjType { get; }
    }
}
