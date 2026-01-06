using DummyClient.Network;
using PacketGen;
using ServerCore.Packet;

namespace DummyClient.Scenarios
{
    public class AreaTransitionScenario : IScenario
    {
        // [Configuration]
        public const int RecommendedConnectCount = 5000;

        private readonly Random _rand = new Random();

        public void OnConnected(ServerSession session)
        {
            var syn = new C_HandshakeSyn
            {
                ClientVersion = 1,
                DeviceId = $"ZombieTester_{Guid.NewGuid()}",
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

            // [Race Condition Trigger]
            // 로그인 패킷 전송 후, 서버가 처리하는 도중(DB -> Logic) 혹은
            // Area.Enter 작업을 큐에 넣은 직후에 연결을 끊어버림.
            // 10ms ~ 300ms 사이의 랜덤 지연 후 Disconnect
            Task.Delay(_rand.Next(10, 300)).ContinueWith(_ => 
            {
                if (!session.Disconnected)
                {
                    session.Disconnect("Zombie Test Force Disconnect");
                }
            });
        }

        public void OnLoginResult(ServerSession session, S_LoginResponse result)
        {
            // 로그인 성공 응답을 받았다는 것은, 이미 서버 메모리(Area)에 들어갔을 확률이 높음.
            // 이 시점에서도 끊어봄.
            if (result.ServerResult == ServerResult.Success)
            {
                Task.Delay(_rand.Next(10, 100)).ContinueWith(_ =>
                {
                    if (!session.Disconnected)
                    {
                        session.Disconnect("Zombie Test After Login");
                    }
                });
            }
        }

        public void OnRecvPacket(ServerSession session, ushort protocolId, ArraySegment<byte> buffer)
        {
            PacketManager.OnRecvPacket(session, protocolId, buffer);
        }

        public void OnDisconnected(ServerSession session, string? msg)
        {
            // 연결 해제됨
        }

        public void Update(ServerSession session)
        {
            // 주기적 작업 없음
        }
    }
}