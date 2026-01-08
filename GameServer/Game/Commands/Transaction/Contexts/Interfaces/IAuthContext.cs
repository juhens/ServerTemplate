using GameServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Game.Commands.Transaction.Contexts.Interfaces
{
    public interface IAuthContext : IContext
    {
        public string SessionToken { get; set; }

        public ClientSession ExitSession { get; set; }
    }
}
