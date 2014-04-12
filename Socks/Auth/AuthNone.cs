using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

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
