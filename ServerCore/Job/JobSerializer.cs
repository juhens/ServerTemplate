using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore.Job
{
    public abstract class JobSerializer
    {
        protected JobSerializer(IJobScheduler jobScheduler)
        {
            _executorPush = jobScheduler.Push;
        }

        private readonly JobQueue _jobQueue = new();
        private readonly Action<JobSerializer> _executorPush;
        private volatile int _isScheduled;

        private static readonly AsyncLocal<JobSerializer?> InternalCurrent = new();
        public static JobSerializer? Current
        {
            get => InternalCurrent.Value;
            private set => InternalCurrent.Value = value;
        }

        private static readonly AsyncLocal<bool> InternalIsExecutorPush = new();
        public static bool IsExecutorPush
        {
            get => InternalIsExecutorPush.Value;
            private set => InternalIsExecutorPush.Value = value;
        }

        public void Push(IJob job)
        {
            _jobQueue.Push(job);
            RegisterSchedule();
        }

        protected void Push(Action action, JobPriority jobPriority)
        {
            _jobQueue.Push(Job.Create(action, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1>(Action<T1> action, T1 t1, JobPriority jobPriority)
        {
            _jobQueue.Push(Job<T1>.Create(action, t1, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2, JobPriority jobPriority)
        {
            _jobQueue.Push(Job<T1, T2>.Create(action, t1, t2, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3, JobPriority jobPriority)
        {
            _jobQueue.Push(Job<T1, T2, T3>.Create(action, t1, t2, t3, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4, JobPriority jobPriority)
        {
            _jobQueue.Push(Job<T1, T2, T3, T4>.Create(action, t1, t2, t3, t4, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, JobPriority jobPriority)
        {
            _jobQueue.Push(Job<T1, T2, T3, T4, T5>.Create(action, t1, t2, t3, t4, t5, jobPriority));
            RegisterSchedule();
        }


        protected void Push(Func<ValueTask> action, JobPriority jobPriority)
        {
            _jobQueue.Push(JobAsync.Create(action, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1>(Func<T1, ValueTask> action, T1 t1, JobPriority jobPriority)
        {
            _jobQueue.Push(JobAsync<T1>.Create(action, t1, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2>(Func<T1, T2, ValueTask> action, T1 t1, T2 t2, JobPriority jobPriority)
        {
            _jobQueue.Push(JobAsync<T1, T2>.Create(action, t1, t2, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2, T3>(Func<T1, T2, T3, ValueTask> action, T1 t1, T2 t2, T3 t3, JobPriority jobPriority)
        {
            _jobQueue.Push(JobAsync<T1, T2, T3>.Create(action, t1, t2, t3, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2, T3, T4>(Func<T1, T2, T3, T4, ValueTask> action, T1 t1, T2 t2, T3 t3, T4 t4, JobPriority jobPriority)
        {
            _jobQueue.Push(JobAsync<T1, T2, T3, T4>.Create(action, t1, t2, t3, t4, jobPriority));
            RegisterSchedule();
        }
        protected void Push<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, ValueTask> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, JobPriority jobPriority)
        {
            _jobQueue.Push(JobAsync<T1, T2, T3, T4, T5>.Create(action, t1, t2, t3, t4, t5, jobPriority));
            RegisterSchedule();
        }



        private void RegisterSchedule()
        {
            if (Interlocked.CompareExchange(ref _isScheduled, 1, 0) != 0) return;
            try
            {
                IsExecutorPush = true;
                _executorPush.Invoke(this);
            }
            finally
            {
                IsExecutorPush = false;
            }
        }
        public async ValueTask ExecuteAsync()
        {
            var prev = Current;
            Current = this;

            try
            {
                if (!_jobQueue.IsEmpty)
                {
                    await _jobQueue.FlushAsync();
                }
                OnPostFlush();
            }
            finally
            {
                Current = prev;

                Interlocked.Exchange(ref _isScheduled, 0);

                if (!_jobQueue.IsEmpty)
                {
                    RegisterSchedule();
                }
            }
        }
        protected virtual void OnPostFlush() {}
    }
}