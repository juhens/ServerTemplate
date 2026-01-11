using ServerCore.Job;

namespace LobbyServer.Lobby
{
    public class Node
    {
        private Node() { }
        public static Node Instance { get; } = new Node();
        private static readonly JobScheduler Scheduler = new();
        public readonly Rooms.Lobby Root = new(Scheduler);

        public void Start()
        {
            var workerThreadCount = Math.Max(2, Environment.ProcessorCount - 2);
            Scheduler.Start(workerThreadCount);
        }

    }
}
