using System.Net;
using GameServer.Game.Commands.Transaction;
using GameServer.Network;
using LobbyServer.Lobby.Commands.Transaction;
using LobbyServer.Network.Components;
using PacketGen;
using ServerCore;
using ServerCore.Cipher;
using ServerCore.Infrastructure;
using ServerCore.Packet;

namespace LobbyServer.Network
{
    public class ClientSession : PacketSession
    {
        public ClientSession() : base(AppSide.Server)
        {
            Routing = new RoutingComponent();
            Transaction = new TransactionComponent<ClientSession>(this, LogoutCommand.Execute);
        }

        public readonly RoutingComponent Routing;
        public readonly TransactionComponent<ClientSession> Transaction;

        protected override void OnConnected(EndPoint endPoint)
        {
            Log.Info(this, "OnConnected: Session:{RuntimeId} {EndPoint}", RuntimeId, endPoint);
        }
        protected override void OnRecvPacket(ushort protocolId, ArraySegment<byte> buffer)
        {
            if (!IsCipherEnabled)
            {
                if (protocolId != (ushort)ProtocolId.C_HandshakeSyn)
                {
                    Disconnect($"Unencrypted packet blocked (ID: {(ProtocolId)protocolId})");
                    return;
                }
            }
            PacketManager.OnRecvPacket(this, protocolId, buffer);
        }
        protected override void OnDisconnected(EndPoint endPoint, string? msg)
        {
            Log.Info(this, "OnDisconnected: Session:{0} {1} {2}", RuntimeId, endPoint, msg ?? string.Empty);
            SessionManager.Instance.Leave(this);
            Transaction.OnDisconnected();
            
        }
        protected override void OnSend(int numOfBytes)
        {
            // Console.WriteLine($"Transferred bytes:{numOfBytes}");
        }
    }
}
