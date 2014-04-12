using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyKit.Socks
{
    internal sealed class Socks4Handler : SocksHandler
    {
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
                    Dispoce(false);
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
                            SendRespoce(0x5a);
                            return;
                        }
                        SendRespoce(0x5b);
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
                        
                        SendRespoce(0x5a);
                        _acceptSocket.BeginAccept(OnAccept, null);
                        break;
                    }
                }
            }
            catch
            {
                SendRespoce(0x5b);
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
                    SendRespoce(0x5a);
                    return;
                }
                SendRespoce(0x5b);
            }
            catch
            {
                SendRespoce(0x5b);
            }
            finally
            {
                if(_acceptSocket != null)
                    _acceptSocket.Close();
                _acceptSocket = null;
            }
        }

        protected override void SendRespoce(byte value)
        {
            try
            {
                var result = new byte[] { 0, value, 0, 0, 0, 0, 0, 0 };
                int sent = _localSocket.Send(result);
                if (value == 0x5a && sent > 0)
                {
                    Dispoce(true);
                    return;
                }
                Dispoce(false);
            }
            catch
            {
                Dispoce(false);
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
