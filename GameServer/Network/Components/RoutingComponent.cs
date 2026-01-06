using GameServer.Database;
using GameServer.Game.Objects;
using GameServer.Game.Rooms;
using ServerCore.Job;

// Change Channel Transaction
// Network -> (check route Zone)Zone.Leave() -> Detach Zone
// -> (check route Channel)Channel.Leave() -> Detach Channel -> callback[create new RoutingTargetContext]
// -> Channel.Enter() -> Attach Channel -> Zone.Enter() -> Attach Zone -> Send(MoveChannelResult)

// Change Zone Transaction
// Network -> (check route Zone)Zone.Leave() -> Detach Zone -> callback[create new RoutingTargetContext]
// -> Zone.Enter() -> Attach Zone -> Send(MoveChannelResult)

namespace GameServer.Network.Components
{
    public class RoutingComponent
    {
        // Login, Logout
        public JobSerializedRef<long, DbJobSerializer> AccountDbIdRef { get; } = new();
        // Load PlayerDb, Save PlayerDb, Detach
        public JobSerializedRef<Player, DbJobSerializer> PlayerRef { get; } = new();
        // Enter World, Leave World
        public JobSerializedRef<World, World> WorldRef { get; } = new();
        // Enter Channel, Leave Channel
        public JobSerializedRef<Channel, Channel> ChannelRef { get; } = new();
        // Enter Zone, Leave Zone
        public JobSerializedRef<Zone, Zone> ZoneRef { get; } = new();
    }
}