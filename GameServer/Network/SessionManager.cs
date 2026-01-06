using System.Collections.Concurrent;
using GameServer.Common;

namespace GameServer.Network
{
    public class SessionManager
    {
        public static readonly SessionManager Instance = new SessionManager();

        private readonly ConcurrentDictionary<long /*runtimeId*/, ClientSession> _sessions = new();
        private void Enter(ClientSession session)
        {
            if (!_sessions.TryAdd(session.RuntimeId, session)) throw new Exception($"SessionManager Enter Failed: Duplicate Session RuntimeId {session.RuntimeId} detected.");
        }
        public void Leave(ClientSession session)
        {
            if (!_sessions.TryRemove(session.RuntimeId, out _)) throw new Exception($"SessionManager Leave Failed: Session {session.RuntimeId} not found or already removed.");
        }
        public int Count => _sessions.Count;
        public ClientSession Generate()
        {
            var session = new ClientSession
            {
                RuntimeId = SessionRuntimeIdGen.Generate()
            };
            Enter(session);
            return session;
        }

        public void KickAllUsers()
        {
            foreach (var session in _sessions.Values)
            {
                session.Disconnect("Kick");
            }
        }
    }
}
