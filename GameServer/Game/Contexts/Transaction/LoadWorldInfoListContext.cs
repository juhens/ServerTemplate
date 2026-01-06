using GameServer.Game.Dto;
using ServerCore.Job;

namespace GameServer.Game.Contexts.Transaction
{
    public class LoadWorldInfoListContext : BaseContext
    {
        private LoadWorldInfoListContext() { }

        public static LoadWorldInfoListContext Create() => new LoadWorldInfoListContext();


        private WriteOnce<List<WorldInfoDto>> _worldInfoList = new();

        public List<WorldInfoDto> WorldInfoList
        {
            get => _worldInfoList.Value;
            set => _worldInfoList.Value = value;
        }
    }
}
