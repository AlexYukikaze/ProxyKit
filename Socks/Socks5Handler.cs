using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ProxyKit.Socks.Auth;

namespace ProxyKit.Socks
{
    internal sealed class Socks5Handler : SocksHandler
    {
        private readonly AuthCallbackHandler _authCallback;
        public Socks5Handler(Socket localSocket, 
            HandshakeCallback callback,
            AuthCallbackHandler authCallback) 
            : base(localSocket, callback)
        {
            _authCallback = authCallback;
        }

        protected override void ProcessRequest(NetworkStream stream)
        {
            try
            {
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                int len = reader.ReadByte();
                byte[] types = reader.ReadBytes(len);
                byte type = 0xFF;

                AuthMethod authMethod = null;


                foreach (byte b in types)
                {
                    if (b == 0 && _authCallback == null) // No auth
                    {
                        type = 0;
                        authMethod = new AuthNone();
                        break;
                    }

                    if (b == 2 && _authCallback != null) // Auth by login/password
                    {
                        type = 2;
                        authMethod = new AuthUserPass(_authCallback);
                    }
                }

                writer.Write(new byte[] { 0x05, type });
                if (authMethod == null)
                {
                    Dispoce(false);
                    return;
                }

                bool success = authMethod.Auth(stream);
                if (!success)
                {
                    Dispoce(false);
                    return;
                }

                byte ver = reader.ReadByte();
                if(ver != 0x05)
                {
                    Dispoce(false);
                    return;
                }

                byte command = reader.ReadByte();
                reader.ReadByte(); // SKIP RESERVED

                switch (command)
                {
                    case 0x01: // CONNECT
                        byte addrType = reader.ReadByte();
                        _endPoint = GetEndPoint(reader, addrType);

                        _remoteSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        _remoteSocket.BeginConnect(_endPoint, ConnectCallback,
                            _remoteSocket);

                        break;

                    case 0x02: // BIND
                        //TODO: Impliment binding
                        SendRespoce(0x07);
                        break;

                    case 0x03: // UDP ASSOCIATE
                        //TODO: Impliment association
                        SendRespoce(0x07);
                        break;

                    default:
                        SendRespoce(0x07);
                        return;
                }

            }
            catch
            {
                Dispoce(false);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _remoteSocket.EndConnect(ar);
                SendRespoce(0x00);
            }
            catch
            {
                SendRespoce(0x01);
            }
        }

        private IPEndPoint GetEndPoint(BinaryReader reader, byte type)
        {
            switch (type)
            {
                case 1:
                    {
                        byte[] ipBytes = reader.ReadBytes(4);
                        byte[] portBytes = reader.ReadBytes(2);
                        int port = portBytes[0] * 256 + portBytes[1];

                        return new IPEndPoint(new IPAddress(ipBytes), port);
                    }
                case 3:
                    {
                        string host = reader.ReadString();
                        byte[] portBytes = reader.ReadBytes(2);
                        var ip = Dns.GetHostEntry(host).AddressList[0];
                        int port = portBytes[0] * 256 + portBytes[1];

                        return new IPEndPoint(ip, port);
                    }
                case 4:
                    throw new NotImplementedException("IPv6 not implimented yet");
                default:
                    throw new Exception("Unknown cammand type '" + type + "'");
            }
        }

        protected override void OnAccept(IAsyncResult ar)
        {
            throw new NotImplementedException();
        }

        protected override void SendRespoce(byte value)
        {
            byte[] responce;
            try
            {
                var endPoint = (IPEndPoint)_remoteSocket.LocalEndPoint;
                var ipBytes = endPoint.Address.GetAddressBytes();
                var portBytes = BitConverter.GetBytes((short) endPoint.Port);
                responce = new byte[] {
                    5, value,0, 1,
                    ipBytes[0], 
                    ipBytes[1], 
                    ipBytes[2], 
                    ipBytes[3], 
                    portBytes[1], 
                    portBytes[0]
                };
            }
            catch (Exception)
            {
                responce = new byte[] { 5, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
            }

            try
            {
                int sent = _localSocket.Send(responce);
                if (sent > 0)
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
    }
}
