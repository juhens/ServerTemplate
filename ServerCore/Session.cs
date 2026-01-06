using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using ServerCore.Packet;
using ServerCore.Cipher;

namespace ServerCore
{
    public abstract class Session : IDisposable
    {
        protected Session(AppSide side)
        {
            CipherSuite = new CipherSuite(side);
        }

        private Socket _socket = null!;
        private EndPoint _endPoint = null!;
        private readonly RecvBuffer _recvBuffer = new();
        private readonly SendBuffer _sendBuffer = new();

        protected bool IsCipherEnabled => CipherSuite.IsRecvEnabled;

        protected readonly CipherSuite CipherSuite;
        private static readonly ArraySegment<byte> CipherFlag = new byte[1];

        private static readonly ArraySegment<byte> PoisonPill = new byte[1];
        private volatile bool _disconnectAfterFlush = false;
        private string _lastMessage = string.Empty;


        public void EnableCipher(long encryptionSeed)
        {
            var key = KeyMaker.CreateKey(encryptionSeed);
            CipherSuite.Init(key);
            CipherSuite.IsRecvEnabled = true;
            _sendQueue.Enqueue(CipherFlag);
            // Send 이후 호출될수 있으니 바로 등록해야함
            RegisterSend();
        }

        private readonly ConcurrentQueue<ArraySegment<byte>> _sendQueue = new();

        private volatile int _disconnected; // 0: 연결, 1: 디스커넥트
        private volatile int _isSending;    // 0: 대기, 1: 전송중
        private volatile int _recvClosed;   // 0: 열림, 1: 닫힘

        private readonly SocketAsyncEventArgs _sendArgs = new();
        private readonly SocketAsyncEventArgs _recvArgs = new();

        protected abstract void OnConnected(EndPoint endPoint);
        protected abstract int OnRecv(ArraySegment<byte> buffer);
        protected abstract void OnSend(int numOfBytes);
        protected abstract void OnDisconnected(EndPoint endPoint, string? msg);

        public bool Disconnected => _disconnected == 1;

        public void Start(Socket socket)
        {
            _socket = socket;
            _endPoint = socket.RemoteEndPoint!;

            _recvArgs.Completed += OnRecvCompleted;
            _sendArgs.Completed += OnSendCompleted;

            OnConnected(_endPoint);
            RegisterRecv();
        }

        public void Disconnect(string msg)
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) return;
            OnDisconnected(_endPoint, msg);
            Dispose();
        }
        public void DisconnectWithLastMessage(string lastMessage, ArraySegment<byte> sendBuff)
        {
            if (_disconnected == 1) return;
            _lastMessage = lastMessage;
            _sendQueue.Enqueue(sendBuff);
            _sendQueue.Enqueue(PoisonPill);
            RegisterSend();
        }


        public void Dispose()
        {
            try
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    // ignored
                }

                _socket.Close();

                if (Interlocked.CompareExchange(ref _isSending, 1, 0) == 0)
                {
                    ClearSendResources();
                }
            }
            catch (Exception e)
            {
                Log.Error(this, "Session Dispose exception: {Exception}", e);
                throw;
            }
        }
        private void ClearRecvResources()
        {
            // 중복 실행 방지 (0->1 이동 시에만 진입)
            if (Interlocked.CompareExchange(ref _recvClosed, 1, 0) != 0) return;

            try
            {
                _recvArgs.Completed -= OnRecvCompleted;
                _recvArgs.Dispose();
                _recvBuffer.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        private void ClearSendResources()
        {
            try
            {
                _sendArgs.Completed -= OnSendCompleted;
                _sendArgs.SetBuffer(null, 0, 0);
                _sendArgs.Dispose();
            }
            catch
            {
                // ignored
            }
            _sendQueue.Clear();
            _sendBuffer.Dispose();
        }


        #region 네트워크 통신
        private void RegisterRecv()
        {
            // 이미 연결이 끊겼다면 자원 정리 후 종료
            if (_disconnected == 1)
            {
                ClearRecvResources();
                return;
            }

            while (true)
            {
                _recvBuffer.Trim();
                var segment = _recvBuffer.WriteSegment;

                try
                {
                    _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

                    var pending = _socket.ReceiveAsync(_recvArgs);

                    // [대기] 비동기 작업 시작됨 -> 콜백에서 처리하므로 리턴
                    if (pending) return;

                    // [완료] 동기 완료되었으나 실패(소켓 닫힘 등)한 경우
                    if (!ProcessReceive(_recvArgs))
                    {
                        ClearRecvResources();
                        return;
                    }

                    // 성공했다면 루프 계속 (while true)
                }
                catch (Exception e)
                {
                    Disconnect($"RegisterRecv:Exception:{e}");
                    // 예외 발생 시 루프가 깨지므로 여기서 자원 정리
                    ClearRecvResources();
                    return;
                }
            }
        }

        private void OnRecvCompleted(object? sender, SocketAsyncEventArgs args)
        {
            // [완료] 비동기 완료 후 처리
            if (ProcessReceive(args))
            {
                // 성공 시 다시 수신 대기
                RegisterRecv();
            }
            else
            {
                // 실패(소켓 닫힘/에러) 시 자원 정리 (여기가 Last Man)
                ClearRecvResources();
            }
        }

        private bool ProcessReceive(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred <= 0)
            {
                Disconnect($"ProcessReceive:BytesTransferred:{args.BytesTransferred}");
                return false;
            }

            if (args.SocketError != SocketError.Success)
            {
                Disconnect($"ProcessReceive:SocketError:{args.SocketError}");
                return false;
            }

            try
            {
                if (!_recvBuffer.OnWrite(args.BytesTransferred))
                {
                    Disconnect($"ProcessReceive:Failed OnWrite");
                    return false;
                }

                var processLen = OnRecv(_recvBuffer.ReadSegment);
                if (processLen < 0 || _recvBuffer.DataSize < processLen)
                {
                    Disconnect($"ProcessReceive:Failed _recvBuffer.OnRecv()");
                    return false;
                }

                if (!_recvBuffer.OnRead(processLen))
                {
                    Disconnect($"ProcessReceive:Failed _recvBuffer.OnRead()");
                    return false;
                }
            }
            catch (Exception e)
            {
                Disconnect($"ProcessReceive:Exception:{e}");
                return false;
            }

            return true;
        }


        public void Send(ArraySegment<byte> sendBuff)
        {
            if (_disconnected == 1) return;

            _sendQueue.Enqueue(sendBuff);
            RegisterSend();
        }

        public void SendBatch(List<(Session srcSession, ArraySegment<byte> segment)> broadcastList)
        {
            if (_disconnected == 1) return;

            foreach (var broadcast in broadcastList)
            {
                _sendQueue.Enqueue(broadcast.segment);
            }
            RegisterSend();
        }

        public void SendFlush()
        {
            RegisterSend();
        }

        private void RegisterSend()
        {
            if (Interlocked.CompareExchange(ref _isSending, 1, 0) != 0) return;

            while (true)
            {
                if (_disconnected == 1)
                {
                    ClearSendResources();
                    return;
                }

                try
                {
                    FillSendBuffer();
                }
                catch (Exception e)
                {
                    Disconnect($"RegisterSend:BufferError:{e.Message}");
                    ClearSendResources();
                    return;
                }

                if (_sendBuffer.IsEmpty)
                {
                    Interlocked.Exchange(ref _isSending, 0);
                    if (_sendQueue.IsEmpty) return;
                    if (Interlocked.CompareExchange(ref _isSending, 1, 0) != 0) return;
                    continue;
                }

                try
                {
                    var segment = _sendBuffer.GetUsedSegment();
                    _sendArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

                    var pending = _socket.SendAsync(_sendArgs);

                    if (pending) return;
                    if (ProcessSendCompletion(_sendArgs)) continue;
                    return;
                }
                catch (Exception e)
                {
                    Disconnect($"RegisterSend:SocketSendException:{e}");
                    ClearSendResources();
                    return;
                }
            }
        }

        private void FillSendBuffer()
        {
            while (_sendQueue.TryDequeue(out var plainBuffer))
            {
                if (plainBuffer.Array! == PoisonPill.Array!)
                {
                    _disconnectAfterFlush = true;
                    break;
                }

                if (!CipherSuite.IsSendEnabled)
                {
                    if (plainBuffer.Array! == CipherFlag.Array!)
                    {
                        CipherSuite.IsSendEnabled = true;
                        continue;
                    }
                    _sendBuffer.Append(plainBuffer);
                }
                else
                {
                    var headerSize = Unsafe.SizeOf<PacketHeader>();
                    var payloadLen = plainBuffer.Count - headerSize;

                    _sendBuffer.Reserve(plainBuffer.Count);
                    _sendBuffer.Append(plainBuffer.AsSpan(0, headerSize));

                    var destCipher = _sendBuffer.Open(payloadLen);
                    var srcPayload = plainBuffer.AsSpan(headerSize, payloadLen);

                    CipherSuite.Encrypt(srcPayload, destCipher);
                    _sendBuffer.Close(payloadLen);
                }
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (!ProcessSendCompletion(args)) return;

            Interlocked.Exchange(ref _isSending, 0);
            RegisterSend();
        }

        private bool ProcessSendCompletion(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Disconnect($"OnSendCompleted:SocketError:{args.SocketError}");
                ClearSendResources();
                return false;
            }

            if (args.BytesTransferred <= 0)
            {
                Disconnect($"OnSendCompleted:BytesTransferred:{args.BytesTransferred}");
                ClearSendResources();
                return false;
            }

            try
            {
                args.SetBuffer(null, 0, 0);
                _sendBuffer.Clear();

                if (_disconnectAfterFlush)
                {
                    Disconnect($"PoisonPill:{_lastMessage}");
                    ClearSendResources();
                    return false;
                }

                OnSend(args.BytesTransferred);
                return true;
            }
            catch (Exception e)
            {
                Disconnect($"OnSendCompleted:OnSend:{e}");
                ClearSendResources();
                return false;
            }
        }
        #endregion
    }
}