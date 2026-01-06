using DummyClient.Network;
using PacketGen;

namespace DummyClient.Scenarios
{
    public interface IScenario
    {
        void OnConnected(ServerSession session);
        void OnRecvPacket(ServerSession session, ushort protocolId, ArraySegment<byte> buffer);
        void OnDisconnected(ServerSession session, string? msg);
        
        // 중앙 집중형 루프에서 호출될 업데이트 메서드
        void Update(ServerSession session);

        // [Refactoring] PacketHandler로부터 위임받을 고수준 이벤트
        void OnHandshakeCompleted(ServerSession session);
        void OnLoginResult(ServerSession session, S_LoginResponse result);
    }
}