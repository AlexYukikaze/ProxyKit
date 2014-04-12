#ProxyKit


Proxy server framework

## Features :

- **Core functional**
- **Socks4 proxy**
- **Socks5**
    - **Login/Password auth**
    - **~~Port binding~~** not yet
    - **~~UDP~~** not yet
- **~~HTTP/HTTPS~~** not yet

## Usage :

### Socks :

```csharp
using System;
using ProxyKit;
using ProxyKit.Socks;

namespace ProjectProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            ProxyServer proxy = new SocksServer(1777);
            proxy.Connect += ProxyOnConnect;
            proxy.Start();
            Console.ReadKey(true);
        }

        private static void ProxyOnConnect(object sender, ConnectEventArgs e)
        {
            Console.WriteLine("New connection");
            e.Client.ReceiveClient += FromClient;
            e.Client.ReceiveServer += FormServer;
        }

        private static void FormServer(object sender, ClientReceiveEventArgs e)
        {
            Console.WriteLine("S->C: " + e.Count);
        }

        private static void FromClient(object sender, ClientReceiveEventArgs e)
        {
            Console.WriteLine("C->S: " + e.Count);
        }
    }
}
```


## Support :

- Any bugs please feel free to report [here][issue].
- And you are welcome to fork and submit pullrequests.


## License :

The code is available at github [project][home] under **MIT licence**

 [home]: https://github.com/AlexYukikaze/ProxyKit
 [issue]: https://github.com/AlexYukikaze/ProxyKit/issues
