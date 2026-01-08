using System.Collections.Concurrent;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using GameServer.Network;
using ServerCore.Job;

namespace GameServer.Game.Commands.Transaction.Contexts
{
    public abstract class BaseContext<T> : IContext, IDisposable where T : BaseContext<T>, new()
    {
        private WriteOnce<long> _accountDbId;
        private WriteOnce<ClientSession> _session;

        public ClientSession Session
        {
            get => _session.Value;
            set => _session.Value = value;
        }
        public long AccountDbId
        {
            get => _accountDbId.Value;
            set => _accountDbId.Value = value;
        }

        public TransactionResult Result { get; set; }
        public Action<T>? OnCompleted { get; set; }

        void IContext.Complete()
        {
            var callback = OnCompleted;
            OnCompleted = null;
#if DEBUG
            if (callback == null) Environment.FailFast("Logic Error: OnCompleted is missing.");
#endif
            callback?.Invoke((T)this);
        }


        private static readonly ConcurrentBag<T> Pool = [];
        public static T Create()
        {
            if (!Pool.TryTake(out var ctx))
            {
                ctx = new T();
            }

            ctx.InternalInit();
            return ctx;
        }

        protected abstract void OnInit();
        protected abstract void OnDispose();

        private void InternalInit()
        {
            _accountDbId = new WriteOnce<long>();
            _session = new WriteOnce<ClientSession>();
            Result = TransactionResult.Success;
            OnCompleted = null;
            OnInit();
        }

        private void InternalReset()
        {
            _accountDbId = default!;
            _session = default!;
            Result = default!;
            OnCompleted = null;
            OnDispose();
        }

        public void Dispose()
        {
            InternalReset();
            Pool.Add((T)this);
        }
    }
}
