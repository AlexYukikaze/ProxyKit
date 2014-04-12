using System;
using System.Net.Sockets;

namespace ProxyKit.Socks
{
    public delegate bool AuthCallbackHandler(string login, string password);
    public class SocksClient : ProxyClient
    {
        private AuthCallbackHandler _authCallback;
        private SocksHandler _socksHandler;

        public SocksClient(Socket localSocket, 
            SubscribeHandler subscribe, 
            SelfDestructHandler selfDestruct,
            AuthCallbackHandler authCallback) 
            : base(localSocket, subscribe, selfDestruct)
        {
            _authCallback = authCallback;
        }

        public override void StartHandshake()
        {
            try
            {
                _localSocket.BeginReceive(_localBuffer, 0, 1,
                    SocketFlags.None, 
                    HandshakeCallback,
                    null);
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
                int received = _localSocket.EndReceive(ar);
                if (received == 0)
                {
                    Dispose();
                    return;
                }

                switch (_localBuffer[0])
                {
                    case 4: //SOCKS 4
                        _socksHandler = new Socks4Handler(_localSocket, handshakeEnd);
                        break;

                    case 5: //SOCKS 5
                        _socksHandler = new Socks5Handler(_localSocket, handshakeEnd,
                            _authCallback);
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

        private void handshakeEnd(bool success, Socket remote)
        {
            _remoteSocket = remote;
            if (success)
            {
                OnConnect();
                BeginExchange();
                return;
            }
            Dispose();
        }
    }
}
