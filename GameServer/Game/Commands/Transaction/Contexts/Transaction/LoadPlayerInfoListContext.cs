using GameServer.Database;
using GameServer.Game.Commands.Transaction.Contexts;
using ServerCore.Job;

namespace GameServer.Game.Commands.Transaction.Contexts.Transaction
{
    public class LoadPlayerInfoListContext : BaseContext<LoadPlayerInfoListContext>
    {
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

        protected override void OnDispose()
        {
            _worldStaticId = new WriteOnce<short>();
            _playerDbList = new WriteOnce<List<PlayerDb>>();
        }

        protected override void OnInit()
        {
            _worldStaticId = default!;
            _playerDbList = default!;
        }
    }
}
