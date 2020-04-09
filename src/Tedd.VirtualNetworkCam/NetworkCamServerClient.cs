using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KinectCam;

namespace Tedd.VirtualNetworkCam
{
    internal class NetworkCamServerClient : IDisposable
    {
        private enum ProtocolState
        {
            CommandWait = 0,
            Command = 1,
            PayloadSize = 2,
            Payload = 3
        }

        private const int ImageSize = 1920 * 1080;

        private TcpClient _client;
        private readonly CancellationToken _token;
        private readonly VirtualCamFilter _camFilter;
        private Socket _socket;
        //private Thread _fillThread;
        private Thread _readThread;

        //private byte[] _prb = new byte[MaxBufferSize];
        //private Memory<byte> _prbMem;
        private ProtocolState state = ProtocolState.CommandWait;
        private byte command = 0;
        private int payloadSize = 0;

        public delegate void ClosedDelegate(NetworkCamServerClient sender);

        public event ClosedDelegate Closed;

        public NetworkCamServerClient(TcpClient client, in CancellationToken token, VirtualCamFilter camFilter)
        {
            //_prbMem = new Memory<byte>(_prb);
            _client = client;
            _token = token;
            _camFilter = camFilter;
            Logger.Info("Client: " + client.Client.RemoteEndPoint.ToString());

            _socket = _client.Client;

            //_fillThread = new Thread(FillPipe)
            //{
            //    Name = "FillPipeThread:" + socket.RemoteEndPoint.ToString(),
            //    IsBackground = true
            //};
            //_fillThread.Start();
            _readThread = new Thread(ReadThreadLoop)
            {
                Name = "ReadPipeThread:" + _socket.RemoteEndPoint.ToString(),
                IsBackground = true
            };
            _readThread.Start();

            //return Task.WhenAll(reading, writing);
        }


        private void ReadThreadLoop()
        {
            // Send header

            SendInfo();


            var length = 0;
            var readBytes = 0;
            var buffer = new byte[1920 * 1080 * 4 * 2];
            var bufferSpan = new Span<byte>(buffer);
            var bufferMemory = new Memory<byte>(buffer);
            var asList = new ArraySegment<byte>[1];
            var noData = false;
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    readBytes = 0;
                    // Fill up buffer with header
                    while (state == ProtocolState.CommandWait && length < 5)
                    {
                        var segment = new ArraySegment<byte>(buffer, length, 5 - length);
                        asList[0] = segment;
                        readBytes = _socket.Receive(asList);
                        length += readBytes;

                        if (readBytes == 0)
                            noData = true;
                    }
                    // Fill up buffer with payload
                    while (state == ProtocolState.Payload && length < payloadSize)
                    {
                        var segment = new ArraySegment<byte>(buffer, length, payloadSize - length);
                        asList[0] = segment;
                        readBytes = _socket.Receive(asList);
                        length += readBytes;
                        if (readBytes == 0)
                            noData = true;
                    }
                    if (noData)
                        break;

                    switch (state)
                    {
                        case ProtocolState.CommandWait:
                            // Look for command identifier
                            if (buffer[0] != 0xFF)
                                throw new Exception("Expected start of command 0xFF, got " + buffer[0].ToString("X2") + $" (length: {length}, readBytes: {readBytes})");

                            state = ProtocolState.Command;
                            //Logger.Debug("Command start flag. Protocol state: " + state.ToString());

                            break;
                        case ProtocolState.Command: // Read command
                            command = buffer[1];

                            state = ProtocolState.PayloadSize;
                            //Logger.Debug("Command " + command + ". Protocol state: " + state.ToString());
                            break;
                        case ProtocolState.PayloadSize:
                            // Convert
                            payloadSize = (buffer[4] << 16)
                                          | (buffer[3] << 8)
                                          | buffer[2];

                            // Start from scratch
                            length = 0;

                            // Wait for payload (or not)
                            if (payloadSize == 0)
                            {
                                ProcessCommand(bufferMemory);
                                state = ProtocolState.CommandWait;
                            }
                            else
                                state = ProtocolState.Payload;

                            //Logger.Debug("Payload size " + payloadSize + ". Protocol state: " + state.ToString());
                            break;
                        case ProtocolState.Payload:
                            // Process it
                            ProcessCommand(bufferMemory.Slice(0, payloadSize));
                            length = 0;

                            // Wait for command again

                            state = ProtocolState.CommandWait;
                            //Logger.Debug("Protocol state: " + state.ToString());
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Reading client socket");
            }

            if (readBytes == 0)
            {
                Logger.Info("Client disconnect.");
            }

        }

        private void SendInfo()
        {
            var width = _camFilter.m_nWidth;
            var height = _camFilter.m_nHeight;
            var info = new byte[]
            {
                // Command start
                0xFF,
                // Type 2
                0x02,
                // Width
                (byte) (width >> 24),
                (byte) ((width >> 16) & 0xFF),
                (byte) ((width >> 8) & 0xFF),
                (byte) (width & 0xFF),
                // Height
                (byte) (height >> 24),
                (byte) ((height >> 16) & 0xFF),
                (byte) ((height >> 8) & 0xFF),
                (byte) (height & 0xFF),
                // BitsPerPixel
                (byte) _camFilter.m_nBitCount
            };
            _socket.Send(info);
        }


        private void ProcessCommand(Memory<byte> bufferMemory)
        {
            //Logger.Debug($"Received command {command} with payload size {payloadSize}");
            var dm = VirtualCamFilter.FrontBuffer;
            if (payloadSize > 0)
            {
                Parallel.For(0, ImageSize, (i) =>
                {
                    dm.Span[ImageSize - i] = bufferMemory.Span[i];
                });
                bufferMemory.Span.CopyTo(VirtualCamFilter.FrontBuffer.Span);
            }

        }



        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _client?.Dispose();
            _socket?.Dispose();
        }

        #endregion

    }
}