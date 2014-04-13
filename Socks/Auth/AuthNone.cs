using System.Net.Sockets;

namespace ProxyKit.Socks.Auth
{
    internal sealed class AuthNone : AuthMethod
    {
        internal override bool Auth(NetworkStream stream)
        {
            return true;
        }
    }
}