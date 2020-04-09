//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.IO;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using KinectCam;

//namespace Tedd.VirtualNetworkCam
//{
//    internal class NetworkCamServerClient : IDisposable
//    {
//        private enum ProtocolState
//        {
//            CommandWait = 0,
//            Command = 1,
//            PayloadSize = 2,
//            Payload = 3
//        }

//        private const int MaxBufferSize = 1920 * 1080 * 4 * 2;

//        private TcpClient _client;
//        private readonly CancellationToken _token;
//        private readonly VirtualCamFilter _camFilter;
//        private Socket _socket;
//        //private Thread _fillThread;
//        private Thread _readThread;

//        private byte[] _prb = new byte[MaxBufferSize];
//        private Memory<byte> _prbMem;
//        private ProtocolState state = ProtocolState.CommandWait;
//        private byte command = 0;
//        private int payloadSize = 0;

//        public delegate void ClosedDelegate(NetworkCamServerClient sender);

//        public event ClosedDelegate Closed;

//        public NetworkCamServerClient(TcpClient client, in CancellationToken token, VirtualCamFilter camFilter)
//        {
//            _prbMem = new Memory<byte>(_prb);
//            _client = client;
//            _token = token;
//            _camFilter = camFilter;
//            Logger.Info("Client: " + client.Client.RemoteEndPoint.ToString());

//            _socket = _client.Client;

//            //_fillThread = new Thread(FillPipe)
//            //{
//            //    Name = "FillPipeThread:" + socket.RemoteEndPoint.ToString(),
//            //    IsBackground = true
//            //};
//            //_fillThread.Start();
//            _readThread = new Thread(ReadThreadLoop)
//            {
//                Name = "ReadPipeThread:" + _socket.RemoteEndPoint.ToString(),
//                IsBackground = true
//            };
//            _readThread.Start();

//            //return Task.WhenAll(reading, writing);
//        }


//        private void ReadThreadLoop()
//        {
//            const int bufferSize = 4096*10;
//            var readBuffer = new byte[bufferSize];
//            //Buffer bigger than what we need, only fill it with as much as we expect, i
//            var cb = new CircularBuffer(MaxBufferSize);

//            while (!_token.IsCancellationRequested)
//            {
//                try
//                {
//                    int bytesRead = _socket.Receive(readBuffer);
//                    if (bytesRead == 0)
//                        break;

//                    Logger.Debug($"Client sent {bytesRead} bytes");
//                    // Copy to writer
//                    cb.Write(readBuffer, 0, bytesRead);
//                }
//                catch (Exception ex)
//                {
//                    Logger.Error(ex, "Reading client socket");
//                    break;
//                }

//                ProcessProtocolData(cb);

//            }
//            Logger.Info("Client disconnect.");
//            Closed?.Invoke(this);
//        }


//        private void ProcessProtocolData(CircularBuffer cb)
//        {

//            switch (state)
//            {
//                case ProtocolState.CommandWait:
//                    // Look for command identifier
//                    cb.Read(_prb, 0, 1);
//                    if (_prb[0] != 0xFF)
//                        throw new Exception("Expected stard of command 0xFF");

//                    state = ProtocolState.Command;
//                    Logger.Debug("Command start flag. Protocol state: " + state.ToString());

//                    break;
//                case ProtocolState.Command: // Read command
//                                            // Do we have enough data for command?
//                    if (cb.Count < 1)
//                        break;

//                    cb.Read(_prb, 0, 1);

//                    command = _prb[0];

//                    state = ProtocolState.PayloadSize;
//                    Logger.Debug("Command " + command + ". Protocol state: " + state.ToString());
//                    break;
//                case ProtocolState.PayloadSize:
//                    // Do we have enough for size data? (24-bit)
//                    if (cb.Count < 3)
//                        break;

//                    cb.Read(_prb, 0, 3);

//                    // Convert
//                    payloadSize = (_prb[2] << 16)
//                                  | (_prb[1] << 8)
//                                  | _prb[0];

//                    // Wait for payload (or not)
//                    if (payloadSize == 0)
//                    {
//                        ProcessCommand();
//                        state = ProtocolState.CommandWait;
//                    }
//                    else
//                        state = ProtocolState.Payload;

//                    Logger.Debug("Payload size " + payloadSize + ". Protocol state: " + state.ToString());
//                    break;
//                case ProtocolState.Payload:
//                    // Do we have enough data?
//                    if (cb.Count < payloadSize)
//                        break;

//                    cb.Read(_prb, 0, payloadSize);

//                    // Process it
//                    ProcessCommand();

//                    // Wait for command again
//                    state = ProtocolState.CommandWait;
//                    Logger.Debug("Protocol state: " + state.ToString());
//                    break;
//            }

//        }




//        private void ProcessCommand()
//        {
//            Logger.Debug($"Received command {command} with payload size {payloadSize}");
//            if (payloadSize > 0)
//            {
//                _prbMem.Span.Slice(0, payloadSize).CopyTo(VirtualCamFilter.FrontBuffer.Span);
//            }
//        }


//        #region IDisposable

//        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
//        public void Dispose()
//        {
//            _client?.Dispose();
//            _socket?.Dispose();
//        }

//        #endregion

//    }
//}