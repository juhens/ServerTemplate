using GameServer.Game.Commands.Transaction;
using GameServer.Network;
using ServerCore.Packet;
using ServerCore.Job;

// ReSharper disable once CheckNamespace
namespace PacketGen
{
    public static class PacketHandler
    {
        internal static void C_HandshakeSyn_Handler(PacketSession session, C_HandshakeSyn syn)
        {
            if (session is not ClientSession clientSession) return;

            var synAck = new S_HandshakeSynAck();

            if (syn.ClientVersion < 1)
            {
                synAck.ServerResult = ServerResult.VersionMismatch;
            }
            else
            {
                synAck.ServerTime = DateTime.Now;
                synAck.EncryptionSeed = 123456;
                synAck.ServerResult = ServerResult.Success;
            }

            session.Send(synAck.Encode());
            session.EnableCipher(synAck.EncryptionSeed);
        }

        internal static void C_Login_Handler(PacketSession session, C_Login login)
        {
            if (session is not ClientSession clientSession) return;

            LoginCommand.Execute(clientSession, login);
        }

        internal static void C_Chat_Handler(PacketSession session, C_Chat recvPacket)
        {
            if (session is not ClientSession clientSession) return;

            if (!clientSession.Routing.PlayerRef.TryCapture(out var player)) return;
            if (!clientSession.Routing.ZoneRef.TryCapture(out var zone)) return;
            if (clientSession.Disconnected) return;

            var chatPacket = new S_Chat()
            {
                GameObjRuntimeId = player.RuntimeId,
                Chat = $"From[{player.RuntimeId}]:{recvPacket.Chat}"
            };

            zone.Broadcast(clientSession, chatPacket.Encode(), JobPriority.Normal);
        }

        public static void C_RequestWorldInfoArray_Handler(PacketSession arg1, C_RequestWorldInfoArray arg2)
        {
            throw new NotImplementedException();
        }

        internal static void C_RequestPlayerInfoArray_Handler(PacketSession session, C_RequestPlayerInfoArray array)
        {
            throw new NotImplementedException();
        }

        internal static void C_EnterZone_Handler(PacketSession session, C_EnterZone zone)
        {
            throw new NotImplementedException();
        }

        internal static void C_CreatePlayer_Handler(PacketSession session, C_CreatePlayer player)
        {
            throw new NotImplementedException();
        }

        internal static void C_DeletePlayer_Handler(PacketSession session, C_DeletePlayer player)
        {
            throw new NotImplementedException();
        }

        internal static void C_ChangeChannel_Handler(PacketSession session, C_ChangeChannel channel)
        {
            throw new NotImplementedException();
        }

        internal static void C_ChangeZone_Handler(PacketSession session, C_ChangeZone zone)
        {
            throw new NotImplementedException();
        }
    }
}