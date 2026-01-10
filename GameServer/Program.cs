using System.Net;
using GameServer.Cache;
using GameServer.Common;
using GameServer.Database;
using GameServer.Game;
using GameServer.Network;
using ServerCore;

namespace GameServer
{
    public class Program
    {
        private static void Main()
        {
            // TODO: 슬슬 설정파일 읽기 드가야할때


            // conn cache server
            ICacheManager cacheManager = MockCacheManager.Instance;
            cacheManager.Connect("localhost:6379", 5000);

            // init
            LogManager.Initialize();
            Node.Instance.Initialize(0, "0서버",1, new object[1]);
            DbManager.Instance.Initialize(2);
            
            // start
            Node.Instance.Start();
            DbManager.Instance.Start();

            // update to cache server
            var worldStates = Node.Instance.GetWorldInfoList();
            MockRedis.UpdateWorldInfoList(worldStates);


            // network setting
            var host = Dns.GetHostName();
            var ipHost = Dns.GetHostEntry(host);
            var ip = ipHost.AddressList[0];
            var port = 7777;
            var endPoint = new IPEndPoint(ip, port);

            var listener = new Listener();
            listener.Start(endPoint, SessionManager.Instance.Generate, 10, 512);

            // 서버 콘솔 시작
            Log.Info(typeof(Program), "--------------------------------------------------------");
            Log.Info(typeof(Program), "               Server Application Started               ");
            Log.Info(typeof(Program), "--------------------------------------------------------");
            Log.Info(typeof(Program), "     Listening on: {IpAddress}:{Port}", ip, port);
            Log.Info(typeof(Program), "  To exit, press Ctrl+C or type /exit and press Enter.");
            Log.Info(typeof(Program), "--------------------------------------------------------");

            ServerMonitor.Instance.Start();
            ServerConsole.Instance.CommandLoop();
        }
    }
}