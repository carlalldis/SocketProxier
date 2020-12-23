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
    class SocketProxy : IDisposable
    {
        private readonly string _connectionString;
        private readonly Socket _client;
        private readonly Socket _server;
        private long _threadExits = 0;
        public SocketProxy(Socket client, IPAddress destinationAddress, int destinationPort)
        {
            _client = client;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.Connect(new IPEndPoint(destinationAddress, destinationPort));
            _connectionString = $"Connection from {client.RemoteEndPoint.ToString()} forwarded through {_server.LocalEndPoint.ToString()} to {_server.RemoteEndPoint.ToString()}";
            Console.WriteLine($"Open:  {_connectionString}");

            // Launch threads for bi-directional communication
            Thread serverThread = new Thread(ServerToClient);
            Thread clientThread = new Thread(ClientToServer);
            serverThread.Start();
            clientThread.Start();
        }

        private void ServerToClient()
        {
            byte[] data = new byte[1024];
            try
            {
                while (true)
                {
                    int count = _server.Receive(data);
                    if (count == 0)
                    {
                        HandleClosure();
                        return;
                    }
                    _client.Send(data, count, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
            finally
            {
                Dispose();
            }
        }

        private void ClientToServer()
        {
            byte[] stuff = new byte[1024];
            try
            {
                while (true)
                {
                    int count = _client.Receive(stuff);
                    if (count == 0)
                    {
                        HandleClosure();
                        return;
                    }
                    _server.Send(stuff, count, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Logs a closed message if no existing exits have been logged
        /// </summary>
        private void HandleClosure()
        {
            var exits = IncrementExits();
            if (exits <= 1)
            {
                Console.WriteLine($"Close: {_connectionString} closed gracefully");
            }
        }

        /// <summary>
        /// Logs an error if no existing exits have been logged
        /// </summary>
        /// <param name="e"></param>
        private void HandleError(Exception e)
        {
            var exits = IncrementExits();
            if (exits <= 1)
            {
                Console.WriteLine($"Error: {_connectionString} closed: {e.Message}");
            }
        }

        /// <summary>
        /// Increments the exit count
        /// </summary>
        /// <returns>The new exit count</returns>
        private long IncrementExits()
        {
            Interlocked.Increment(ref _threadExits);
            return Interlocked.Read(ref _threadExits);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _server.Close();
                    _client.Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}