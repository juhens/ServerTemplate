using GameServer.Network;
using ServerCore.Infrastructure;

namespace GameServer.Logic.Commands.Transaction.Contexts.Interfaces
{
    public interface IAuthContext : IContext<ClientSession>
    {
        public string SessionToken { get; set; }

        public ClientSession ExitSession { get; set; }
    }
}
