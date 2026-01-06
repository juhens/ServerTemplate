using System;

namespace ServerCore.Job
{
    public struct WriteOnce<T>
    {
        public WriteOnce(){}

        private T _value = default!;
        private bool _hasValue = false;
        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    Environment.FailFast($"{typeof(T)} has not been initialized.");
                }
                return _value;
            }
            set
            {
                if (_hasValue)
                {
                    Environment.FailFast($"{typeof(T)} can only be set once.");
                }

                _value = value;
                _hasValue = true;
            }
        }
    }
}
