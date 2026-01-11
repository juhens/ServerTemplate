using System;
using System.Collections.Concurrent;

namespace ServerCore
{
    public abstract class BaseSessionManager<TSession> where TSession : Session
    {
        private readonly ConcurrentDictionary<long /*sessionRuntimeId*/, TSession> _sessions = new();
        public int Count => _sessions.Count;

        protected abstract TSession Create();

        public TSession Generate()
        {
            var session = Create();
            Enter(session);
            return session;
        }

        private void Enter(TSession session)
        {
            if (!_sessions.TryAdd(session.RuntimeId, session))
                throw new Exception($"SessionManager Enter Failed: {session.RuntimeId}");
        }

        public void Leave(TSession session)
        {
            _sessions.TryRemove(session.RuntimeId, out _);
        }

        public void KickAllUsers()
        {
            foreach (var session in _sessions.Values)
            {
                session.Disconnect("Server Shutdown or KickAll");
            }
        }

        public TSession? Find(long runtimeId)
        {
            _sessions.TryGetValue(runtimeId, out var session);
            return session;
        }
    }
}