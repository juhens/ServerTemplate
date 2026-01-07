using GameServer.Game.Contexts.Interfaces;
using GameServer.Network;
using ServerCore;
using ServerCore.Job;

namespace GameServer.Game.Rooms
{
    public abstract class RoomJobSerializer : JobSerializer
    {
        protected RoomJobSerializer(IJobScheduler jobScheduler) : base(jobScheduler)
        {
            JobScheduler = jobScheduler;
        }

        protected readonly IJobScheduler JobScheduler;
        private readonly Dictionary<long /*playerRuntimeId*/, ClientSession> _sessions = [];
        private readonly Dictionary<string, long /*playerRuntimeId*/> _playerNameToRuntimeId = [];
        private readonly List<(Session srcSession, ArraySegment<byte> segment)> _broadcastList = [];

        private volatile int _sessionCount;
        public int SessionCount => _sessionCount;

        public void Enter<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext
        {
            Push(EnterJob, this, session, ctx, callback, JobPriority.Critical);
        }
        private static void EnterJob<T>(RoomJobSerializer @this, ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext
        {
            if (!session.Routing.PlayerRef.TryCapture(out var player))
            {
                ctx.Result = TransactionResult.NotRoutedPlayer;
                callback(session, ctx);
                return;
            }

            if (!@this._sessions.TryAdd(player.RuntimeId, session))
            {
                ctx.Result = TransactionResult.DuplicateNickname;
                callback(session, ctx);
                return;
            }

            if (!@this._playerNameToRuntimeId.TryAdd(player.Nickname, player.RuntimeId))
            {
                @this._sessions.Remove(player.RuntimeId, out _);
                ctx.Result = TransactionResult.DuplicateNickname;
                callback(session, ctx);
                return;
            }

            if (!@this.OnEnter(session))
            {
                @this._sessions.Remove(player.RuntimeId, out _);
                @this._playerNameToRuntimeId.Remove(player.Nickname, out _);
                ctx.Result = TransactionResult.FailedOnEnter;
                return;
            }

            Interlocked.Increment(ref @this._sessionCount);
            callback(session, ctx);
        }

        public void Leave<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext
        {
            Push(LeaveJob, this, session, ctx, callback, JobPriority.Critical);
        }
        private static void LeaveJob<T>(RoomJobSerializer @this, ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext
        {
            if (session.Routing.PlayerRef.TryCapture(out var player))
            {
                if (@this._sessions.Remove(player.RuntimeId, out _))
                {
                    Interlocked.Decrement(ref @this._sessionCount);
                    @this._playerNameToRuntimeId.Remove(player.Nickname, out _);
                }
            }
            else
            {
                Log.Error(@this, "Leave: Missing PlayerDb. Session:{0}", session.RuntimeId);
            }

            if (!@this.OnLeave(session))
                Log.Error(@this, "Leave: OnLeaveFailed. Session:{0}", session.RuntimeId);
            callback(session, ctx);
        }

        public void Broadcast(ClientSession session, ArraySegment<byte> segment, JobPriority jobPriority)
        {
            Push(BroadcastJob, this, session, segment, jobPriority);
        }
        private static void BroadcastJob(RoomJobSerializer @this, ClientSession session, ArraySegment<byte> segment)
        {
            @this._broadcastList.Add((session, segment));
        }

        protected ClientSession? FindSession(long playerRuntimeId)
        {
            CheckThreadAffinity();
            return _sessions.GetValueOrDefault(playerRuntimeId);
        }
        protected ClientSession? FindSession(string nickName)
        {
            CheckThreadAffinity();
            var runtimeId = _playerNameToRuntimeId.GetValueOrDefault(nickName, -1);
            if (runtimeId == -1) return null;
            return _sessions.GetValueOrDefault(runtimeId);
        }
        private void CheckThreadAffinity()
        {
            if (Current != this)
            {
                var currentInfo = Current?.GetType().Name ?? "External Thread";
                Environment.FailFast($"[Thread Violation] {GetType().Name} access denied from {currentInfo}.");
            }
        }

        protected override void OnPostFlush()
        {
            if (_broadcastList.Count == 0) return;

            foreach (var (playerRuntimeId, session) in _sessions)
            {
                session.SendBatch(_broadcastList);
                session.SendFlush();
            }
            _broadcastList.Clear();
        }
        protected abstract bool OnEnter(ClientSession session);
        protected abstract bool OnLeave(ClientSession session);
    }
}
