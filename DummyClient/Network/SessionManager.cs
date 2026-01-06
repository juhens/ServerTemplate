using PacketGen;
using ServerCore.Packet;

namespace DummyClient.Network
{
    public class SessionManager
    {
        private readonly List<ServerSession> _sessions = [];
        private readonly object _lock = new();
        public static SessionManager Instance { get; } = new();

        public IReadOnlyList<ServerSession> Sessions => _sessions;

        public ServerSession Generate()
        {
            lock (_lock)
            {
                var session = new ServerSession();
                _sessions.Add(session);
                return session;
            }
        }

        public void SendForeach()
        {
            lock (_lock)
            {
                // TODO 도중에 세션 갯수 바뀌면 터짐
                if (_sessions.Count == 0) return;
                foreach (var session in _sessions)
                {
                    var chatPacket = new C_Chat
                    {
                        Chat = $"Hello Server"
                    };
                    session.Send(chatPacket.Encode());
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                foreach (var session in _sessions)
                {
                    session.Disconnect("Clear");
                }
                _sessions.Clear();
            }
        }
    }
}
