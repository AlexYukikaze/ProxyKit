using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ProxyKit
{
    public abstract class ProxyServer : IDisposable
    {
        private readonly List<ProxyClient> _clients;

        protected readonly EndPoint endPoint;
        protected Socket ListenerSocket;
        private bool _isDisposed;

        protected ProxyServer(IPAddress ip, int port)
        {
            endPoint = new IPEndPoint(ip, port);
            _clients = new List<ProxyClient>();
            _isDisposed = false;
        }

        protected ProxyServer(int port)
            : this(IPAddress.Any, port)
        {}

        public IPEndPoint EndPoint
        {
            get
            {
                return (IPEndPoint)endPoint;
            }
        }

        public bool Disposed
        {
            get
            {
                return _isDisposed;
            }
        }

        public bool Started
        {
            get
            {
                return ListenerSocket != null;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if(_isDisposed)
            {
                return;
            }
            while(_clients.Count > 0)
            {
                _clients[0].Dispose();
            }
            try
            {
                ListenerSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {}
            if(ListenerSocket != null)
            {
                ListenerSocket.Close();
            }
            ListenerSocket = null;
            _isDisposed = true;
        }

        #endregion

        public event EventHandler<ConnectEventArgs> Connect;

        protected abstract void AcceptCallback(IAsyncResult ar);

        protected void Add(ProxyClient client)
        {
            if(!_clients.Contains(client))
            {
                _clients.Add(client);
            }
        }

        protected void Remove(ProxyClient client)
        {
            if(_clients.Contains(client))
            {
                _clients.Remove(client);
            }
        }

        protected void ConnectCallback(ProxyClient client)
        {
            OnConnect(new ConnectEventArgs(client));
        }

        public void Start()
        {
            try
            {
                ListenerSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ListenerSocket.Bind(endPoint);
                ListenerSocket.Listen(10);
                ListenerSocket.BeginAccept(AcceptCallback, ListenerSocket);
            }
            catch
            {
                Dispose();
                throw new SocketException();
            }
        }

        protected virtual void OnConnect(ConnectEventArgs e)
        {
            EventHandler<ConnectEventArgs> handler = Connect;
            if(handler != null)
            {
                handler(this, e);
            }
        }
    }
}