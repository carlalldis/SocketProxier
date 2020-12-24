using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketProxierLib
{
    public class SocketProxy : IDisposable
    {
        private readonly Socket _client;
        private readonly Socket _server;
        private long _threadExits = 0;
        private long _bytesIn = 0;
        private long _bytesOut = 0;
        public event EventHandler Disconnect;
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The number of bytes sent from server to client
        /// </summary>
        public long BytesIn
        {
            get => Interlocked.Read(ref _bytesIn);
            private set
            {
                _ = Interlocked.Exchange(ref _bytesIn, value);
            }
        }

        /// <summary>
        /// The number of bytes sent from client to server
        /// </summary>
        public long BytesOut
        {
            get => Interlocked.Read(ref _bytesOut);
            private set
            {
                _ = Interlocked.Exchange(ref _bytesOut, value);
            }
        }

        public SocketProxy(Socket client, EndPoint endPoint)
        {
            _client = client;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            _server.Connect(endPoint);
            ConnectionString = $"Connection from {client.RemoteEndPoint.ToString()} forwarded via {_server.LocalEndPoint.ToString()} to {_server.RemoteEndPoint.ToString()}";
            Logger.LogMessage(Severity.Information, "SocketProxy", $"Open:  {ConnectionString}");

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
                    BytesIn += count;
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
                    BytesOut += count;
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
                Logger.LogMessage(Severity.Information, "SocketProxy", $"Close: {ConnectionString} closed gracefully");
                OnDisconnect(EventArgs.Empty);
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
                Logger.LogMessage(Severity.Information, "SocketProxy", $"Error: {ConnectionString} closed: {e.Message}");
                OnDisconnect(EventArgs.Empty);
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

        protected virtual void OnDisconnect(EventArgs e)
        {
            Disconnect?.Invoke(this, e);
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public override string ToString()
        {
            return ConnectionString;
        }
    }

    public class DisconnectEventArgs
    {
    }
}