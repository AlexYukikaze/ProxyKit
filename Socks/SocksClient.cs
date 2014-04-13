using System;
using System.Net.Sockets;

namespace ProxyKit.Socks
{
    public delegate bool AuthCallbackHandler(string login, string password);

    public class SocksClient : ProxyClient
    {
        private readonly AuthCallbackHandler _authCallback;
        private SocksHandler _socksHandler;

        public SocksClient(
            Socket localSocket, 
            SubscribeHandler subscribe, 
            SelfDestructHandler selfDestruct, 
            AuthCallbackHandler authCallback)
            : base(localSocket, subscribe, selfDestruct)
        {
            _authCallback = authCallback;
        }

        internal override void StartHandshake()
        {
            try
            {
                LocalSocket.BeginReceive(LocalBuffer, 0, 1, SocketFlags.None, HandshakeCallback, null);
            }
            catch
            {
                Dispose();
            }
        }

        private void HandshakeCallback(IAsyncResult ar)
        {
            try
            {
                int received = LocalSocket.EndReceive(ar);
                if(received == 0)
                {
                    Dispose();
                    return;
                }
                switch(LocalBuffer[0])
                {
                    case 4: // SOCKS 4
                        _socksHandler = new Socks4Handler(LocalSocket, HandshakeEnd);
                        break;
                    case 5: // SOCKS 5
                        _socksHandler = new Socks5Handler(LocalSocket, HandshakeEnd, _authCallback);
                        break;
                    default:
                        Dispose();
                        return;
                }
                _socksHandler.BeginRequestData();
            }
            catch
            {
                Dispose();
            }
        }

        private void HandshakeEnd(bool success, Socket remote)
        {
            RemoteSocket = remote;
            if(success)
            {
                OnConnect();
                BeginExchange();
                return;
            }
            Dispose();
        }
    }
}