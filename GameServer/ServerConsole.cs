using GameServer.Common;
using GameServer.Database;
using GameServer.Network;
using ServerCore;

namespace GameServer
{
    public class ServerConsole
    {
        public static ServerConsole Instance { get; } = new ServerConsole();

        private bool _isRunning = true;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        private void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            ServerMonitor.Instance.Stop();
            
            // Console.ReadLine()이 블로킹되어 있을 경우 해제하기 위해
            // 새로운 라인을 입력하는 효과
            // Log.Info(typeof(ServerConsole), "");
        }

        public void CommandLoop()
        {
            while (_isRunning)
            {
                var cmd = Console.ReadLine();

                // 서버 종료 중이라면 더 이상 명령 처리 안함
                if (!_isRunning) break;

                if (string.IsNullOrEmpty(cmd))
                {
                    continue;
                }

                if (cmd.Trim().ToLower() == "/exit")
                {
                    HandleExit();
                }
                else if (cmd.Trim().ToLower() == "/force_exit")
                {
                    HandleForceExit();
                }
                else
                {
                    ServerMonitor.Instance.PrintStatus();
                }
            }
        }

        private void HandleExit()
        {
            Log.Info(typeof(ServerConsole), "Initiating Safe Shutdown...");
            
            // 더 이상 콘솔 명령을 받지 않고, 모니터링도 중지
            Stop(); 

            SessionManager.Instance.KickAllUsers();

            var timeout = 30;
            while (DbManager.Instance.ProcessingCount > 0 && timeout > 0)
            {
                Log.Info(typeof(ServerConsole), "Waiting for {ProcessingCount} players to save/leave... ({Timeout}s)", DbManager.Instance.ProcessingCount, timeout);
                Thread.Sleep(1000);
                timeout--;
            }

            Log.Info(typeof(ServerConsole), "Shutdown completed. Bye!");
            Environment.Exit(0);
        }

        private void HandleForceExit()
        {
            Log.Error(typeof(ServerConsole), "Force Exit Triggered during shutdown!");
            Environment.Exit(0);
        }
    }
}