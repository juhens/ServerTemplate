using GameServer.Game.Commands.Transaction.Contexts.Interfaces;
using GameServer.Network;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace GameServer.Game.Commands.Transaction.Contexts.Transaction
{
    public class LoginContext : BaseContext<ClientSession, LoginContext>, IAuthContext
    {
        private WriteOnce<string> _sessionToken;
        private WriteOnce<ClientSession> _exitSession;

        public string SessionToken
        {
            get => _sessionToken.Value;
            set => _sessionToken.Value = value;
        }
        public ClientSession ExitSession
        {
            get => _exitSession.Value;
            set => _exitSession.Value = value;
        }

        protected override void OnInit()
        {
            _sessionToken = new WriteOnce<string>();
            _exitSession = new WriteOnce<ClientSession>();
        }
        protected override void OnDispose()
        {
            _sessionToken = default!;
            _exitSession = default!;
        }
    }
}