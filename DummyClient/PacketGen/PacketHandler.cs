using DummyClient.Network;
using ServerCore.Packet;

// ReSharper disable once CheckNamespace
namespace PacketGen
{
    public class PacketHandler
    {
        internal static void S_HandshakeSynAck_Handler(PacketSession session, S_HandshakeSynAck ack)
        {
            if (ack.ServerState == ServerState.Success)
            {
                // 1. 암호화 설정
                session.EnableCipher(ack.EncryptionSeed);
                
                // 2. 시나리오에 위임
                if (session is ServerSession serverSession)
                {
                    serverSession.Scenario?.OnHandshakeCompleted(serverSession);
                }
            }
        }

        internal static void S_LoginResponse_Handler(PacketSession session, S_LoginResponse response)
        {
            // 시나리오에 결과 통보
            if (session is ServerSession serverSession)
            {
                serverSession.Scenario?.OnLoginResult(serverSession, response);
            }
        }

        internal static void S_Chat_Handler(PacketSession session, S_Chat recvPacket)
        {
            if (session is not ServerSession serverSession) return;

            // [Global Monitoring]
            // 특정 시나리오와 무관하게 전역적으로 채팅 수신량 집계
            Interlocked.Increment(ref ServerSession.TotalCount);
        }

        internal static void S_SystemMessage_Handler(PacketSession session, S_SystemMessage message)
        {
            throw new NotImplementedException();
        }

        internal static void S_WorldInfoArray_Handler(PacketSession session, S_WorldInfoArray array)
        {
            throw new NotImplementedException();
        }

        internal static void S_PlayerInfoArray_Handler(PacketSession session, S_PlayerInfoArray array)
        {
            throw new NotImplementedException();
        }

        internal static void S_EnterZoneResult_Handler(PacketSession session, S_EnterZoneResult result)
        {
            throw new NotImplementedException();
        }
    }
}