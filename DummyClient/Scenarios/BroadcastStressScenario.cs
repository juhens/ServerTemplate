using DummyClient.Network;
using ServerCore.Packet;
using PacketGen;

namespace DummyClient.Scenarios
{
    public class BroadcastStressScenario : IScenario
    {
        // [Configuration]
        public const int RecommendedConnectCount = 1;
        public const int MaxSendCount = 100;

        private int _currentSendCount;
        private bool _isLoggedIn = false;

        public void OnConnected(ServerSession session)
        {
            var syn = new C_HandshakeSyn
            {
                ClientVersion = 1,
                DeviceId = $"DummyClient_{Guid.NewGuid()}",
                PlatformType = PlatformType.Windows
            };
            session.Send(syn.Encode());
        }

        public void OnRecvPacket(ServerSession session, ushort protocolId, ArraySegment<byte> buffer)
        {
            PacketManager.OnRecvPacket(session, protocolId, buffer);
        }

        public void OnDisconnected(ServerSession session, string? msg)
        {
            Console.WriteLine($"Disconnected: {session.RuntimeId}, Msg: {msg}");
            _isLoggedIn = false;
        }

        public void Update(ServerSession session)
        {
            if (!_isLoggedIn) return;
            if (_currentSendCount >= MaxSendCount) return;

            // 한 번의 Update 호출에 패킷 하나 전송 (Program.cs 루프 주기에 따름)
            var chatPacket = new C_Chat
            {
                Chat = $"Hello from {session.RuntimeId} - {_currentSendCount++}"
            };
            session.Send(chatPacket.Encode());
        }

        public void OnHandshakeCompleted(ServerSession session)
        {
            var packet = new C_Login();
            // 기존 PacketHandler에 있던 로직 이동
            packet.SessionToken = $"CORE_STRESS_TEST_{session.RuntimeId}";
            packet.VerifySeed = 123456789;
            session.Send(packet.Encode());
        }

        public void OnLoginResult(ServerSession session, S_LoginResponse result)
        {
            if (result.ServerResult == ServerResult.Success)
            {
                Interlocked.Increment(ref ServerSession.ConnectedCount);
                _isLoggedIn = true;
            }
        }
    }
}