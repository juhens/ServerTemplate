using DummyClient.Network;
using PacketGen;
using ServerCore.Packet;

namespace DummyClient.Scenarios
{
    public class LoginSpamScenario : IScenario
    {
        public const int RecommendedConnectCount = 10;

        public void OnConnected(ServerSession session)
        {
            var syn = new C_HandshakeSyn
            {
                ClientVersion = 1,
                DeviceId = $"SpamTester_{Guid.NewGuid()}",
                PlatformType = PlatformType.Windows
            };
            session.Send(syn.Encode());
        }

        public void OnHandshakeCompleted(ServerSession session)
        {
            for (var i = 0; i < 100; i++)
            {
                var packet = new C_Login();
                // 동일한 토큰 사용
                packet.SessionToken = $"CORE_STRESS_TEST_{session.RuntimeId}";
                packet.VerifySeed = 123456789;
                
                session.Send(packet.Encode());
            }
            Console.WriteLine($"[Client {session.RuntimeId}] Spammed 10 Login Packets.");
        }

        public void OnLoginResult(ServerSession session, S_LoginResponse result)
        {
            if (result.ServerResult == ServerResult.Success)
            {
                Console.WriteLine($"[Client {session.RuntimeId}] Login Success");
            }
            else
            {
                Console.WriteLine($"[Client {session.RuntimeId}] Login Failed/Duplicate: {result.ServerResult}");
            }
        }

        public void OnRecvPacket(ServerSession session, ushort protocolId, ArraySegment<byte> buffer)
        {
            PacketManager.OnRecvPacket(session, protocolId, buffer);
        }

        public void OnDisconnected(ServerSession session, string? msg)
        {
            // Console.WriteLine($"[Client {session.RuntimeId}] Disconnected: {msg}");
        }

        public void Update(ServerSession session)
        {
            // Do nothing
        }
    }
}