using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KinectCam;

namespace Tedd.VirtualNetworkCam
{
    internal class NetworkCamServer
    {
        private readonly int _port;
        private readonly VirtualCamFilter _camFilter;
        private CancellationTokenSource _cancellationTokenSource;
        private List<NetworkCamServerClient> _clients = new List<NetworkCamServerClient>();
        public NetworkCamServer(int port, VirtualCamFilter camFilter)
        {
            _port = port;
            _camFilter = camFilter;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            Logger.Info("Listening to TCP port " + _port);
            TcpListener listener = new TcpListener(_port);
            listener.Start();
            _cancellationTokenSource.Token.Register(listener.Stop);
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Logger.Info("Accepting new client: " + client.Client.RemoteEndPoint.ToString());
                    var clientTask =
                            HandleClient(client, _cancellationTokenSource.Token);
                                //.ContinueWith((antecedent) => client.Dispose())
                                //.ContinueWith((antecedent) => Console.WriteLine("Client disposed."))
                                ;
                }
                catch (ObjectDisposedException) when (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Logger.Info("TcpListener stopped listening because cancellation was requested.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error handling client.");
                }
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            var c = new NetworkCamServerClient(client, token, _camFilter);

            // Add reference
            lock (_clients)
                _clients.Add(c);


            // Remove reference
            c.Closed += (cc) =>
            {
                lock (_clients)
                    _clients.Remove(cc);
            };

        }

    }
}
