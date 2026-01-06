using System;

namespace ServerCore.Job
{
    public sealed class JobSerializedRef<TData, TThread> where TThread : JobSerializer
    {
        private TData _t = default!;
        private bool _hasValue;
        private readonly object _lock = new();

        public void Attach(TData t)
        {
#if DEBUG
            if (JobSerializer.Current is not TThread)
            {
                var current = JobSerializer.Current?.GetType().Name ?? "Unknown Thread";
                Environment.FailFast($"[RoutingComponent] Attach {typeof(TData)} violation: Wrong Thread ({current})");
            }
#endif
            lock (_lock)
            {
#if DEBUG
                if (_hasValue)
                {
                    Environment.FailFast($"[RoutingComponent] {typeof(TData)} collision! Already bound. Missing Detach call?");
                }
#endif
                _t = t;
                _hasValue = true;
            }
        }
        public void Detach()
        {
#if DEBUG
            if (JobSerializer.Current is not TThread)
            {
                var current = JobSerializer.Current?.GetType().Name ?? "External Thread";
                Environment.FailFast($"[RoutingComponent] Detach {typeof(TData)} violation: Wrong Thread ({current})");
            }
#endif
            lock (_lock)
            {
#if DEBUG
                if (!_hasValue)
                {
                    Environment.FailFast($"[RoutingComponent] {typeof(TData)} is already null! Double-Free detected.");
                }
#endif
                _t = default!;
                _hasValue = false;
            }

        }
        public bool TryCapture(out TData t)
        {
            lock (_lock)
            {
                t = _t;
                return _hasValue;
            }
        }
    }

    public sealed class DataRef<TData, TThread> where TThread : JobSerializer
    {
        private TData _t;

        public DataRef(TData t)
        {
            _t = t;
        }
        public TData Data
        {
            get
            {
#if DEBUG
                if (JobSerializer.Current is not TThread)
                {
                    var current = JobSerializer.Current?.GetType().Name ?? "External Thread";
                    Environment.FailFast($"[RoutingComponent] Set {typeof(TData)} violation: Wrong Thread ({current})");
                }
#endif
                return _t;
            }
            set
            {
#if DEBUG
                if (JobSerializer.Current is not TThread)
                {
                    var current = JobSerializer.Current?.GetType().Name ?? "External Thread";
                    Environment.FailFast($"[RoutingComponent] Get {typeof(TData)} violation: Wrong Thread ({current})");
                }
#endif
                _t = value;
            }
        }
    }
}