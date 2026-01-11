using System.Net;
using ServerCore.Cipher;
using ServerCore.Packet;
using DummyClient.Scenarios;

namespace DummyClient.Network
{
    public class ServerSession : PacketSession
    {
        public new long RuntimeId { get; }
        private static long _idCounter = 0;

        public static void ResetIdCounter()
        {
            Interlocked.Exchange(ref _idCounter, 0);
        }

        public ServerSession() : base(AppSide.Client) 
        {
            RuntimeId = Interlocked.Increment(ref _idCounter);
        }

        public static long TotalCount;
        public static long ConnectedCount;

        public IScenario? Scenario { get; set; }

        protected override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected:{endPoint}");
            Scenario?.OnConnected(this);
        }

        protected override void OnDisconnected(EndPoint endPoint, string? msg)
        {
            Console.WriteLine($"OnDisconnected:{endPoint}\n\t{msg}");
            Scenario?.OnDisconnected(this, msg);
        }

        protected override void OnRecvPacket(ushort protocolId, ArraySegment<byte> buffer)
        {
            Scenario?.OnRecvPacket(this, protocolId, buffer);
        }

        protected override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes:{numOfBytes}");
        }
    }
}
