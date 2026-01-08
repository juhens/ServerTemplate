using GameServer.Network;

namespace GameServer.Game.Commands.Transaction.Contexts.Interfaces
{
    public interface IContext
    {
        public ClientSession Session { get; set; }
        public long AccountDbId { get; set; }
        public TransactionResult Result { get; set; }
        public void Complete();
    }
}
