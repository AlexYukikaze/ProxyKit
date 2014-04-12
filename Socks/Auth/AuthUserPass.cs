using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ProxyKit.Socks.Auth
{
    internal sealed class AuthUserPass : AuthMethod
    {
        private readonly AuthCallbackHandler _callback;
        
        public AuthUserPass(AuthCallbackHandler callback)
        {
            _callback = callback;
        }
        
        internal override bool Auth(NetworkStream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);

            try
            {
                int ver = reader.ReadByte();
                if (ver != 0x05)
                    throw new Exception();
                string login = reader.ReadString();
                string password = reader.ReadString();
                bool result = _callback(login, password);

                if (result)
                {
                    writer.Write(new byte[] {0x05, 0x00});
                    return true;
                }
                writer.Write(new byte[] { 0x05, 0xFF });
            }
            catch (IOException){ }
            catch
            {
                writer.Write(new byte[] { 0x05, 0xFF });
            }
            return false;
        }
    }
}
