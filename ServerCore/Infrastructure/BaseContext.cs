using System;
using System.Collections.Concurrent;
using ServerCore.Job;

namespace ServerCore.Infrastructure
{
    public abstract class BaseContext<TSession, TContext> : IContext<TSession>, IDisposable
        where TSession : Session
        where TContext : BaseContext<TSession, TContext>, new()
    {
        private WriteOnce<long> _accountDbId;
        private WriteOnce<TSession> _session;

        public TSession Session
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
        public Action<TContext>? OnCompleted { get; set; }

        void IContext<TSession>.Complete()
        {
            var callback = OnCompleted;
            OnCompleted = null;
#if DEBUG
            if (callback == null) Environment.FailFast("Logic Error: OnCompleted is missing.");
#endif
            callback?.Invoke((TContext)this);
        }


        private static readonly ConcurrentBag<TContext> Pool = new();
        public static TContext Create()
        {
            if (!Pool.TryTake(out var ctx))
            {
                ctx = new TContext();
            }

            ctx.InternalInit();
            return ctx;
        }

        protected abstract void OnInit();
        protected abstract void OnDispose();

        private void InternalInit()
        {
            _accountDbId = new WriteOnce<long>();
            _session = new WriteOnce<TSession>();
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
            Pool.Add((TContext)this);
        }
    }
}
