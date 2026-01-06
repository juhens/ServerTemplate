using DummyClient.Network;
using PacketGen;
using ServerCore.Packet;

namespace DummyClient.Scenarios
{
    public class LoginConflictScenario : IScenario
    {
        public const int RecommendedConnectCount = 2000;

        private long _targetTokenId;

        public void OnConnected(ServerSession session)
        {
            var syn = new C_HandshakeSyn
            {
                ClientVersion = 1,
                DeviceId = $"ConflictTest_{Guid.NewGuid()}",
                PlatformType = PlatformType.Windows
            };
            session.Send(syn.Encode());
        }

        public void OnHandshakeCompleted(ServerSession session)
        {
            // 역할 분담: 홀수는 Victim, 짝수는 Attacker
            // 1, 2 | 3, 4 | ...
            
            if (session.RuntimeId % 2 != 0) // 홀수 (Victim)
            {
                _targetTokenId = session.RuntimeId;
                Console.WriteLine($"[Victim {session.RuntimeId}] Logging in with Token {_targetTokenId}");
            }
            else // 짝수 (Attacker)
            {
                _targetTokenId = session.RuntimeId - 1; // 짝꿍인 홀수 ID 타겟팅
                Console.WriteLine($"[Attacker {session.RuntimeId}] Will attack Token {_targetTokenId} after delay...");
                
                // Victim이 먼저 로그인할 시간을 벌어줌 (2초 대기)
                Task.Delay(2000).ContinueWith(_ => 
                {
                     SendLoginPacket(session); 
                });
                return; 
            }

            SendLoginPacket(session);
        }

        private void SendLoginPacket(ServerSession session)
        {
            var packet = new C_Login();
            packet.SessionToken = $"CORE_STRESS_TEST_{_targetTokenId}";
            packet.VerifySeed = 123456789;
            session.Send(packet.Encode());
        }

        public void OnLoginResult(ServerSession session, S_LoginResponse result)
        {
            if (session.RuntimeId % 2 != 0) // Victim
            {
                if (result.ServerResult == ServerResult.Success)
                    Console.WriteLine($"[Victim {session.RuntimeId}] Login Success. Waiting for Kick...");
            }
            else // Attacker
            {
                if (result.ServerResult == ServerResult.DuplicateAuth)
                {
                    Console.WriteLine($"[Attacker {session.RuntimeId}] Got DuplicateLogin (Expected). Victim should be kicked now.");

                    Task.Delay(1000).ContinueWith(_ => 
                    {
                        Console.WriteLine($"[Attacker {session.RuntimeId}] Retrying login...");
                        SendLoginPacket(session);
                    });
                }
                else if (result.ServerResult == ServerResult.Success)
                {
                    Console.WriteLine($"[Attacker {session.RuntimeId}] Login Success (Scenario Passed).");
                    Interlocked.Increment(ref ServerSession.ConnectedCount); // 최종 성공 카운트
                }
            }
        }

        public void OnDisconnected(ServerSession session, string? msg)
        {
             if (session.RuntimeId % 2 != 0) // Victim
             {
                 Console.WriteLine($"[Victim {session.RuntimeId}] Disconnected as expected (Kicked). Msg: {msg}");
             }
             else
             {
                 // Attacker가 끊기면 문제
                 Console.WriteLine($"[Attacker {session.RuntimeId}] Disconnected unexpectedly! Msg: {msg}");
             }
        }

        public void OnRecvPacket(ServerSession session, ushort protocolId, ArraySegment<byte> buffer)
        {
            // [Fix] 패킷 매니저에게 처리를 위임해야 핸들러가 호출됨
            PacketManager.OnRecvPacket(session, protocolId, buffer);
        }

        public void Update(ServerSession session)
        {
            // 주기적 작업 불필요
        }
    }
}