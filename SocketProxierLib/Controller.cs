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
    /// <summary>
    /// Controls the spawning of new socket proxies to a fixed destination based on incoming client connections
    /// </summary>
    public class Controller : IDisposable
    {
        Socket _listener;
        DnsEndPoint _destination;
        Task _loop;
        CancellationTokenSource _loopCancelTokenSrc;
        CancellationToken _loopCancelToken;
        public List<SocketProxy> OpenConnections { get; }
        public ControllerStatus Status { get; private set; } = ControllerStatus.NotStarted;

        public Controller(int listenPort, string destinationHost, int destinationPort)
        {
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(new IPEndPoint(IPAddress.Loopback, listenPort));
            }
            catch (Exception e)
            {
                Logger.LogMessage(Severity.Critical, "Controller", $"Failed to start listener {e.Message}");
                return;
            }

            try
            {
                _destination = new DnsEndPoint(destinationHost, destinationPort);
            }
            catch (Exception e)
            {
                Logger.LogMessage(Severity.Critical, "Controller", $"Failed to resolve endpoint {e.Message}");
                return;
            }

            OpenConnections = new List<SocketProxy>(16);
            Logger.LogMessage(Severity.Verbose, "Controller", "Initialised");
        }

        /// <summary>
        /// Start listening for incoming connections in a background thread
        /// </summary>
        public void Start()
        {
            Logger.LogMessage(Severity.Verbose, "Controller", "Starting");
            Status = ControllerStatus.Starting;
            try
            {
                _loopCancelTokenSrc = new CancellationTokenSource();
                CancellationToken _loopCancelToken = _loopCancelTokenSrc.Token;
                // Run the listening loop
                _listener.Listen(10);
                _loop = Task.Run(ListenLoop, _loopCancelToken);
            }
            catch (Exception e)
            {
                Logger.LogMessage(Severity.Error, "Controller", $"Failed during listening loop {e.Message}");
                return;
            }
        }

        /// <summary>
        /// Internal listening loop which starts new socket proxies for each incoming connection
        /// </summary>
        private void ListenLoop()
        {
            Logger.LogMessage(Severity.Verbose, "Controller", "Running");
            Status = ControllerStatus.Running;
            while (true)
            {
                // Listen for a client connection; once found, create a new socket proxy connection to the destination endpoint
                // Repeat for subsequent connections
                try
                {
                    var client = _listener.Accept();
                    _loopCancelToken.ThrowIfCancellationRequested();
                    var socketProxy = new SocketProxy(client, _destination);
                    OpenConnections.Add(socketProxy);
                }
                catch (Exception e)
                {
                    if (_loopCancelToken.IsCancellationRequested)
                    {
                        Logger.LogMessage(Severity.Verbose, "Controller", "Cancellation Requested");
                        break;
                    }
                    else
                    {
                        Logger.LogMessage(Severity.Error, "Controller", $"Failed during remote connection {e.Message}");
                        Status = ControllerStatus.Error;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Wait until an error is encountered
        /// </summary>
        public void Wait()
        {
            Logger.LogMessage(Severity.Verbose, "Controller", "Waiting");
            _loop.Wait();
        }

        public void Stop()
        {
            Logger.LogMessage(Severity.Information, "Controller", "Stopping");
            Status = ControllerStatus.Stopping;
            _loopCancelTokenSrc.Cancel();
            _listener.Shutdown(SocketShutdown.Both); // Shutting down the listener causes the loop to break out if stuck in .Accept() call and accept the cancellation
            _loop.Wait();
            OpenConnections.ForEach((c) => c.Dispose());
            _loop.Dispose();
            Status = ControllerStatus.Stopped;
        }

        public enum ControllerStatus
        {
            NotStarted,
            Starting,
            Running,
            Stopping,
            Stopped,
            Error
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
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
