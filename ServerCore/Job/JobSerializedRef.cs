using System;

namespace ServerCore.Job
{
    public sealed class JobSerializedRef<TData, TThread> where TThread : JobSerializer
    {
        private TData _t = default!;
        private bool _hasValue;
        private readonly object _lock = new();

        public bool TryAttach(TData t)
        {

            if (JobSerializer.Current is not TThread)
            {
#if DEBUG
                var current = JobSerializer.Current?.GetType().Name ?? "Unknown Thread";
                Environment.FailFast($"[RoutingComponent] TryAttach {typeof(TData)} violation: Wrong Thread ({current})");
#endif
                return false;
            }

            lock (_lock)
            {
                if (_hasValue)
                {
#if DEBUG
                    Environment.FailFast($"[RoutingComponent] {typeof(TData)} collision! Already bound. Missing TryDetach call?");
#endif
                    return false;
                }

                _t = t;
                _hasValue = true;
                return true;
            }
        }
        public bool TryDetach()
        {

            if (JobSerializer.Current is not TThread)
            {
#if DEBUG
                var current = JobSerializer.Current?.GetType().Name ?? "External Thread";
                Environment.FailFast($"[RoutingComponent] TryDetach {typeof(TData)} violation: Wrong Thread ({current})");
#endif
                return false;
            }

            lock (_lock)
            {

                if (!_hasValue)
                {
#if DEBUG
                    Environment.FailFast($"[RoutingComponent] {typeof(TData)} is already null! Double-Free detected.");
#endif
                    return false;
                }

                _t = default!;
                _hasValue = false;
                return true;
            }
        }
        public bool TryCapture(out TData t)
        {
            lock (_lock)
            {
                t = _hasValue ? _t : default!;
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