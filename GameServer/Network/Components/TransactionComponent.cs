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

        public bool TrySetBusy()
        {
            var oldState = Interlocked.CompareExchange(ref _transactionState, (int)TransactionState.Busy, (int)TransactionState.Idle);
            return oldState == (int)TransactionState.Idle;
        }
        public void ReleaseState()
        {
            DisposeContext();

            var oldState = Interlocked.CompareExchange(ref _transactionState, (int)TransactionState.Idle, (int)TransactionState.Busy);
            
            if (_session.Disconnected) _logout.Invoke(_session);
        }
        public void FailedWithLastMessage(string msg, ArraySegment<byte> buffer)
        {
            if (!TryFailed()) return;
            _session.DisconnectWithLastMessage(msg, buffer);
        }
        public void Failed(string msg)
        {
            if (!TryFailed()) return;
            _session.Disconnect(msg);
        }
        private bool TryFailed()
        {
            if (Interlocked.CompareExchange(ref _transactionState, (int)TransactionState.Failed, (int)TransactionState.Busy) == (int)TransactionState.Busy)
            {
                DisposeContext();
                // Busy상태일때 네트워크 끊기면 OnDisconnected => _logout 실패, 그리고 이 함수이후 Disconnect에선 OnDisconnected =>_logout 호출 안됨
                // 네트워크가 살아있을때 호출되면 여기서 1번, 외부 Disconnect로 1번 총 2번 _logout 호출됨
                // 그러나 TryLogout 체크가 있으니 실제론 1번만 통과함
                // if (_session.Disconnected) 체크하면 아마도 중복 _logout 호출 빈도를 줄이기는 될거같음
                // 그러나 라스트메시지 구현과 리소스 회수 시점 차이가 생김
                _logout.Invoke(_session);
                return true;
            }
#if DEBUG
            Environment.FailFast($"[Transaction Error] Failed() called but state is not Busy. Current: {(TransactionState)_transactionState}");
#endif
            return false;
        }
        public bool TrySetLogout()
        {
            var wait = new SpinWait();
            while (true)
            {
                var snapshot = _transactionState;
                switch (snapshot)
                {
                    case (int)TransactionState.Idle:
                    case (int)TransactionState.Failed:
                    {
                        var snapshot2 = Interlocked.CompareExchange(ref _transactionState, (int)TransactionState.Logout, snapshot);
                        if (snapshot == snapshot2) return true; // 현 스레드 승
                        break;
                    }
                    // 패배 분기
                    case (int)TransactionState.Logout:
                        return false;
                    // 트랜잭션 직후 Kick 명령 또는 네트워크 연결끊김(OnDisconnected)으로 발생가능 시나리오
                    case (int)TransactionState.Busy:
                        return false;
                }
                // 1. Idle -> Failed 로 변경 되었다면?
                // Failed 내 _logout 호출로 아마 다음번에 중복호출 분기로 빠져나가서 다시 경합 할것임
                // 2. Idle -> Busy 로 변경 되었다면?
                // 일단 진행시키고 마지막 Release 나 Failed에게 짬때리고 다음 반복때 false
                wait.SpinOnce();
            }
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
