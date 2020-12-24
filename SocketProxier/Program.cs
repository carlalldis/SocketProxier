using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketProxierLib;

namespace SocketProxier
{
    class Program
    {
        static int Main(string[] args)
        {
            int listenPort;
            string destinationHost;
            int destinationPort;

            try
            {
                listenPort = int.Parse(args[0]);
                destinationHost = args[1];
                destinationPort = int.Parse(args[2]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse arguments: {e.Message}");
                Console.WriteLine("");
                Console.WriteLine("Usage: SocketProxier.exe listenPort destinationHost destinationPort");
                Console.WriteLine("");
                Console.WriteLine("    Example 1: SocketProxier.exe 12345 localhost 443");
                Console.WriteLine("    (Opens a local listening port on 12345 which redirects traffic to localhost:443)");
                Console.WriteLine("");
                Console.WriteLine("    Example 2: SocketProxier.exe 12345 www.website.com 443");
                Console.WriteLine("    (Opens a local listening port on 12345 which redirects traffic to www.website.com:443)");
                Console.WriteLine("");
                Console.WriteLine("    Example 3: SocketProxier.exe 12345 192.168.0.1 443");
                Console.WriteLine("    (Opens a local listening port on 12345 which redirects traffic to 192.168.0.1:443)");
                Console.WriteLine("");
                return 1;
            }

            try
            {
                Logger.WriteMessage += LoggingMethods.LogToConsole;
                var proxyController = new Controller(listenPort, destinationHost, destinationPort);
                proxyController.Start();

                // Wait for escape key to quit
                ConsoleKeyInfo cki;
                do
                {
                    cki = Console.ReadKey();
                } while (cki.Key != ConsoleKey.Escape);
                Console.Write("\b \b");
                proxyController.Dispose();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                return 1;
            }
        }
    }
}