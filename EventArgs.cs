using System;

namespace ProxyKit
{
    public class ClientReceiveEventArgs : EventArgs
    {
        public ClientReceiveEventArgs(byte[] data, int count)
        {
            Data = new byte[count];
            Array.Copy(data, Data, count);
            Cancel = false;
        }

        public byte[] Data { get; set; }

        public int Count
        {
            get
            {
                return Data.Length;
            }
        }

        public bool Cancel { get; set; }
    }

    public class ConnectEventArgs : EventArgs
    {
        public ConnectEventArgs(ProxyClient client)
        {
            Client = client;
        }

        public ProxyClient Client { get; private set; }
    }
}