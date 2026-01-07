using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

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
        public JobPriority Priority { get; protected set; }
    }

    public class Job : JobBase
    {
        private static readonly ConcurrentQueue<Job> Pool = new();
        private Job() { }
        private Action _action = null!;

        public static Job Create(Action action, JobPriority priority)
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

    public class JobAsync : JobBase, IValueTaskSource
    {
        private static readonly ConcurrentQueue<JobAsync> Pool = new();
        private ManualResetValueTaskSourceCore<bool> _core; // Dummy result
        private Action _action = null!;

        public static JobAsync Create(Action action, JobPriority priority, out ValueTask valueTask)
        {
            if (!Pool.TryDequeue(out var job)) job = new JobAsync();

            job._action = action;
            job.Priority = priority;
            job.Cancel = false;

            job._core.Reset();
            job._core.RunContinuationsAsynchronously = true;

            valueTask = new ValueTask(job, job._core.Version);
            return job;
        }
        public override void Execute()
        {
            try
            {
                if (Cancel) _core.SetException(new OperationCanceledException());
                else
                {
                    _action.Invoke();
                    _core.SetResult(true);
                }
            }
            catch (Exception ex) { _core.SetException(ex); }
        }

        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally
            {
                _action = null!;
                Pool.Enqueue(this);
            }
        }
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
    public class JobAsync<T1> : JobBase, IValueTaskSource
    {
        private static readonly ConcurrentQueue<JobAsync<T1>> Pool = new();
        private ManualResetValueTaskSourceCore<bool> _core;
        private Action<T1> _action = null!;
        private T1 _t1 = default!;

        public static JobAsync<T1> Create(Action<T1> action, T1 t1, JobPriority priority, out ValueTask valueTask)
        {
            if (!Pool.TryDequeue(out var job)) job = new JobAsync<T1>();

            job._action = action;
            job._t1 = t1;
            job.Priority = priority;
            job.Cancel = false;

            job._core.Reset();
            job._core.RunContinuationsAsynchronously = true;

            valueTask = new ValueTask(job, job._core.Version);
            return job;
        }
        public override void Execute()
        {
            try
            {
                if (Cancel) _core.SetException(new OperationCanceledException());
                else
                {
                    _action.Invoke(_t1);
                    _core.SetResult(true);
                }
            }
            catch (Exception ex) { _core.SetException(ex); }
        }
        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally
            {
                _action = null!;
                _t1 = default!;
                Pool.Enqueue(this);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
    public class JobAsync<T1, T2> : JobBase, IValueTaskSource
    {
        private static readonly ConcurrentQueue<JobAsync<T1, T2>> Pool = new();
        private ManualResetValueTaskSourceCore<bool> _core;
        private Action<T1, T2> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;

        public static JobAsync<T1, T2> Create(Action<T1, T2> action, T1 t1, T2 t2, JobPriority priority, out ValueTask valueTask)
        {
            if (!Pool.TryDequeue(out var job)) job = new JobAsync<T1, T2>();

            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job.Priority = priority;
            job.Cancel = false;

            job._core.Reset();
            job._core.RunContinuationsAsynchronously = true;

            valueTask = new ValueTask(job, job._core.Version);
            return job;
        }
        public override void Execute()
        {
            try
            {
                if (Cancel) _core.SetException(new OperationCanceledException());
                else
                {
                    _action.Invoke(_t1, _t2);
                    _core.SetResult(true);
                }
            }
            catch (Exception ex) { _core.SetException(ex); }
        }
        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally
            {
                _action = null!;
                _t1 = default!;
                _t2 = default!;
                Pool.Enqueue(this);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
    public class JobAsync<T1, T2, T3> : JobBase, IValueTaskSource
    {
        private static readonly ConcurrentQueue<JobAsync<T1, T2, T3>> Pool = new();
        private ManualResetValueTaskSourceCore<bool> _core;
        private Action<T1, T2, T3> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;

        public static JobAsync<T1, T2, T3> Create(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3, JobPriority priority, out ValueTask valueTask)
        {
            if (!Pool.TryDequeue(out var job)) job = new JobAsync<T1, T2, T3>();

            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job.Priority = priority;
            job.Cancel = false;

            job._core.Reset();
            job._core.RunContinuationsAsynchronously = true;

            valueTask = new ValueTask(job, job._core.Version);
            return job;
        }

        public override void Execute()
        {
            try
            {
                if (Cancel) _core.SetException(new OperationCanceledException());
                else
                {
                    _action.Invoke(_t1, _t2, _t3);
                    _core.SetResult(true);
                }
            }
            catch (Exception ex) { _core.SetException(ex); }
        }
        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally
            {
                _action = null!;
                _t1 = default!;
                _t2 = default!;
                _t3 = default!;
                Pool.Enqueue(this);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
    public class JobAsync<T1, T2, T3, T4> : JobBase, IValueTaskSource
    {
        private static readonly ConcurrentQueue<JobAsync<T1, T2, T3, T4>> Pool = new();
        private ManualResetValueTaskSourceCore<bool> _core;
        private Action<T1, T2, T3, T4> _action = null!;
        private T1 _t1 = default!;
        private T2 _t2 = default!;
        private T3 _t3 = default!;
        private T4 _t4 = default!;

        public static JobAsync<T1, T2, T3, T4> Create(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4, JobPriority priority, out ValueTask valueTask)
        {
            if (!Pool.TryDequeue(out var job)) job = new JobAsync<T1, T2, T3, T4>();

            job._action = action;
            job._t1 = t1;
            job._t2 = t2;
            job._t3 = t3;
            job._t4 = t4;
            job.Priority = priority;
            job.Cancel = false;

            job._core.Reset();
            job._core.RunContinuationsAsynchronously = true;

            valueTask = new ValueTask(job, job._core.Version);
            return job;
        }

        public override void Execute()
        {
            try
            {
                if (Cancel) _core.SetException(new OperationCanceledException());
                else
                {
                    _action.Invoke(_t1, _t2, _t3, _t4);
                    _core.SetResult(true);
                }
            }
            catch (Exception ex) { _core.SetException(ex); }
        }
        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally
            {
                _action = null!;
                _t1 = default!;
                _t2 = default!;
                _t3 = default!;
                _t4 = default!;
                Pool.Enqueue(this);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
}