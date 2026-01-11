namespace ServerCore.Infrastructure
{
    public interface IContext<TSession> where TSession : Session
    {
        public TSession Session { get; set; }
        public long AccountDbId { get; set; }
        public TransactionResult Result { get; set; }
        public void Complete();
    }
}
