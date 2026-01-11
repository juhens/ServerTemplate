using ServerCore;

namespace GameServer.Network
{
    public class SessionManager : BaseSessionManager<ClientSession>
    {
        public static readonly SessionManager Instance = new SessionManager();
        protected override ClientSession Create()
        {
            var session = new ClientSession();
            return session;
        }
    }
}
