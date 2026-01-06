using GameServer.Database;
using ServerCore.Job;

namespace GameServer.Game.Contexts.Transaction
{
    public class LoadPlayerInfoListContext : BaseContext
    {
        private LoadPlayerInfoListContext() { }
        public static LoadPlayerInfoListContext Create()
        {
            return new LoadPlayerInfoListContext();
        } 
        
        private WriteOnce<short> _worldStaticId = new();
        private WriteOnce<List<PlayerDb>> _playerDbList = new();

        public short WorldStaticId
        {
            get => _worldStaticId.Value;
            set => _worldStaticId.Value = value;
        }
        public List<PlayerDb> PlayerDbList
        {
            get => _playerDbList.Value;
            set => _playerDbList.Value = value;
        }
    }
}
