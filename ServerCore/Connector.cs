using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class Connector
    {
        private Func<Session> _sessionFactory = null!;
        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int connectCount)
        {
            
            for (var i = 0; i < connectCount; i++)
            {
                var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _sessionFactory = sessionFactory;

                var args = new SocketAsyncEventArgs();
                args.Completed += OnConnectCompleted;
                args.RemoteEndPoint = endPoint;
                args.UserToken = socket;

                RegisterConnect(args);
            }
        }

        private void RegisterConnect(SocketAsyncEventArgs args)
        {
            if (args.UserToken is not Socket socket) return;

            var pending = socket.ConnectAsync(args);
            if (!pending) OnConnectCompleted(null, args);
        }

        private void OnConnectCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                var session = _sessionFactory.Invoke();
                session.Start(args.ConnectSocket!);
            }
            else
            {
                Log.Error(this, "OnConnectCompleted failed: {SocketError}", args.SocketError);

                RegisterConnect(args);
            }
        }
    }
}
