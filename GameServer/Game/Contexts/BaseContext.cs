using GameServer.Game.Contexts.Interfaces;
using ServerCore.Job;

namespace GameServer.Game.Contexts
{
    public abstract class BaseContext : IContext
    {
        private WriteOnce<long> _accountDbId = new();
        private WriteOnce<TransactionResult> _result = new();

        public long AccountDbId
        {
            get => _accountDbId.Value;
            set => _accountDbId.Value = value;
        }

        public TransactionResult Result
        {
            get => _result.Value;
            set => _result.Value = value;
        }
    }
}
