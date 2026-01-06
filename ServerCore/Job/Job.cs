using System;
using System.Collections.Concurrent;

namespace ServerCore.Job
{
    public enum JobPriority
    {
        Low,
        Normal,
        Critical,
    }

    public interface IJob
    {
        public void Execute();
        public bool Cancel { get; }
        public JobPriority Priority { get; }
    }

    public abstract class JobBase : IJob
    {
        public abstract void Execute();

        public bool Cancel { get; set; }
        public JobPriority Priority { get; set; }
    }

    public class Job : JobBase
    {
        private static readonly ConcurrentQueue<Job> Pool = new();
        private Job() { }
        private Action _action = null!;

        public static Job Create(Action action, JobPriority priority = JobPriority.Normal)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job();
            job._action = action;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke();
            _action = null!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1>> Pool = new();
        private Job() { }
        private Action<T1> _action = null!;
        private T1 _t1 = default!;

        public static Job<T1> Create(Action<T1> action, T1 t1, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1>();
            job._action = action;
            job._t1 = t1;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1);
            _action = null!;
            _t1 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2>> Pool = new();
        private Job() { }
        private Action<T1, T2> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;

        public static Job<T1, T2> Create(Action<T1, T2> action, T1 t1, T2 t2, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2, T3> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2, T3>> Pool = new();
        private Job() { }
        private Action<T1, T2, T3> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;

        public static Job<T1, T2, T3> Create(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2, T3>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2, _t3);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2, T3, T4> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2, T3, T4>> Pool = new();
        private Job() { }
        private Action<T1, T2, T3, T4> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;

        public static Job<T1, T2, T3, T4> Create(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2, T3, T4>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2, _t3, _t4);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2, T3, T4, T5> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2, T3, T4, T5>> Pool = new();
        private Job() { }
        private Action<T1, T2, T3, T4, T5> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;
        private T5 _t5 = default!;

        public static Job<T1, T2, T3, T4, T5> Create(Action<T1, T2, T3, T4, T5> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2, T3, T4, T5>();
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
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2, _t3, _t4, _t5);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            _t5 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2, T3, T4, T5, T6> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2, T3, T4, T5, T6>> Pool = new();
        private Job() { }
        private Action<T1, T2, T3, T4, T5, T6> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;
        private T5 _t5 = default!;
        private T6 _t6 = default!;

        public static Job<T1, T2, T3, T4, T5, T6> Create(Action<T1, T2, T3, T4, T5, T6> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2, T3, T4, T5, T6>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job._t5 = t5;
            job._t6 = t6;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2, _t3, _t4, _t5, _t6);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            _t5 = default!;
            _t6 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2, T3, T4, T5, T6, T7> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2, T3, T4, T5, T6, T7>> Pool = new();
        private Job() { }
        private Action<T1, T2, T3, T4, T5, T6, T7> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;
        private T5 _t5 = default!;
        private T6 _t6 = default!;
        private T7 _t7 = default!;

        public static Job<T1, T2, T3, T4, T5, T6, T7> Create(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2, T3, T4, T5, T6, T7>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job._t5 = t5;
            job._t6 = t6;
            job._t7 = t7;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2, _t3, _t4, _t5, _t6, _t7);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            _t5 = default!;
            _t6 = default!;
            _t7 = default!;
            Pool.Enqueue(this);
        }
    }

    public class Job<T1, T2, T3, T4, T5, T6, T7, T8> : JobBase
    {
        private static readonly ConcurrentQueue<Job<T1, T2, T3, T4, T5, T6, T7, T8>> Pool = new();
        private Job() { }
        private Action<T1, T2, T3, T4, T5, T6, T7, T8> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;
        private T5 _t5 = default!;
        private T6 _t6 = default!;
        private T7 _t7 = default!;
        private T8 _t8 = default!;

        public static Job<T1, T2, T3, T4, T5, T6, T7, T8> Create(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, JobPriority priority)
        {
            if (!Pool.TryDequeue(out var job)) job = new Job<T1, T2, T3, T4, T5, T6, T7, T8>();
            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job._t5 = t5;
            job._t6 = t6;
            job._t7 = t7;
            job._t8 = t8;
            job.Priority = priority;
            job.Cancel = false;
            return job;
        }
        public override void Execute()
        {
            if (!Cancel) _action.Invoke(_t1, _t2, _t3, _t4, _t5, _t6, _t7, _t8);
            _action = null!;
            _t1 = default!;
            _t2 = default!;
            _t3 = default!;
            _t4 = default!;
            _t5 = default!;
            _t6 = default!;
            _t7 = default!;
            _t8 = default!;
            Pool.Enqueue(this);
        }
    }
}