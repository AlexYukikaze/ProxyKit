using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyKit.Socks
{
    internal sealed class Socks4Handler : SocksHandler
    {
        public Socks4Handler(Socket localSocket, HandshakeCallback callback)
            : base(localSocket, callback)
        {}

        protected override void ProcessRequest(NetworkStream stream)
        {
            try
            {
                var reader = new BinaryReader(stream);
                var type = (CommandType)reader.ReadByte();
                if(type != CommandType.Connect && type != CommandType.Bind)
                {
                    Dispoce(false);
                    return;
                }
                switch(type)
                {
                    case CommandType.Connect:
                    {
                        int port = ReaderUtils.ReadInt16BE(reader);
                        byte[] host = reader.ReadBytes(4);
                        Username = ReadString(reader);
                        EndPoint = new IPEndPoint(new IPAddress(host), port);
                        RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        RemoteSocket.Connect(EndPoint);
                        if(RemoteSocket.Connected)
                        {
                            SendRespoce(0x5a);
                            return;
                        }
                        SendRespoce(0x5b);
                        break;
                    }
                    case CommandType.Bind:
                    {
                        // BUG: Potential bug
                        byte[] bytes = reader.ReadBytes(6);
                        int port = bytes[1] * 256 + bytes[0];
                        string host = bytes[2] + "." + bytes[3] + "." + bytes[4] + "." + bytes[5];
                        BindEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
                        AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        AcceptSocket.Bind(new IPEndPoint(0, 0));
                        AcceptSocket.Listen(10);
                        SendRespoce(0x5a);
                        AcceptSocket.BeginAccept(OnAccept, null);
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
                RemoteSocket = AcceptSocket.EndAccept(ar);
                IPAddress remoteAddress = ((IPEndPoint)RemoteSocket.RemoteEndPoint).Address;
                if(remoteAddress.Equals(BindEndPoint.Address))
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
                if(AcceptSocket != null)
                {
                    AcceptSocket.Close();
                }
                AcceptSocket = null;
            }
        }

        protected override void SendRespoce(byte value)
        {
            try
            {
                var result = new byte[] { 0, value, 0, 0, 0, 0, 0, 0 };
                int sent = LocalSocket.Send(result);
                if(value == 0x5a && sent > 0)
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
            var stringBytes = new byte[256];
            int len = 0;
            for(int i = 0; i < 256; i++)
            {
                len += reader.Read(stringBytes, len, 1);
                if(stringBytes[len - 1] == 0)
                {
                    break;
                }
            }
            return Encoding.UTF8.GetString(stringBytes, 0, len - 1);
        }

        #region Nested type: CommandType

        private enum CommandType : byte
        {
            Connect = 1, 
            Bind = 2, 
        }

        #endregion
    }
}