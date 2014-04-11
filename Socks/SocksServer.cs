using System;
using System.Net;
using System.Net.Sockets;

namespace ProxyKit.Socks
{
    public class SocksServer : ProxyServer
    {
        public AuthCallbackHandler AuthCallback { get; set; }
        public SocksServer(IPAddress ip, int port, 
            AuthCallbackHandler callback) 
            : base(ip, port)
        {
            AuthCallback = callback;
        }

        public SocksServer(int port) 
            : this(IPAddress.Any, port, null)
        {
        }

        protected override void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket localSocket = _listenerSocket.EndAccept(ar);
                if (localSocket != null)
                {
                    SocksClient client = new SocksClient(localSocket, 
                        Subscribe, 
                        Remove,
                        AuthCallback);
                    client.StartHandshake();
                }
            }
            catch{ }

            try
            {
                _listenerSocket.BeginAccept(AcceptCallback, null);
            }
            catch
            {
                Dispose();
            }
        }

        private void Subscribe(ProxyClient client)
        {
            base.OnConnect(new ConnectEventArgs(client));
        }
    }
}
