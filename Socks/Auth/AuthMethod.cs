using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ProxyKit.Socks.Auth
{
    internal abstract class AuthMethod
    {
        protected AuthMethod()
        {
        }

        internal abstract bool Auth(NetworkStream stream);
    }
}
