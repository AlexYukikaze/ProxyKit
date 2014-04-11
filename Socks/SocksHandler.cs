using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProxyKit.Socks
{
    internal delegate void HandshakeCallback(bool success, Socket remote);
    internal abstract class SocksHandler
    {
        protected Socket _localSocket, _remoteSocket, _acceptSocket;
        protected byte[] _buffer;
        protected string _username;
        protected IPEndPoint _bindEndPoint;
        private readonly HandshakeCallback _handshakeCallback;

        protected SocksHandler(Socket localSocket,
            HandshakeCallback callback)
        {
            if(callback == null)
                throw new ArgumentNullException("callback");

            _localSocket = localSocket;
            _handshakeCallback = callback;
            _buffer = new byte[0x100];
        }

        protected abstract void OnAccept(IAsyncResult ar);
        protected abstract void ProcessRequest(NetworkStream stream);
        protected abstract void Complete(byte value);

        protected void Complete(bool success)
        {
            if(_acceptSocket != null)
                _acceptSocket.Close();
            _handshakeCallback(success, _remoteSocket);
        }

        public void BeginRequestData()
        {
            new Thread(requestThread).Start();
        }

        private void requestThread()
        {
            try
            {
                NetworkStream stream = new NetworkStream(_localSocket, false);
                ProcessRequest(stream);
            }
            catch (Exception)
            {
                Complete(false);
            }
        }
    }
}
