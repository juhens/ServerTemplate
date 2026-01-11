using LobbyServer.Lobby;
using ServerCore.Job;

namespace LobbyServer.Network.Components
{
    public class RoutingComponent
    {
        // Login, Logout
        public JobSerializedRef<long, Node> AccountDbIdRef { get; } = new();
        public JobSerializedRef<Lobby.Rooms.Lobby, Lobby.Rooms.Lobby> LobbyRef { get; } = new();
    }
}