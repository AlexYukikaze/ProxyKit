using System;

namespace ProxyKit
{
    public class ClientReceiveEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int Count { get; set; }
        public bool Cancel { get; set; }

        public ClientReceiveEventArgs(byte[] data, int count)
        {
            Data = data;
            Count = count;
            Cancel = false;
        }
    }

    public class ConnectEventArgs : EventArgs
    {
        public ProxyClient Client { get; private set; }

        public ConnectEventArgs(ProxyClient client)
        {
            Client = client;
        }
    }
}
