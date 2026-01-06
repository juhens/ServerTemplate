using ServerCore.Job;

namespace GameServer.Database
{
    public class DbJobSerializer : JobSerializer
    {
        public DbJobSerializer(IJobScheduler jobScheduler) : base(jobScheduler)
        {
        }
        // [추가] DbManager가 작업을 넣을 수 있도록 래핑
        public void PushTask(Action job)
        {
            Push(job, JobPriority.Critical);
        }

        public void PushTask<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            Push(action, t1, t2, t3, JobPriority.Critical);
        }
        public void PushTask<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            Push(action, t1, t2, t3, t4, JobPriority.Critical);
        }

        // [Override] DB 작업 처리 후 마무리 로직
        protected override void OnPostFlush()
        {
            // 트랜잭션 커밋
            // 예: _dbConnection.Commit(); 또는 Log 기록
        }
    }
}