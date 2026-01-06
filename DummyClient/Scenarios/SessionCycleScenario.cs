using DummyClient.Network;
using PacketGen;
using ServerCore.Packet;

namespace DummyClient.Scenarios
{
    public class SessionCycleScenario : IScenario
    {
        public const int RecommendedConnectCount = 1;

        // 세션 종료 시점을 외부에 알리기 위한 콜백
        public Action? OnSessionDisconnected { get; set; }

        private readonly Random _rand = new Random();

        public void OnConnected(ServerSession session)
        {
            var syn = new C_HandshakeSyn
            {
                ClientVersion = 1,
                DeviceId = $"SimpleTester_{Guid.NewGuid()}",
                PlatformType = PlatformType.Windows
            };
            session.Send(syn.Encode());
        }

        public void OnHandshakeCompleted(ServerSession session)
        {
            var packet = new C_Login();
            packet.SessionToken = $"CORE_STRESS_TEST_{session.RuntimeId}";
            packet.VerifySeed = 123456789;
            session.Send(packet.Encode());

            // [Auto Disconnect] 로그인 패킷 전송 후 랜덤 지연 후 강제 종료
            Task.Delay(_rand.Next(0, 100)).ContinueWith( _ =>
            {
                if (!session.Disconnected)
                {
                    session.Disconnect("Session Cycle Test Random Force Disconnect");
                }
            });
        }

        public void OnLoginResult(ServerSession session, S_LoginResponse result)
        {
            if (result.ServerResult == ServerResult.Success)
            {
                Console.WriteLine($"[Client {session.RuntimeId}] Login Success");
            }
            else
            {
                Console.WriteLine($"[Client {session.RuntimeId}] Login Failed: {result.ServerResult}");
            }
        }

        public void OnRecvPacket(ServerSession session, ushort protocolId, ArraySegment<byte> buffer)
        {
            PacketManager.OnRecvPacket(session, protocolId, buffer);
        }

        public void OnDisconnected(ServerSession session, string? msg)
        {
            Task.Delay(500).ContinueWith(_ =>
            {
                OnSessionDisconnected?.Invoke();
            });
        }

        public void Update(ServerSession session)
        {

        }
    }
}