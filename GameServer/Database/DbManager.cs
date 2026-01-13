using System.Collections.Concurrent;
using GameServer.Cache;
using GameServer.Database.Commands;
using GameServer.Logic.Commands.Transaction.Contexts.Interfaces;
using GameServer.Logic.Commands.Transaction.Contexts.Transaction;
using GameServer.Network;
using ServerCore;
using ServerCore.Infrastructure;
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

        private void Push<T1>(Action<T1> job, T1 t1, long key)
        {
            if (_shardCount == 0) return;
            var index = (int)((ulong)key % (ulong)_shardCount);
            _shards[index].PushTask(job, t1);
        }
        private void Push<T1, T2>(Action<T1, T2> job, T1 t1, T2 t2, long key)
        {
            if (_shardCount == 0) return;
            var index = (int)((ulong)key % (ulong)_shardCount);
            _shards[index].PushTask(job, t1, t2);
        }
        private void Push<T1>(Action<T1> action, T1 t1, string key)
        {
            if (_shardCount == 0) return;
            var index = (int)((uint)key.GetHashCode() % (uint)_shardCount);
            _shards[index].PushTask(action, t1);
        }


        public void Auth(LoginContext ctx)
        {
            Push(AuthJob, ctx, ctx.SessionToken);
        }
        private static void AuthJob(LoginContext ctx)
        {
            try
            {
                if (ctx.Session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }

                long accountDbId;
                if (ctx.SessionToken.StartsWith("CORE_STRESS_TEST_"))
                {
                    var dbIdStr = ctx.SessionToken.Substring("CORE_STRESS_TEST_".Length);
                    if (!long.TryParse(dbIdStr, out accountDbId))
                    {
                        ctx.Result = TransactionResult.FailedDummyAuth;
                        ctx.AccountDbId = -1;
                        return;
                    }
                }
                else
                {
                    var nullableAccountDbId = MockRedis.GetUserIdByToken(ctx.SessionToken);
                    if (nullableAccountDbId == null)
                    {
                        ctx.Result = TransactionResult.InvalidToken;
                        ctx.AccountDbId = -1;
                        return;
                    }
                    accountDbId = nullableAccountDbId.Value;
                }
                ctx.Result = TransactionResult.Success;
                ctx.AccountDbId = accountDbId;
            }
            catch (Exception e)
            {
                Log.Error(typeof(DbManager), "{0}", e);
                ctx.Result = TransactionResult.Exception;
            }
            finally
            {
                ctx.OnCompleted?.Invoke(ctx);
            }
        }

        public void Login<T>(T ctx) where T: IAuthContext
        {
            Push(LoginJob, this, ctx, ctx.AccountDbId);
        }
        private static void LoginJob<T>(DbManager @this, T ctx) where T : IAuthContext
        {
            try
            {
                if (ctx.Session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }

                if (@this._remainSavePlayerDb.TryGetValue(ctx.AccountDbId, out _))
                {
                    ctx.Result = TransactionResult.TryAgainLater;
                    return;
                }

                // var accountDb = DbCommand.GetAccount(accountDbId);
                // 유저 Db를 불러와서 벤 체크 등을 한다.

                // 중복로그인 체크
                if (!@this._authSessions.TryAdd(ctx.AccountDbId, ctx.Session))
                {
                    ctx.ExitSession = @this._authSessions[ctx.AccountDbId];
                    ctx.Result = TransactionResult.DuplicateAuth;
                    return;
                }

                if (!ctx.Session.Routing.AccountDbIdRef.TryAttach(ctx.AccountDbId))
                {
                    @this._authSessions.TryRemove(ctx.AccountDbId, out _);
                    ctx.Result = TransactionResult.FailedAttach;
                    return;
                }

                ctx.Result = TransactionResult.Success;
            }
            catch(Exception e)
            {
                Log.Error(typeof(DbManager), "{0}", e );
                ctx.Result = TransactionResult.Exception;
            }
            finally
            {
                ctx.Complete();
            }
        }

        public void Logout<T>(T ctx) where T : IContext<ClientSession>
        {
            Push(LogoutJob, this, ctx, ctx.AccountDbId);
        }
        private static void LogoutJob<T>(DbManager @this, T ctx) where T : IContext<ClientSession>
        {
            var session = ctx.Session;
            if (!@this._authSessions.TryRemove(ctx.AccountDbId, out _))
            {
                Log.Error(typeof(DbManager), "LogoutFailed: Session:{0} AccountDbId:{1}", session.RuntimeId, ctx.AccountDbId);
            }
            if (!session.Routing.AccountDbIdRef.TryDetach())
                Log.Error(typeof(DbManager), "FailedDetach(AccountDbId): Session:{0} AccountDbId:{1}", session.RuntimeId, ctx.AccountDbId);
            ctx.Complete();
        }

        public void SavePlayerWithDetach<T>(T ctx) where T : IPlayerDbContext
        {
            // 중요!!! ctx 에서 직접 accountDbId 읽기 금지
            Push(SavePlayerWithDetachJob, this, ctx, ctx.PlayerDb.PlayerDbId);
        }
        private static void SavePlayerWithDetachJob<T>(DbManager @this, T ctx) where T : IPlayerDbContext
        {
            var session = ctx.Session;
            try
            {
                DbCommand.SavePlayer(ctx.PlayerDb);
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

            if (!session.Routing.PlayerRef.TryDetach())
                Log.Error(typeof(DbManager), "FailedDetach(PlayerDbId): Session:{0} AccountDbId:{1}", session.RuntimeId, ctx.AccountDbId);
            ctx.Complete();
        }

        public void FindPlayer<T>(T ctx) where T : IContext<ClientSession>, ILoadPlayerDbContext
        {
            Push(FindPlayerJob, this, ctx, ctx.AccountDbId);
        }
        private static void FindPlayerJob<T>(DbManager @this, T ctx) where T : IContext<ClientSession>, ILoadPlayerDbContext
        {
            var session = ctx.Session;
            try
            {
                if (session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }

                var playerDbList = DbCommand.GetPlayerList(ctx.AccountDbId, ctx.World.StaticId);
                var playerDb = playerDbList.FirstOrDefault(p => p.Index == ctx.PlayerIndex);

                if (playerDb is null)
                {
                    Log.Warn(typeof(DbManager),
                        "FailedFindPlayer: Session:{0} AccountDbId:{1} PlayerIndex:{2} NotFound", session.RuntimeId,
                        ctx.AccountDbId, ctx.PlayerIndex);
                    ctx.Result = TransactionResult.BadPlayerIndex;
                    return;
                }

                ctx.PlayerDbId = playerDb.PlayerDbId;
                ctx.Result = TransactionResult.Success;
            }
            catch (Exception e)
            {
                Log.Warn(typeof(DbManager), "FailedFindPlayer: Session:{0} AccountDbId:{1} Error: {2}",
                    session.RuntimeId, ctx.AccountDbId, e.Message);
                ctx.Result = TransactionResult.FailedFindPlayer;
            }
            finally
            {
                ctx.Complete();
            }
        }

        public void LoadPlayer<T>(T ctx) where T : ILoadPlayerDbContext, IPlayerDbContext
        {
            Push(LoadPlayerJob, ctx, ctx.PlayerDbId);
        }
        private static void LoadPlayerJob<T>(T ctx) where T : ILoadPlayerDbContext, IPlayerDbContext
        {
            var session = ctx.Session;
            try
            {
                if (session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }

                //TODO: 일단 중복코드는 개념상 표기이고, GetPlayer 라는 메서드로 추후 교체한다.
                var playerDbList = DbCommand.GetPlayerList(ctx.AccountDbId, ctx.World.StaticId);
                var playerDb = playerDbList.FirstOrDefault(p => p.Index == ctx.PlayerIndex);

                if (playerDb is null)
                {
                    Log.Warn(typeof(DbManager),
                        "LoadPlayerInternalFailed: Session:{0} AccountDbId:{1} PlayerIndex:{2} NotFound",
                        session.RuntimeId, ctx.AccountDbId, ctx.PlayerIndex);
                    ctx.Result = TransactionResult.BadPlayerDbId;
                    return;
                }

                ctx.PlayerDb = playerDb;
                ctx.Result = TransactionResult.Success;
            }
            catch (Exception e)
            {
                Log.Warn(typeof(DbManager), "FailedFindPlayer: Session:{0} AccountDbId:{1} Error: {2}",
                    session.RuntimeId, ctx.AccountDbId, e.Message);
                ctx.Result = TransactionResult.FailedLoadPlayer;
            }
            finally
            {
                ctx.Complete();
            }
        }

        public void LoadWorldInfoList(LoadWorldInfoListContext ctx)
        {
            Push(LoadWorldInfoListJob, ctx, ctx.AccountDbId);
        }
        private static void LoadWorldInfoListJob(LoadWorldInfoListContext ctx)
        {
            var session = ctx.Session;
            try
            {
                if (session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }
                ctx.WorldInfoList = MockRedis.GetWorldInfoList();
                ctx.Result = TransactionResult.Success;
            }
            catch (Exception e)
            {
                Log.Warn(typeof(DbManager), "FailedLoadWorldInfoList: Session:{0} AccountDbId:{1} Error: {2}",
                    session.RuntimeId, ctx.AccountDbId, e.Message);
                ctx.Result = TransactionResult.FailedLoadWorldInfoList;
                ctx.WorldInfoList = [];
            }
            finally
            {
                ctx.OnCompleted?.Invoke(ctx);
            }
        }

        public void LoadPlayerInfoList(LoadPlayerInfoListContext ctx)
        {
            Push(LoadPlayerInfoListJob, ctx, ctx.AccountDbId);
        }
        private static void LoadPlayerInfoListJob(LoadPlayerInfoListContext ctx)
        {
            var session = ctx.Session;
            try
            {
                if (session.Disconnected)
                {
                    ctx.Result = TransactionResult.Disconnected;
                    return;
                }
                ctx.PlayerDbList = DbCommand.GetPlayerList(ctx.AccountDbId, ctx.WorldStaticId);
                ctx.Result = TransactionResult.Success;
            }
            catch (Exception e)
            {
                Log.Warn(typeof(DbManager), "FailedLoadPlayerInfoList: Session:{0} AccountDbId:{1} Error: {2}",
                    session.RuntimeId, ctx.AccountDbId, e.Message);
                ctx.Result = TransactionResult.FailedLoadPlayerInfoList;
                ctx.PlayerDbList = [];
            }
            finally
            {
                ctx.OnCompleted?.Invoke(ctx);
            }
        }
    }
}