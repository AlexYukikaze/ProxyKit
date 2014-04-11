using System;
using System.Net.Sockets;

namespace ProxyKit
{
    public delegate void SelfDestructHandler(ProxyClient client);
    public delegate void SubscribeHandler(ProxyClient client);
    
    public abstract class ProxyClient : IDisposable
    {
        private const int BUFFER_SIZE = 0x1000;
        protected Socket _localSocket, _remoteSocket;
        protected readonly byte[] _localBuffer, _remoteBuffer;
        private readonly SelfDestructHandler _selfDestruct;
        private readonly SubscribeHandler _subscribe;

        public event EventHandler<ClientReceiveEventArgs> ReceiveClient;
        public event EventHandler<ClientReceiveEventArgs> ReceiveServer;

        protected ProxyClient(Socket localSocket, 
            SubscribeHandler subscribe, 
            SelfDestructHandler selfDestruct)
        {
            _localSocket  = localSocket;
            _selfDestruct = selfDestruct;
            _subscribe = subscribe;

            _localBuffer  = new byte[BUFFER_SIZE];
            _remoteBuffer = new byte[BUFFER_SIZE];
        }

        public abstract void StartHandshake();

        protected void BeginExchange()
        {
            try
            {
                _localSocket.BeginReceive(_localBuffer, 0,
                _localBuffer.Length,
                SocketFlags.None,
                localReceiveCallback,
                null);

                _remoteSocket.BeginReceive(_remoteBuffer, 0,
                    _remoteBuffer.Length,
                    SocketFlags.None,
                    remoteReceiveCallback,
                    null);
            }
            catch
            {
                Dispose();
            }
        }

        #region Socket callbacks
        protected void localReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int received = _localSocket.EndReceive(ar);
                if (received == 0)
                {
                    Dispose();
                    return;
                }

                //TODO: add event invocator

                _remoteSocket.BeginSend(_localBuffer, 0,
                    _localBuffer.Length,
                    SocketFlags.None,
                    remoteSendCallback,
                    null);
            }
            catch
            {
                Dispose();
            }
        }

        protected void remoteSendCallback(IAsyncResult ar)
        {
            try
            {
                int sent = _remoteSocket.EndSend(ar);
                if (sent > 0)
                {
                    _localSocket.BeginReceive(_localBuffer, 0,
                        _localBuffer.Length,
                        SocketFlags.None,
                        localReceiveCallback,
                        null);
                    return;
                }
            }
            catch { }
            Dispose();
        }

        protected void remoteReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int received = _remoteSocket.EndReceive(ar);
                if (received == 0)
                {
                    Dispose();
                    return;
                }

                //TODO: add event invocator

                _localSocket.BeginSend(_remoteBuffer, 0,
                    _remoteBuffer.Length,
                    SocketFlags.None,
                    localSendCallback,
                    null);
            }
            catch
            {
                Dispose();
            }
        }

        protected void localSendCallback(IAsyncResult ar)
        {
            try
            {
                int sent = _localSocket.EndSend(ar);
                if (sent > 0)
                {
                    _remoteSocket.BeginReceive(_remoteBuffer, 0,
                        _remoteBuffer.Length,
                        SocketFlags.None,
                        remoteReceiveCallback,
                        null);
                    return;
                }
            }
            catch { }
            Dispose();
        }

        #endregion
        
        public void Dispose()
        {
            try
            {
                _localSocket.Shutdown(SocketShutdown.Both);
            }
            catch{ }
            try
            {
                _remoteSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }

            if (_localSocket != null)
                _localSocket.Close();
            if (_remoteSocket != null)
                _remoteSocket.Close();

            _localSocket = null;
            _remoteSocket = null;
            if (_selfDestruct != null)
                _selfDestruct(this);
        }

        protected void OnConnect()
        {
            var handler = _subscribe;
            if (handler != null) handler(this);
        }

        protected virtual void OnReceiveClient(ClientReceiveEventArgs e)
        {
            EventHandler<ClientReceiveEventArgs> handler = ReceiveClient;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnReceiveServer(ClientReceiveEventArgs e)
        {
            EventHandler<ClientReceiveEventArgs> handler = ReceiveServer;
            if (handler != null) handler(this, e);
        }
    }
}
