using System;

namespace ServerCore
{
    using System.Net.Sockets;
    using System.Net;

    public class Listener
    {
        private Socket _listenSocket = null!;
        private Func<Session> _sessionFactory = null!;

        public void Start(IPEndPoint endPoint, Func<Session> sessionFactory, int register, int backlog)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory = sessionFactory;

            _listenSocket.Bind(endPoint);

            _listenSocket.Listen(backlog);

            for (var i = 0; i < register; i++)
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += OnAcceptCompleted;
                RegisterAccept(args);
            }
        }
        private void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            var pending = _listenSocket.AcceptAsync(args);
            if (!pending) OnAcceptCompleted(null, args);
        }
        private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            try
            {
                if (args.SocketError == SocketError.Success)
                {
                    var session = _sessionFactory.Invoke();
                    session.Start(args.AcceptSocket!);
                }
                else
                {
                    Log.Error(this, "Accept failed: {SocketError}", args.SocketError);
                }
            }
            catch (Exception e)
            {
                Log.Error(this, "Accept exception: {Exception}", e);
            }
            RegisterAccept(args);
        }
    }
}
