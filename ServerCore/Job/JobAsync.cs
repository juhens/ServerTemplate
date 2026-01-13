using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ServerCore.Job
{
    public class JobAsync : JobBase
    {
        private static readonly ConcurrentBag<JobAsync> Pool = new();
        private JobAsync() { }
        private Func<ValueTask> _action = null!;

        public static JobAsync Create(Func<ValueTask> action, JobPriority priority)
        {
            if (!Pool.TryTake(out var job)) job = new JobAsync();
            job._action = action;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override async ValueTask ExecuteAsync()
        {
            if (!Cancel) await _action.Invoke();
            _action = null!;
            Pool.Add(this);
        }
    }
    public class JobAsync<T1> : JobBase
    {
        private static readonly ConcurrentBag<JobAsync<T1>> Pool = new();
        private JobAsync() { }
        private Func<T1, ValueTask> _action = null!;
        private T1 _t1 = default!;

        public static JobAsync<T1> Create(Func<T1, ValueTask> action, T1 t1, JobPriority priority)
        {
            if (!Pool.TryTake(out var job)) job = new JobAsync<T1>();
            job._action = action;
            job._t1 = t1;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override async ValueTask ExecuteAsync()
        {
            if (!Cancel) await _action.Invoke(_t1);
            _action = null!;
            _t1 = default!;
            Pool.Add(this);
        }
    }
    public class JobAsync<T1, T2> : JobBase
    {
        private static readonly ConcurrentBag<JobAsync<T1, T2>> Pool = new();
        private JobAsync() { }
        private Func<T1, T2, ValueTask> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;

        public static JobAsync<T1, T2> Create(Func<T1, T2, ValueTask> action, T1 t1, T2 t2, JobPriority priority)
        {
            if (!Pool.TryTake(out var job)) job = new JobAsync<T1, T2>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }

        public override async ValueTask ExecuteAsync()
        {
            if (!Cancel) await _action.Invoke(_t1, _t2);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            Pool.Add(this);
        }
    }
    public class JobAsync<T1, T2, T3> : JobBase
    {
        private static readonly ConcurrentBag<JobAsync<T1, T2, T3>> Pool = new();
        private JobAsync() { }
        private Func<T1, T2, T3, ValueTask> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;

        public static JobAsync<T1, T2, T3> Create(Func<T1, T2, T3, ValueTask> action, T1 t1, T2 t2, T3 t3, JobPriority priority)
        {
            if (!Pool.TryTake(out var job)) job = new JobAsync<T1, T2, T3>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }

        public override async ValueTask ExecuteAsync()
        {
            if (!Cancel) await _action.Invoke(_t1, _t2, _t3);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            Pool.Add(this);
        }
    }
    public class JobAsync<T1, T2, T3, T4> : JobBase
    {
        private static readonly ConcurrentBag<JobAsync<T1, T2, T3, T4>> Pool = new();
        private JobAsync() { }
        private Func<T1, T2, T3, T4, ValueTask> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;

        public static JobAsync<T1, T2, T3, T4> Create(Func<T1, T2, T3, T4, ValueTask> action, T1 t1, T2 t2, T3 t3, T4 t4, JobPriority priority)
        {
            if (!Pool.TryTake(out var job)) job = new JobAsync<T1, T2, T3, T4>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }

        public override async ValueTask ExecuteAsync()
        {
            if (!Cancel) await _action.Invoke(_t1, _t2, _t3, _t4);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            Pool.Add(this);
        }
    }
    public class JobAsync<T1, T2, T3, T4, T5> : JobBase
    {
        private static readonly ConcurrentBag<JobAsync<T1, T2, T3, T4, T5>> Pool = new();
        private JobAsync() { }
        private Func<T1, T2, T3, T4, T5, ValueTask> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;
        private T5 _t5 = default!;

        public static JobAsync<T1, T2, T3, T4, T5> Create(Func<T1, T2, T3, T4, T5, ValueTask> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, JobPriority priority)
        {
            if (!Pool.TryTake(out var job)) job = new JobAsync<T1, T2, T3, T4, T5>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job._t5 = t5;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }

        public override async ValueTask ExecuteAsync()
        {
            if (!Cancel) await _action.Invoke(_t1, _t2, _t3, _t4, _t5);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            _t5 = default!;
            Pool.Add(this);
        }
    }
}