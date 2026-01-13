using GameServer.Database;
using GameServer.Logic;
using GameServer.Network;
using ServerCore;

namespace GameServer.Common
{
    public class ServerMonitor
    {
        public static ServerMonitor Instance { get; } = new ServerMonitor();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private ServerMonitor() { }

        public void Start()
        {
            Task.Run(() =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(5000);
                    if (_cancellationTokenSource.Token.IsCancellationRequested) break;
                    PrintStatus();
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void PrintStatus()
        {
            var sessionCount = SessionManager.Instance.Count;
            var dbProcessingCount = DbManager.Instance.ProcessingCount;
            var worldCount = Node.Instance.TotalWorldSessionCount;
            var channelCount = Node.Instance.TotalChannelSessionCount;
            var zoneCount = Node.Instance.TotalZoneSessionCount;

            Log.Info(this, "S:{0} | P:{1} | W:{2} | C:{3} | Z:{4}", sessionCount, dbProcessingCount, worldCount, channelCount, zoneCount);
        }
    }
}