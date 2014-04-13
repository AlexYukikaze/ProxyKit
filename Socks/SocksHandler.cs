using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProxyKit.Socks
{
    internal delegate void HandshakeCallback(bool success, Socket remote);

    internal abstract class SocksHandler
    {
        private readonly HandshakeCallback _handshakeCallback;
        protected Socket AcceptSocket;
        protected IPEndPoint BindEndPoint;
        protected byte[] Buffer;
        protected IPEndPoint EndPoint;
        protected Socket LocalSocket;
        protected Socket RemoteSocket;
        protected string Username;

        protected SocksHandler(Socket localSocket, HandshakeCallback callback)
        {
            if(callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            LocalSocket = localSocket;
            _handshakeCallback = callback;
            Buffer = new byte[0x100];
        }

        protected abstract void OnAccept(IAsyncResult ar);
        protected abstract void ProcessRequest(NetworkStream stream);
        protected abstract void SendRespoce(byte value);

        protected void Dispoce(bool success)
        {
            if(AcceptSocket != null)
            {
                AcceptSocket.Close();
            }
            _handshakeCallback(success, RemoteSocket);
        }

        public void BeginRequestData()
        {
            new Thread(RequestThread).Start();
        }

        private void RequestThread()
        {
            try
            {
                var stream = new NetworkStream(LocalSocket, false);
                ProcessRequest(stream);
            }
            catch(Exception)
            {
                Dispoce(false);
            }
        }
    }
}