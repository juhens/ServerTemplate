using GameServer.Game.Dto;
using GameServer.Game.Rooms;
using ServerCore.Job;

namespace GameServer.Game
{
    public class Node
    {
        private Node() {}
        public static Node Instance { get; } = new Node();
        private readonly JobScheduler _executor = new();
        private readonly Dictionary<int/*worldStaticId*/, World> _worlds = [];
        public void Initialize(short worldStaticId, string worldName, short channelCount, object[] zoneData)
        {
            _worlds.Add(worldStaticId, new World(_executor, worldStaticId, worldName));


            foreach (var world in _worlds.Values)
            {
                world.Initialize(channelCount, zoneData);
            }
        }
        public void Start()
        {
            if (_worlds.Count == 0) throw new InvalidOperationException("Node must be initialized first. Call Initialize() in Program.cs");
            var workerThreadCount = Math.Max(2, Environment.ProcessorCount - 2);
            _executor.Start(workerThreadCount);
        }

        public World? FindWorld(short worldStaticId)
        {
            return _worlds.GetValueOrDefault(worldStaticId);
        }

        public List<WorldInfoDto> GetWorldInfoList()
        {
            var worldStates = new List<WorldInfoDto>();

            foreach (var world in _worlds.Values)
            {
                worldStates.Add(world.GetWorldInfo());
            }

            return worldStates;
        }

        public int TotalWorldSessionCount => _worlds.Values.Sum(w => w.SessionCount);
        public int TotalChannelSessionCount => _worlds.Values.Sum(w => w.TotalChannelSessionCount);
        public int TotalZoneSessionCount => _worlds.Values.Sum(w => w.TotalZoneSessionCount);
    }
}
