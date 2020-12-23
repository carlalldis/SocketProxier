using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketProxier
{
    class Program
    {
        static int Main(string[] args)
        {
            int listenPort;
            IPAddress destinationAddress;
            int destinationPort;
            try
            {
                listenPort = int.Parse(args[0]);
                destinationAddress = IPAddress.Parse(args[1]);
                destinationPort = int.Parse(args[2]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse arguments: {e.Message}");
                Console.WriteLine("Usage: SocketProxier.exe listenPort destinationAddress destinationPort");
                Console.WriteLine("    Example: SocketProxier.exe 12345 www.webserver.com 443");
                Console.WriteLine("    (Opens a local listening port on 12345 which redirects traffic to www.webserver.com:443)");
                return 1;
            }

            Socket listener;
            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, listenPort));
                listener.Listen(10);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to start listener: {e.Message}");
                return 1;
            }

            try
            {
                while (true)
                {
                    var client = listener.Accept();
                    new SocketProxy(client, destinationAddress, destinationPort);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed during listening loop: {e.Message}");
                return 1;
            }
        }
    }
}