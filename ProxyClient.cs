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

        protected readonly byte[] LocalBuffer;
        protected readonly byte[] RemoteBuffer;
        private readonly SelfDestructHandler _selfDestruct;
        private readonly SubscribeHandler _subscribe;
        protected Socket LocalSocket;
        protected Socket RemoteSocket;

        protected ProxyClient(Socket localSocket, SubscribeHandler subscribe, SelfDestructHandler selfDestruct)
        {
            LocalSocket = localSocket;
            _selfDestruct = selfDestruct;
            _subscribe = subscribe;
            LocalBuffer = new byte[BUFFER_SIZE];
            RemoteBuffer = new byte[BUFFER_SIZE];
        }

        public event EventHandler<ClientReceiveEventArgs> ReceiveFromClient;
        public event EventHandler<ClientReceiveEventArgs> ReceiveFromServer;

        #region Public Properties

        public IPEndPoint ClientEndPoint
        {
            get
            {
                return (IPEndPoint)LocalSocket.RemoteEndPoint;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return (IPEndPoint)RemoteSocket.RemoteEndPoint;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            try
            {
                LocalSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {}
            try
            {
                RemoteSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {}
            if(LocalSocket != null)
            {
                LocalSocket.Close();
            }
            if(RemoteSocket != null)
            {
                RemoteSocket.Close();
            }
            LocalSocket = null;
            RemoteSocket = null;
            if(_selfDestruct != null)
            {
                _selfDestruct(this);
            }
        }

        public void SendToClient(byte[] data, int offset, int count)
        {
            if(data == null)
            {
                throw new ArgumentNullException("data");
            }
            if(offset > data.Length)
            {
                throw new ArgumentException("Offset can't be greater than total length");
            }
            if(count > data.Length)
            {
                throw new ArgumentException("Count can't be greater than buffer length");
            }
            if(offset + count > data.Length)
            {
                throw new ArgumentException("Offset + count can't be greater than total length");
            }
            LocalSocket.BeginSend(data, offset, count, SocketFlags.None, LocalSendCallback, LocalSocket);
        }

        public void SendToClient(byte[] data)
        {
            SendToClient(data, 0, data.Length);
        }

        public void SendToServer(byte[] data, int offset, int count)
        {
            if(data == null)
            {
                throw new ArgumentNullException("data");
            }
            if(offset > data.Length)
            {
                throw new ArgumentException("Offset can't be greater than total length");
            }
            if(count > data.Length)
            {
                throw new ArgumentException("Count can't be greater than buffer length");
            }
            if(offset + count > data.Length)
            {
                throw new ArgumentException("Offset + count can't be greater than total length");
            }
            RemoteSocket.BeginSend(data, offset, count, SocketFlags.None, RemoteSendCallback, RemoteSocket);
        }

        public void SendToServer(byte[] data)
        {
            SendToServer(data, 0, data.Length);
        }

        #endregion

        #region Methods

        internal abstract void StartHandshake();

        protected void BeginExchange()
        {
            try
            {
                LocalSocket.BeginReceive(
                                         LocalBuffer, 
                    0, 
                    LocalBuffer.Length, 
                    SocketFlags.None, 
                    LocalReceiveCallback, 
                    null);
                RemoteSocket.BeginReceive(
                                          RemoteBuffer, 
                    0, 
                    RemoteBuffer.Length, 
                    SocketFlags.None, 
                    RemoteReceiveCallback, 
                    null);
            }
            catch
            {
                Dispose();
            }
        }

        protected void LocalReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int received = LocalSocket.EndReceive(ar);
                if(received == 0)
                {
                    Dispose();
                    return;
                }
                var args = new ClientReceiveEventArgs(LocalBuffer, received);
                OnFromClient(args);
                if(args.Cancel)
                {
                    LocalSocket.BeginReceive(
                                             LocalBuffer, 
                        0, 
                        LocalBuffer.Length, 
                        SocketFlags.None, 
                        LocalReceiveCallback, 
                        null);
                    return;
                }
                RemoteSocket.BeginSend(args.Data, 0, args.Count, SocketFlags.None, RemoteSendCallback, null);
            }
            catch
            {
                Dispose();
            }
        }

        protected void LocalSendCallback(IAsyncResult ar)
        {
            try
            {
                int sent = LocalSocket.EndSend(ar);
                if(sent > 0)
                {
                    RemoteSocket.BeginReceive(
                                              RemoteBuffer, 
                        0, 
                        RemoteBuffer.Length, 
                        SocketFlags.None, 
                        RemoteReceiveCallback, 
                        null);
                    return;
                }
            }
            catch
            {}
            Dispose();
        }

        protected void OnConnect()
        {
            SubscribeHandler handler = _subscribe;
            if(handler != null)
            {
                handler(this);
            }
        }

        protected virtual void OnFromClient(ClientReceiveEventArgs e)
        {
            EventHandler<ClientReceiveEventArgs> handler = ReceiveFromClient;
            if(handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnFromServer(ClientReceiveEventArgs e)
        {
            EventHandler<ClientReceiveEventArgs> handler = ReceiveFromServer;
            if(handler != null)
            {
                handler(this, e);
            }
        }

        protected void RemoteReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int received = RemoteSocket.EndReceive(ar);
                if(received == 0)
                {
                    Dispose();
                    return;
                }
                var args = new ClientReceiveEventArgs(RemoteBuffer, received);
                OnFromClient(args);
                if(args.Cancel)
                {
                    RemoteSocket.BeginReceive(
                                              RemoteBuffer, 
                        0, 
                        RemoteBuffer.Length, 
                        SocketFlags.None, 
                        RemoteReceiveCallback, 
                        null);
                    return;
                }
                LocalSocket.BeginSend(RemoteBuffer, 0, RemoteBuffer.Length, SocketFlags.None, LocalSendCallback, null);
            }
            catch
            {
                Dispose();
            }
        }

        protected void RemoteSendCallback(IAsyncResult ar)
        {
            try
            {
                int sent = RemoteSocket.EndSend(ar);
                if(sent > 0)
                {
                    LocalSocket.BeginReceive(
                                             LocalBuffer, 
                        0, 
                        LocalBuffer.Length, 
                        SocketFlags.None, 
                        LocalReceiveCallback, 
                        null);
                    return;
                }
            }
            catch
            {}
            Dispose();
        }

        #endregion
    }
}