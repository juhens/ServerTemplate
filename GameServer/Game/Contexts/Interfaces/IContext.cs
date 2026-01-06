namespace GameServer.Game.Contexts.Interfaces
{
    public interface IContext
    {
        public long AccountDbId { get; set; }
        public TransactionResult Result { get; set; }
    }
}
