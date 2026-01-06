using GameServer.Common;
using GameServer.Database;

namespace GameServer.Game.Objects
{
    public class Player : GameObj
    {
        public Player(PlayerDb playerDb)
        {
            AccountDbId = playerDb.AccountDbId;
            PlayerDbId = playerDb.PlayerDbId;
            RuntimeId = GameObjRuntimeIdGen.Generate();
        }
        public override GameObjType GameObjType => GameObjType.Player;

        #region ¿Œ«¡∂Û
        public long AccountDbId { get; }
        public long PlayerDbId { get; }
        // --------------------------------
        #endregion

        public string Nickname = string.Empty;

        public PlayerDb ToPlayerDb()
        {
            return new PlayerDb
            {
                AccountDbId = AccountDbId,
                PlayerDbId = PlayerDbId,
                Nickname = Nickname,
            };
        }
    }
}