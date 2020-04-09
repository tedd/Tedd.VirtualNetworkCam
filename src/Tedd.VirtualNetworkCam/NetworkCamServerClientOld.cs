//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.IO.Pipelines;
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

//        private TcpClient _client;
//        private readonly CancellationToken _token;
//        private readonly VirtualCamFilter _camFilter;
//        private Pipe _pipe;
//        private Socket _socket;
//        private Thread _fillThread;
//        private Thread _readThread;

//        public NetworkCamServerClient(TcpClient client, in CancellationToken token, VirtualCamFilter camFilter)
//        {
//            _client = client;
//            _token = token;
//            _camFilter = camFilter;
//            Logger.Info("Client: " + client.Client.RemoteEndPoint.ToString());
//        }

//        public async Task ProcessIncoming()
//        {
//            await ProcessLinesAsync(_client.Client);
//            _client.Close();
//            //_client.Dispose();
//            _client = null;
//        }

//        private async Task ProcessLinesAsync(Socket socket)
//        {
//            _pipe = new Pipe(new PipeOptions());
//            _socket = socket;

//            //var writing = FillPipeAsync();
//            //var reading = ReadPipeAsync();
//            _fillThread = new Thread(FillPipe)
//            {
//                Name = "FillPipeThread:" + socket.RemoteEndPoint.ToString(),
//                IsBackground = true
//            };
//            _fillThread.Start();
//            _readThread = new Thread(ReadPipe)
//            {
//                Name = "ReadPipeThread:" + socket.RemoteEndPoint.ToString(),
//                IsBackground = true
//            };
//            _readThread.Start();

//            while (!_token.IsCancellationRequested)
//            {
//                await Task.Delay(100);
//            }

//            //return Task.WhenAll(reading, writing);
//        }

//        private void FillPipe()
//        {
//            FillPipeAsync().Wait();
//        }
//        private void ReadPipe()
//        {
//            ReadPipeAsync().Wait();
//        }

//        private async Task FillPipeAsync()
//        {
//            var socket = _socket;
//            var writer = _pipe.Writer;
//            const int minimumBufferSize = 8192;
//            var readBuffer = new byte[minimumBufferSize];
//            var rbMem = new Memory<byte>(readBuffer);
//            var aseg = new ArraySegment<byte>(readBuffer);
//            while (!_token.IsCancellationRequested)
//            {
//                // Allocate at least 512 bytes from the PipeWriter
//                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
//                try
//                {
//                    int bytesRead = await socket.ReceiveAsync(aseg, SocketFlags.None);
//                    if (bytesRead == 0)
//                    {
//                        break;
//                    }
//                    Logger.Debug($"Client sent {bytesRead} bytes");
//                    // Copy to writer
//                    var s = rbMem.Slice(0, bytesRead);
//                    s.CopyTo(memory);
//                    // Tell the PipeWriter how much was read from the Socket
//                    writer.Advance(bytesRead);
//                }
//                catch (Exception ex)
//                {
//                    Logger.Error(ex, "Reading client socket");
//                    break;
//                }

//                // Make the data available to the PipeReader
//                var result = await writer.FlushAsync();

//                if (result.IsCompleted)
//                {
//                    break;
//                }
//            }
//            Logger.Info("Client disconnect.");

//            // Tell the PipeReader that there's no more data coming
//            writer.Complete();
//        }


//        private async Task ReadPipeAsync()
//        {
//            var reader = _pipe.Reader;
//            var state = ProtocolState.CommandWait;
//            byte command = 0;
//            var payloadSize = 0;

//            while (!_token.IsCancellationRequested)
//            {
//                ReadResult result = await reader.ReadAsync();

//                ReadOnlySequence<byte> buffer = result.Buffer;
//                SequencePosition? position = null;

//                do
//                {
//                    position = null;
//                    switch (state)
//                    {
//                        case ProtocolState.CommandWait:
//                            // Look for command identifier
//                            position = buffer.PositionOf((byte)0xFF);
//                            if (position == null)
//                                break;

//                            // Skip up until the command
//                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
//                            state = ProtocolState.Command;
//                            Logger.Debug("Protocol state: " + state.ToString());

//                            break;
//                        case ProtocolState.Command: // Read command
//                            // Do we have enough data for command?
//                            if (buffer.Length == 0)
//                                break;

//                            // Command starts at 0
//                            position = buffer.GetPosition(0);
//                            // Get first byte as command
//                            command = buffer.Slice(position.Value, 1).First.Span[0];
//                            // Advance one byte from position 0
//                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

//                            state = ProtocolState.PayloadSize;
//                            Logger.Debug("Protocol state: " + state.ToString());
//                            break;
//                        case ProtocolState.PayloadSize:
//                            // Do we have enough for size data? (24-bit)
//                            if (buffer.Length < 3)
//                                break;

//                            // Size starts at 0
//                            position = buffer.GetPosition(0);
//                            // Get 3 first bytes
//                            var pb = buffer.Slice(position.Value, 3).ToArray();
//                            // Advance 3 bytes from position 0
//                            buffer = buffer.Slice(buffer.GetPosition(3, position.Value));
//                            // Convert
//                            payloadSize = (pb[2] << 16)
//                                          | (pb[1] << 8)
//                                          | pb[0];

//                            // Wait for payload (or not)
//                            if (payloadSize == 0)
//                            {
//                                ProcessCommand(command, payloadSize, null);
//                                state = ProtocolState.CommandWait;
//                            }
//                            else
//                                state = ProtocolState.Payload;

//                            Logger.Debug("Protocol state: " + state.ToString());
//                            break;
//                        case ProtocolState.Payload:
//                            // Do we have enough data?
//                            if (buffer.Length < payloadSize)
//                                //if (buffer.Length < Math.Min(payloadSize - payloadSoFar, 8192))
//                                break;

//                            // Get payload and process it
//                            // Starts at 0
//                            position = buffer.GetPosition(0);
//                            // Get the payload portion
//                            var payload = buffer.Slice(position.Value, buffer.Length);
//                            // Advance payloadSize bytes
//                            buffer = buffer.Slice(buffer.GetPosition(buffer.Length, position.Value));
//                            // Process it
//                            ProcessCommand(command, payloadSize, payload);

//                            // Wait for command again
//                            state = ProtocolState.CommandWait;
//                            Logger.Debug("Protocol state: " + state.ToString());
//                            break;
//                    }

//                }
//                while (position != null);

//                // Tell the PipeReader how much of the buffer we have consumed
//                reader.AdvanceTo(buffer.Start, buffer.End);

//                // Stop reading if there's no more data coming
//                if (result.IsCompleted)
//                {
//                    break;
//                }
//            }

//            // Mark the PipeReader as complete
//            reader.Complete();
//        }

//        private void ProcessCommand(byte command, int payloadSize, ReadOnlySequence<byte>? slice = null)
//        {
//            Logger.Debug($"Received command {command} with payload size {payloadSize} delivered as partial {(slice.HasValue ? slice.Value.Length.ToString() : "NA")}");
//            if (slice.HasValue)
//                slice.Value.CopyTo(VirtualCamFilter.FrontBuffer.Span);
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