using GameServer.Network;
using ServerCore.Infrastructure;
using ServerCore.Job;

namespace GameServer.Logic.Rooms
{
    public abstract class GameRoom : BaseRoom<ClientSession>
    {
        private readonly Dictionary<string, long /*runtimeId*/> _nicknameToRuntimeId = [];
        private readonly Dictionary<long /*dbId*/, long /*runtimeId*/> _dbIdToRuntimeId = [];

        protected GameRoom(IJobScheduler scheduler) : base(scheduler) { }

        protected sealed override bool TryGetRuntimeId(ClientSession session, out long runtimeId)
        {
            runtimeId = 0;
            if (!session.Routing.PlayerRef.TryCapture(out var player))
                return false;
            runtimeId = player.RuntimeId;
            return true;
        }

        protected sealed override TransactionResult OnEnter(ClientSession session)
        {
            if (!session.Routing.PlayerRef.TryCapture(out var player)) return TransactionResult.NotRouted;

            if (!_nicknameToRuntimeId.TryAdd(player.Nickname, player.RuntimeId))
            {
                return TransactionResult.DuplicateNickname;
            }

            if (!_dbIdToRuntimeId.TryAdd(player.PlayerDbId, player.RuntimeId))
            {
                _nicknameToRuntimeId.Remove(player.Nickname, out _);
                return TransactionResult.DuplicateDbId;
            }

            return OnEnterGame(session);
        }

        protected sealed override void OnLeave(ClientSession session)
        {
            if (session.Routing.PlayerRef.TryCapture(out var player))
            {
                _nicknameToRuntimeId.Remove(player.Nickname);
                _dbIdToRuntimeId.Remove(player.PlayerDbId);
            }

            OnLeaveGame(session);
        }

        // [편의 기능] 닉네임으로 세션 찾기
        protected ClientSession? FindSessionByNickname(string nickname)
        {
            if (_nicknameToRuntimeId.TryGetValue(nickname, out var runtimeId))
                return FindSession(runtimeId);
            return null;
        }

        protected ClientSession? FindSessionByPlayerDbId(long playerDbId)
        {
            if (_dbIdToRuntimeId.TryGetValue(playerDbId, out var runtimeId))
                return FindSession(runtimeId);
            return null;
        }

        protected abstract TransactionResult OnEnterGame(ClientSession session);
        protected abstract void OnLeaveGame(ClientSession session);
    }
}
