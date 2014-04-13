using System.Net.Sockets;

namespace ProxyKit.Socks.Auth
{
    internal abstract class AuthMethod
    {
        internal abstract bool Auth(NetworkStream stream);
    }
}