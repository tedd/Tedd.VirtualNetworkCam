using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd.VirtualNetworkCam.Client
{
    public class NetworkCamDriverClient
    {
        private TcpClient _client;
        private NetworkStream _networkStream;
        public int BytesPerPixel;
        public int Height;
        public int Width;
        public NetworkCamDriverClient() { }

        public void Connect(string server, int port)
        {
            _client = new TcpClient();
            _client.Connect(server, port);
            var timer = Environment.TickCount;
            while (!_client.Connected)
            {
                if (Environment.TickCount - timer > 10_000)
                    throw new Exception("Connection timeout...");
            }
            _networkStream = _client.GetStream();

            // Read info header
            var header = new byte[11];
            var len = 0;
            while (len < header.Length)
            {
                var r = _networkStream.Read(header, len, header.Length - len);
                len += r;
            }

            if (header[0] != 0xFF)
                throw new Exception($"Unexpected header: Needed 0xFF command start, got {header[0].ToString("X2")}");
            if (header[1] != 0x02)
                throw new Exception($"Unexpected header: Needed 0x02 header info, got {header[1].ToString("X2")}");
            Width = (header[2] << 24)
                         | (header[3] << 16)
                         | (header[4] << 8)
                         | header[5];
            Height = (header[6] << 24)
                         | (header[7] << 16)
                         | (header[8] << 8)
                         | header[9];
            BytesPerPixel = (byte)((header[10] + 7) / 8);
        }

#if !NET48 && !NETSTANDARD2_0
        public async Task SendImage(ReadOnlySpan<byte> image)
        {
            // Image command
            var s = image.Length;
            var header = new byte[]
            {
                255,                        // Start of command
                1,                          // Command
                (byte)(s & 0xFF),           // Size 0
                (byte)((s >> 8) & 0xFF),    // Size 1
                (byte)((s >> 16) & 0xFF)    // Size 2
            };

            await _networkStream.WriteAsync(header);
            await _networkStream.WriteAsync(image);
        }
#endif

#if NET48 || NETSTANDARD2_0
        public void SendImage(byte[] image)
        {
            // Image command
            var s = image.Length;
            var header = new byte[]
            {
                255,                        // Start of command
                1,                          // Command
                (byte)(s & 0xFF),           // Size 0
                (byte)((s >> 8) & 0xFF),    // Size 1
                (byte)((s >> 16) & 0xFF)    // Size 2
            };
            //#if NET48
            _networkStream.Write(header, 0, header.Length);
            _networkStream.Write(image, 0, image.Length);

        }
#endif

    }
}
