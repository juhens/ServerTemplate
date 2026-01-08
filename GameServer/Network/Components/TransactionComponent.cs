using GameServer.Game.Commands.Transaction.Contexts;
using GameServer.Game.Commands.Transaction.Contexts.Interfaces;

namespace GameServer.Network.Components
{
    public class TransactionComponent
    {
        public TransactionComponent(ClientSession session, Action<ClientSession> logout)
        {
            _session = session;
            _logout = logout;
        }

        private readonly ClientSession _session;
        private readonly Action<ClientSession> _logout;
        private volatile int _transactionState = (int)TransactionState.Idle;

        public bool TrySetState(TransactionState newState)
        {
            switch (newState)
            {
                case TransactionState.Idle:
#if DEBUG
                    Environment.FailFast("[TransactionState Error] Cannot set TransactionState to Idle directly. Use ReleaseState().");
#endif
                    break;
                case TransactionState.Logout:
                
                    if (Interlocked.CompareExchange(ref _transactionState, (int)newState, (int)TransactionState.Idle) == (int)TransactionState.Idle)
                        return true;

                    if (Interlocked.CompareExchange(ref _transactionState, (int)newState, (int)TransactionState.Failed) == (int)TransactionState.Failed)
                        return true;
#if DEBUG
                    if (_transactionState == (int)TransactionState.Logout)
                    {
                        Environment.FailFast($"[Transaction Error] TrySetState(Logout) called but state is already Logout.");
                    }
#endif
                    break;
                
                case TransactionState.Failed:
                {
                    if (Interlocked.CompareExchange(ref _transactionState, (int)TransactionState.Failed, (int)TransactionState.Busy) == (int)TransactionState.Busy)
                        return true;
#if DEBUG
                    Environment.FailFast($"[Transaction Error] Failed() called but state is not Busy. Current: {(TransactionState)_transactionState}");
#endif
                    break;
                }
                case TransactionState.Busy:
                {
                    if (Interlocked.CompareExchange(ref _transactionState, (int)newState, (int)TransactionState.Idle) == (int)TransactionState.Idle)
                        return true;
                    break;
                }
                default:
#if DEBUG
                    Environment.FailFast($"[Transaction Error] bad {newState} Current: {(TransactionState)_transactionState}");
#endif
                    break;
            }

            return false;
        }
        public void ReleaseState()
        {
            DisposeContext();
            var oldState = Interlocked.Exchange(ref _transactionState, (int)TransactionState.Idle);
#if DEBUG
            if (oldState == (int)TransactionState.Idle)
            {
                Environment.FailFast($"[Logic Error] ReleaseState called while State is already Idle. (RuntimeId: {_session.RuntimeId})");
            }
            else if (oldState == (int)TransactionState.Failed)
            {
                Environment.FailFast($"[Logic Error] ReleaseState called while State is Failed. (RuntimeId: {_session.RuntimeId})");
            }
            else if (oldState == (int)TransactionState.Logout)
            {
                Environment.FailFast($"[Logic Error] ReleaseState called while State is already Logout. (RuntimeId: {_session.RuntimeId})");
            }
#endif
            if (_session.Disconnected) _logout.Invoke(_session);
        }
        public void FailedWithLastMessage(string msg, ArraySegment<byte> buffer)
        {
            if (!TrySetState(TransactionState.Failed)) return;

            DisposeContext();

            if (_session.Disconnected)
            {
                _logout.Invoke(_session);
                return;
            }
            _session.DisconnectWithLastMessage(msg, buffer);
        }
        public void Failed(string msg)
        {
            if (!TrySetState(TransactionState.Failed)) return;

            DisposeContext();

            if (_session.Disconnected)
            {
                _logout.Invoke(_session);
                return;
            }
            _session.Disconnect(msg);
        }
        public void OnDisconnected()
        {
            _logout.Invoke(_session);
        }


        private Action? _disposeContext;
        public T CreateContext<T>() where T : BaseContext<T>, new()
        {
            var ctx = BaseContext<T>.Create();
            _disposeContext = ctx.Dispose;
            return ctx;
        }
        private void DisposeContext()
        {
            _disposeContext?.Invoke();
            _disposeContext = null;
        }
    }
}
