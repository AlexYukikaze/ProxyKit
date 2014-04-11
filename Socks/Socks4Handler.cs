using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyKit.Socks
{
    internal sealed class Socks4Handler : SocksHandler
    {
        private IPEndPoint _endPoint;

        public IPEndPoint EndPoint { get { return _endPoint; } }
        
        public Socks4Handler(Socket localSocket, HandshakeCallback callback) : base(localSocket, callback)
        {
        }

        protected override void ProcessRequest(NetworkStream stream)
        {
            try
            {
                BinaryReader reader = new BinaryReader(stream);
                CommandType type = (CommandType)reader.ReadByte();
                if (type != CommandType.CONNECT && type != CommandType.BIND)
                {
                    Complete(false);
                    return;
                }

                switch (type)
                {
                    case CommandType.CONNECT:
                    {
                        int port = ReaderUtils.ReadInt16BE(reader);
                        byte[] host = reader.ReadBytes(4);
                        _username = ReadString(reader);
                        _endPoint = new IPEndPoint(new IPAddress(host), port);

                        _remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _remoteSocket.Connect(_endPoint);
                        if (_remoteSocket.Connected)
                        {
                            Complete(0x5a);
                            return;
                        }
                        Complete(0x5b);
                        break;
                    }
                    case CommandType.BIND: //BUG: Potential bug
                    {
                        byte[] bytes = reader.ReadBytes(6);
                        int port = bytes[1] * 256 + bytes[0];
                        string host = bytes[2] + "." + bytes[3] + "." + bytes[4] + "." + bytes[5];
                        _bindEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

                        _acceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _acceptSocket.Bind(new IPEndPoint(0, 0));
                        _acceptSocket.Listen(10);
                        
                        Complete(0x5a);
                        _acceptSocket.BeginAccept(OnAccept, null);
                        break;
                    }
                }
            }
            catch
            {
                Complete(0x5b);
            }
        }

        protected override void OnAccept(IAsyncResult ar)
        {
            try
            {
                _remoteSocket = _acceptSocket.EndAccept(ar);
                var remoteAddress = ((IPEndPoint)_remoteSocket.RemoteEndPoint).Address;
                if (remoteAddress.Equals(_bindEndPoint.Address))
                {
                    Complete(0x5a);
                    return;
                }
                Complete(0x5b);
            }
            catch
            {
                Complete(0x5b);
            }
            finally
            {
                if(_acceptSocket != null)
                    _acceptSocket.Close();
                _acceptSocket = null;
            }
        }

        protected override void Complete(byte value)
        {
            try
            {
                var result = new byte[] { 0, value, 0, 0, 0, 0, 0, 0 };
                int sent = _localSocket.Send(result);
                if (value == 0x5a && sent > 0)
                {
                    Complete(true);
                    return;
                }
                Complete(false);
            }
            catch
            {
                Complete(false);
            }
        }

        private string ReadString(BinaryReader reader)
        {
            byte[] stringBytes = new byte[256];
            int len = 0;
            for (int i = 0; i < 256; i++)
            {
                len += reader.Read(stringBytes, len, 1);
                if(stringBytes[len - 1] == 0)
                    break;
            }

            return Encoding.UTF8.GetString(stringBytes, 0, len - 1);
        }

        private enum CommandType : byte
        {
            CONNECT = 1,
            BIND    = 2,
        }
    }
}
