using GameServer.Network;
using ServerCore.Job;

namespace GameServer.Game.Contexts.Transaction
{
    public class LoginContext : BaseContext
    {
        public static LoginContext Create()
        {
            return new LoginContext();
        }

        private WriteOnce<string> _sessionToken = new();
        private WriteOnce<ClientSession> _exitSession = new();
        
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
    }
}