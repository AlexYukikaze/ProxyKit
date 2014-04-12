using System;

namespace ProxyKit
{
    public class ClientReceiveEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int Count { get { return Data.Length;  } }
        public bool Cancel { get; set; }

        public ClientReceiveEventArgs(byte[] data, int count)
        {
            Data = new byte[count];
            Array.Copy(data, Data, count);
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
