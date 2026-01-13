using GameServer.Database;
using GameServer.Logic.Objects;
using GameServer.Logic.Rooms;
using ServerCore.Job;

// Change Channel Transaction
// Network -> (check route Zone)Zone.Leave() -> TryDetach Zone
// -> (check route Channel)Channel.Leave() -> TryDetach Channel -> callback[create new RoutingTargetContext]
// -> Channel.Enter() -> TryAttach Channel -> Zone.Enter() -> TryAttach Zone -> Send(MoveChannelResult)

// Change Zone Transaction
// Network -> (check route Zone)Zone.Leave() -> TryDetach Zone -> callback[create new RoutingTargetContext]
// -> Zone.Enter() -> TryAttach Zone -> Send(MoveChannelResult)

namespace GameServer.Network.Components
{
    public class RoutingComponent
    {
        // Login, Logout
        public JobSerializedRef<long, DbJobSerializer> AccountDbIdRef { get; } = new();
        // Load PlayerDb, Save PlayerDb, TryDetach
        public JobSerializedRef<Player, DbJobSerializer> PlayerRef { get; } = new();
        // Enter World, Leave World
        public JobSerializedRef<World, World> WorldRef { get; } = new();
        // Enter Channel, Leave Channel
        public JobSerializedRef<Channel, Channel> ChannelRef { get; } = new();
        // Enter Zone, Leave Zone
        public JobSerializedRef<Zone, Zone> ZoneRef { get; } = new();
    }
}