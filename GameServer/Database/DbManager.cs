using System.Collections.Concurrent;
using GameServer.Game.Contexts.Interfaces;
using GameServer.Game.Contexts.Transaction;
using GameServer.Network;
using ServerCore;
using ServerCore.Job;

namespace GameServer.Database
{
    public class DbManager
    {
        public static DbManager Instance { get; } = new DbManager();

        private readonly JobScheduler _distributor = new();
        private readonly List<DbJobSerializer> _shards = [];

        private readonly ConcurrentDictionary<long /*accountDbId*/, ClientSession> _authSessions = [];
        private readonly ConcurrentDictionary<long /*accountDbId*/, PlayerDb> _remainSavePlayerDb = [];
        public int ProcessingCount => _remainSavePlayerDb.Count;

        private int _shardCount = 0;

        private DbManager() { }

        public void Initialize(int shardCount)
        {
            _shardCount = shardCount;

            for (var i = 0; i < shardCount; i++)
            {
                var worker = new DbJobSerializer(_distributor);
                _shards.Add(worker);
            }
        }

        public void Start()
        {
            if (_shardCount == 0) throw new InvalidOperationException("DbManager must be initialized first. Call Initialize() in Program.cs");
            _distributor.Start(2);
        }
        public void Push(Action job, long key)
        {
            if (_shardCount == 0) return;
            var index = (int)((ulong)key % (ulong)_shardCount);
            _shards[index].PushTask(job);
        }

        private void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3, long key)
        {
            if (_shardCount == 0) return;
            var index = (int)((ulong)key % (ulong)_shardCount);
            _shards[index].PushTask(action, t1, t2, t3);
        }
        private void Push<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4, long key)
        {
            if (_shardCount == 0) return;
            var index = (int)((ulong)key % (ulong)_shardCount);
            _shards[index].PushTask(action, t1, t2, t3, t4);
        }
        private void Push<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4, string key)
        {
            if (_shardCount == 0) return;
            var index = (int)((uint)key.GetHashCode() % (uint)_shardCount);
            _shards[index].PushTask(action, t1, t2, t3, t4);
        }

        public void Push(Action job, long key1, long key2)
        {
            if (_shardCount == 0) return;
            var anchorKey = Math.Min(key1, key2);
            var index = (int)((ulong)anchorKey % (ulong)_shardCount);
            _shards[index].PushTask(job);
        }



        public void Login(ClientSession session, LoginContext ctx, Action<ClientSession, LoginContext> callback)
        {
            var token = ctx.SessionToken;
            Push(LoginJob, this, session, ctx, callback, token);
        }
        private static void LoginJob(DbManager @this, ClientSession session, LoginContext ctx, Action<ClientSession, LoginContext> callback)
        {
            long accountDbId;
            if (ctx.SessionToken.StartsWith("CORE_STRESS_TEST_"))
            {
                var dbIdStr = ctx.SessionToken.Substring("CORE_STRESS_TEST_".Length);
                if (!long.TryParse(dbIdStr, out accountDbId))
                {
                    ctx.Result = TransactionResult.DummyAuthFailed;
                    callback.Invoke(session, ctx);
                    return;
                }
            }
            else
            {
                var nullableAccountDbId = MockRedis.GetUserIdByToken(ctx.SessionToken);
                if (nullableAccountDbId == null)
                {
                    ctx.Result = TransactionResult.InvalidToken;
                    callback.Invoke(session, ctx);
                    return;
                }
                accountDbId = nullableAccountDbId.Value;
            }

            ctx.AccountDbId = accountDbId;
            @this.LoginInternal(session, ctx, callback);
        }
        private void LoginInternal(ClientSession session, LoginContext ctx, Action<ClientSession, LoginContext> callback)
        {
            Push(LoginInternalJob, this, session, ctx, callback, ctx.AccountDbId);
        }
        private static void LoginInternalJob(DbManager @this, ClientSession session, LoginContext ctx, Action<ClientSession, LoginContext> callback)
        {
            // 이전 저장 실패한 플레이어 DB가 있는지 확인
            if (@this._remainSavePlayerDb.TryGetValue(ctx.AccountDbId, out _))
            {
                ctx.Result = TransactionResult.TryAgainLater;
                callback.Invoke(session, ctx);
                return;
            }

            // var accountDb = GameDbCommand.GetAccount(accountDbId);
            // 유저 Db를 불러와서 벤 체크 등을 한다.

            // 중복로그인 체크
            if (!@this._authSessions.TryAdd(ctx.AccountDbId, session))
            {
                ctx.ExitSession = @this._authSessions[ctx.AccountDbId];
                ctx.Result = TransactionResult.DuplicateAuth;
                callback.Invoke(session, ctx);
                return;
            }

            session.Routing.AccountDbIdRef.Attach(ctx.AccountDbId);
            ctx.Result = TransactionResult.Success;
            callback.Invoke(session, ctx);
        }

        public void Logout<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext
        {
            Push(LogoutJob, this, session, ctx, callback, ctx.AccountDbId);
        }
        private static void LogoutJob<T>(DbManager @this, ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext
        {
            if (!@this._authSessions.TryRemove(ctx.AccountDbId, out _))
            {
                Log.Error(typeof(DbManager), "LogoutFailed: Session:{0} AccountDbId:{1}", session.RuntimeId, ctx.AccountDbId);
            }
            session.Routing.AccountDbIdRef.Detach();
            callback.Invoke(session, ctx);
        }

        public void SavePlayerDb<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IPlayerDbContext
        {
            Push(SavePlayerDbJob, this, session, ctx, callback, ctx.PlayerDb.PlayerDbId);
        }
        private static void SavePlayerDbJob<T>(DbManager @this, ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IPlayerDbContext
        {
            try
            {
                GameDbCommand.SavePlayer(ctx.PlayerDb);
                Log.Debug(typeof(DbManager), "SavedSuccess: Session:{0} PlayerDbId:{1}", session.RuntimeId, ctx.PlayerDb.PlayerDbId);
            }
            catch (Exception e)
            {
                Log.Warn(typeof(DbManager), "SaveFailed: Session:{0} PlayerDbId:{1} Error: {2}", session.RuntimeId, ctx.PlayerDb.PlayerDbId, e.Message);

                // TODO: 일단 실패객체 관리 예비용 지금은 얘를 쓰진않는다
                if (!@this._remainSavePlayerDb.TryAdd(ctx.PlayerDb.AccountDbId, ctx.PlayerDb))
                {
                    Log.Error(typeof(DbManager), "FallbackFailed: AccountDbId already exists. Session:{0}", session.RuntimeId);
                }
            }
            finally
            {
                @this.DetachPlayer(session, ctx, callback);
            }
        }
        public void DetachPlayer<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IPlayerDbContext
        {
            Push(DetachPlayerJob, session, ctx, callback, ctx.PlayerDb.PlayerDbId);
        }
        private static void DetachPlayerJob<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IPlayerDbContext
        {
            session.Routing.PlayerRef.Detach();
            callback.Invoke(session, ctx);
        }

        public void LoadPlayer<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext, ILoadPlayerDbContext, IPlayerDbContext
        {
            Push(LoadPlayerJob, this, session, ctx, callback, ctx.AccountDbId);
        }
        private static void LoadPlayerJob<T>(DbManager @this, ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext, ILoadPlayerDbContext, IPlayerDbContext
        {
            try
            {
                var playerDbList = GameDbCommand.GetPlayerList(ctx.AccountDbId, ctx.World.StaticId);
                var playerDb = playerDbList.FirstOrDefault(p => p.Index == ctx.PlayerIndex);

                if (playerDb is null)
                {
                    Log.Warn(typeof(DbManager), "LoadPlayerFailed: Session:{0} AccountDbId:{1} PlayerIndex:{2} NotFound", session.RuntimeId, ctx.AccountDbId, ctx.PlayerIndex);
                    ctx.Result = TransactionResult.LoadPlayerFailed;
                    callback.Invoke(session, ctx);
                    return;
                }

                ctx.PlayerDbId = playerDb.PlayerDbId;
            }
            catch (Exception e)
            {
                Log.Warn(typeof(DbManager), "LoadPlayerInfoListFailed: Session:{0} AccountDbId:{1} Error: {2}", session.RuntimeId, ctx.AccountDbId, e.Message);
                ctx.Result = TransactionResult.LoadPlayerFailed;
                callback.Invoke(session, ctx);
                return;
            }
            @this.LoadPlayerInternal(session, ctx, callback);
        }
        private void LoadPlayerInternal<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext, ILoadPlayerDbContext, IPlayerDbContext
        {
            Push(LoadPlayerInternalJob, session, ctx, callback, ctx.PlayerDbId);
        }
        private static void LoadPlayerInternalJob<T>(ClientSession session, T ctx, Action<ClientSession, T> callback) where T : IContext, ILoadPlayerDbContext, IPlayerDbContext
        {
            //TODO: 일단 중복코드는 개념상 표기이고, GetPlayer 라는 메서드로 추후 교체한다.
            var playerDbList = GameDbCommand.GetPlayerList(ctx.AccountDbId, ctx.World.StaticId);
            var playerDb = playerDbList.FirstOrDefault(p => p.Index == ctx.PlayerIndex);

            if (playerDb is null)
            {
                Log.Warn(typeof(DbManager), "LoadPlayerInternalFailed: Session:{0} AccountDbId:{1} PlayerIndex:{2} NotFound", session.RuntimeId, ctx.AccountDbId, ctx.PlayerIndex);
                ctx.Result = TransactionResult.LoadPlayerFailed;
                callback.Invoke(session, ctx);
                return;
            }
            ctx.PlayerDb = playerDb;
            ctx.Result = TransactionResult.Success;
            callback.Invoke(session, ctx);
        }

        public void LoadWorldInfoList(ClientSession session, LoadWorldInfoListContext ctx, Action<ClientSession, LoadWorldInfoListContext> callback)
        {
            Push(() =>
            {
                try
                {
                    ctx.WorldInfoList = MockRedis.GetWorldInfoList();
                    ctx.Result = TransactionResult.Success;
                }
                catch (Exception e)
                {
                    Log.Warn(typeof(DbManager), "LoadWorldInfoListFailed: Session:{0} AccountDbId:{1} Error: {2}", session.RuntimeId, ctx.AccountDbId, e.Message);
                    ctx.Result = TransactionResult.LoadWorldInfoListFailed;
                    ctx.WorldInfoList = [];
                }
                callback.Invoke(session, ctx);
            }, ctx.AccountDbId);
        }
        public void LoadPlayerInfoList(ClientSession session, LoadPlayerInfoListContext ctx, Action<ClientSession, LoadPlayerInfoListContext> callback)
        {
            Push(() =>
            {
                try
                {
                    ctx.PlayerDbList = GameDbCommand.GetPlayerList(ctx.AccountDbId, ctx.WorldStaticId);
                    ctx.Result = TransactionResult.Success;
                }
                catch (Exception e)
                {
                    Log.Warn(typeof(DbManager), "LoadPlayerInfoListFailed: Session:{0} AccountDbId:{1} Error: {2}", session.RuntimeId, ctx.AccountDbId, e.Message);
                    ctx.Result = TransactionResult.LoadPlayerInfoListFailed;
                    ctx.PlayerDbList = [];
                }
                callback.Invoke(session, ctx);
            }, ctx.AccountDbId);
        }
    }
}