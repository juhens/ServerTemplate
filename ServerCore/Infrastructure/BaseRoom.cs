using System;
using System.Collections.Generic;
using System.Threading;
using ServerCore.Job;

namespace ServerCore.Infrastructure
{
    public abstract class BaseRoom<TSession> : JobSerializer where TSession : Session
    {
        protected BaseRoom(IJobScheduler jobScheduler) : base(jobScheduler)
        {
            JobScheduler = jobScheduler;
        }

        protected readonly IJobScheduler JobScheduler;

        private readonly Dictionary<long /*runtimeId*/, TSession> _sessions = new();
        private readonly Dictionary<long /*accountDbId*/, long /*runtimeId*/> _dbIdToRuntimeId = new();
        private readonly List<(Session srcSession, ArraySegment<byte> segment)> _broadcastList = new();

        private volatile int _sessionCount;
        public int SessionCount => _sessionCount;

        public void Enter<T>(T ctx) where T : IContext<TSession>
        {
            Push(EnterJob, this, ctx, JobPriority.Critical);
        }
        private static void EnterJob<T>(BaseRoom<TSession> @this, T ctx) where T : IContext<TSession>
        {
            var session = ctx.Session;
            try
            {
                if (session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }

                if (!@this.TryGetRuntimeId(session, out var runtimeId))
                {
                    ctx.Result = TransactionResult.NotRouted;
                    return;
                }

                if (!@this._dbIdToRuntimeId.TryAdd(ctx.AccountDbId, runtimeId))
                {
                    ctx.Result = TransactionResult.DuplicateAuth;
                    return;
                }

                if (!@this._sessions.TryAdd(runtimeId, session))
                {
                    @this._dbIdToRuntimeId.Remove(ctx.AccountDbId, out _);
                    ctx.Result = TransactionResult.DuplicateRuntimeId;
                    return;
                }


                var result = @this.OnEnter(session);
                if (result != TransactionResult.Success)
                {
                    @this._dbIdToRuntimeId.Remove(ctx.AccountDbId, out _);
                    @this._sessions.Remove(runtimeId, out _);
                    ctx.Result = result;
                    return;
                }

                Interlocked.Increment(ref @this._sessionCount);

                ctx.Result = TransactionResult.Success;
            }
            finally
            {
                ctx.Complete();
            }
        }

        public void Leave<T>(T ctx) where T : IContext<TSession>
        {
            Push(LeaveJob, this, ctx, JobPriority.Critical);
        }
        private static void LeaveJob<T>(BaseRoom<TSession> @this, T ctx) where T : IContext<TSession>
        {
            var session = ctx.Session;

            if (@this.TryGetRuntimeId(session, out var runtimeId))
            {
                if (@this._sessions.Remove(runtimeId, out _))
                {
                    @this._dbIdToRuntimeId.Remove(ctx.AccountDbId, out _);
                    @this.OnLeave(session);
                    Interlocked.Decrement(ref @this._sessionCount);
                }
            }
            else
            {
                Log.Error(@this.GetType().Name, "Leave: Failed TryGetRuntimeId. Session:{0}", session.RuntimeId);
            }
            ctx.Complete();
        }

        public void Broadcast(TSession session, ArraySegment<byte> segment, JobPriority jobPriority)
        {
            Push(BroadcastJob, this, session, segment, jobPriority);
        }
        private static void BroadcastJob(BaseRoom<TSession> @this, TSession session, ArraySegment<byte> segment)
        {
            @this._broadcastList.Add((session, segment));
        }

        protected TSession? FindSession(long runtimeId)
        {
            CheckThreadAffinity();
            return _sessions.GetValueOrDefault(runtimeId);
        }
        protected TSession? FindSessionByAccountDbId(long accountDbId)
        {
            CheckThreadAffinity();
            var runtimeId = _dbIdToRuntimeId.GetValueOrDefault(accountDbId, -1);
            if (runtimeId == -1) return null;
            return _sessions.GetValueOrDefault(runtimeId);
        }

        private void CheckThreadAffinity()
        {
            if (Current != this)
            {
                var currentInfo = Current?.GetType().Name ?? "External Thread";
#if DEBUG
                Environment.FailFast($"[Thread Violation] {GetType().Name} access denied from {currentInfo}.");
#endif
                Log.Error(this, "[Thread Violation] {0} access denied from {1}.", GetType().Name, currentInfo);
            }
        }

        protected override void OnPostFlush()
        {
            if (_broadcastList.Count == 0) return;

            foreach (var kvp in _sessions)
            {
                var session = kvp.Value;
                session.SendBatch(_broadcastList);
                session.SendFlush();
            }
            _broadcastList.Clear();
        }

        protected abstract bool TryGetRuntimeId(TSession session, out long runtimeId);
        protected abstract TransactionResult OnEnter(TSession session);
        protected abstract void OnLeave(TSession session);
    }
}