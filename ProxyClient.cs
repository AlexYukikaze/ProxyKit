using System;
using System.Net;
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

        public event EventHandler<ClientReceiveEventArgs> ReceiveFromServer;
        public event EventHandler<ClientReceiveEventArgs> ReceiveFromClient;

        public IPEndPoint ClientEndPoint
        {
            get { return (IPEndPoint)_localSocket.RemoteEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)_remoteSocket.RemoteEndPoint; }
        }

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

        #region Send data
        public void SendToServer(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (offset > data.Length)
                throw new ArgumentException("Offset can't be greater than total length");
            if (count > data.Length)
                throw new ArgumentException("Count can't be greater than buffer length");
            if (offset + count > data.Length)
                throw new ArgumentException("Offset + count can't be greater than total length");

            _remoteSocket.BeginSend(data, offset, count, SocketFlags.None,
                    remoteSendCallback, _remoteSocket);
        }

        public void SendToServer(byte[] data)
        {
            SendToServer(data, 0, data.Length);
        }

        public void SendToClient(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (offset > data.Length)
                throw new ArgumentException("Offset can't be greater than total length");
            if (count > data.Length)
                throw new ArgumentException("Count can't be greater than buffer length");
            if (offset + count > data.Length)
                throw new ArgumentException("Offset + count can't be greater than total length");

            _localSocket.BeginSend(data, offset, count, SocketFlags.None,
                    localSendCallback, _localSocket);
        }

        public void SendToClient(byte[] data)
        {
            SendToClient(data, 0, data.Length);
        }
        #endregion

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

                var args = new ClientReceiveEventArgs(_localBuffer, received);
                OnFromClient(args);

                if (args.Cancel)
                {
                    _localSocket.BeginReceive(_localBuffer, 0,
                        _localBuffer.Length,
                        SocketFlags.None,
                        localReceiveCallback,
                        null);
                    return;
                }

                _remoteSocket.BeginSend(args.Data, 0,
                    args.Count,
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

                var args = new ClientReceiveEventArgs(_remoteBuffer, received);
                OnFromClient(args);

                if (args.Cancel)
                {
                    _remoteSocket.BeginReceive(_remoteBuffer, 0,
                        _remoteBuffer.Length,
                        SocketFlags.None,
                        remoteReceiveCallback,
                        null);
                    return;
                }

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

        protected virtual void OnFromServer(ClientReceiveEventArgs e)
        {
            EventHandler<ClientReceiveEventArgs> handler = ReceiveFromServer;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnFromClient(ClientReceiveEventArgs e)
        {
            EventHandler<ClientReceiveEventArgs> handler = ReceiveFromClient;
            if (handler != null) handler(this, e);
        }
    }
}
