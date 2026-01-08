using GameServer.Game.Commands.Transaction.Contexts;
using GameServer.Game.Dto;
using ServerCore.Job;

namespace GameServer.Game.Commands.Transaction.Contexts.Transaction
{
    public class LoadWorldInfoListContext : BaseContext<LoadWorldInfoListContext>
    {
        private WriteOnce<List<WorldInfoDto>> _worldInfoList = new();

        public List<WorldInfoDto> WorldInfoList
        {
            get => _worldInfoList.Value;
            set => _worldInfoList.Value = value;
        }

        protected override void OnInit()
        {
            throw new NotImplementedException();
        }

        protected override void OnDispose()
        {
            throw new NotImplementedException();
        }
    }
}
