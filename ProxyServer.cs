using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ProxyKit
{
    public abstract class ProxyServer : IDisposable
    {
        private readonly List<ProxyClient> _clients;
        protected readonly EndPoint _endPoint;
        protected Socket _listenerSocket;
        private bool _isDisposed;

        public event EventHandler<ConnectEventArgs> Connect;

        public IPEndPoint EndPoint { get { return (IPEndPoint)_endPoint; } }
        public bool Disposed { get { return _isDisposed; } }
        public bool Started { get { return _listenerSocket != null; } }

        protected ProxyServer(IPAddress ip, int port)
        {
            _endPoint = new IPEndPoint(ip, port);
            _clients = new List<ProxyClient>();
            _isDisposed = false;
        }

        protected ProxyServer(int port) 
            : this(IPAddress.Any, port)
        {
        }

        protected abstract void AcceptCallback(IAsyncResult ar);

        protected void Add(ProxyClient client)
        {
            if(!_clients.Contains(client))
                _clients.Add(client);
        }

        protected void Remove(ProxyClient client)
        {
            if (_clients.Contains(client))
                _clients.Remove(client);
        }

        protected void ConnectCallback(ProxyClient client)
        {
            OnConnect(new ConnectEventArgs(client));
        }

        public void Start()
        {
            try
            {
                _listenerSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(_endPoint);
                _listenerSocket.Listen(10);
                _listenerSocket.BeginAccept(AcceptCallback, _listenerSocket);
            }
            catch
            {
                Dispose();
                throw new SocketException();
            }
        }

        public void Dispose()
        {
            if(_isDisposed) return;

            while (_clients.Count > 0)
            {
                _clients[0].Dispose();
            }

            try
            {
                _listenerSocket.Shutdown(SocketShutdown.Both);
            }
            catch{ }

            if(_listenerSocket != null)
                _listenerSocket.Close();
            _listenerSocket = null;

            _isDisposed = true;
        }

        protected virtual void OnConnect(ConnectEventArgs e)
        {
            EventHandler<ConnectEventArgs> handler = Connect;
            if (handler != null) handler(this, e);
        }
    }
}
