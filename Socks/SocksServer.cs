using System;
using System.Net;
using System.Net.Sockets;

namespace ProxyKit.Socks
{
    public class SocksServer : ProxyServer
    {
        public SocksServer(IPAddress ip, int port, AuthCallbackHandler callback)
            : base(ip, port)
        {
            AuthCallback = callback;
        }

        public SocksServer(int port)
            : this(IPAddress.Any, port, null)
        {}

        public SocksServer(int port, AuthCallbackHandler callback)
            : this(IPAddress.Any, port, callback)
        {}

        public AuthCallbackHandler AuthCallback { get; set; }

        protected override void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket localSocket = ListenerSocket.EndAccept(ar);
                if(localSocket != null)
                {
                    var client = new SocksClient(localSocket, Subscribe, Remove, AuthCallback);
                    client.StartHandshake();
                }
            }
            catch
            {}
            try
            {
                ListenerSocket.BeginAccept(AcceptCallback, null);
            }
            catch
            {
                Dispose();
            }
        }

        private void Subscribe(ProxyClient client)
        {
            OnConnect(new ConnectEventArgs(client));
        }
    }
}