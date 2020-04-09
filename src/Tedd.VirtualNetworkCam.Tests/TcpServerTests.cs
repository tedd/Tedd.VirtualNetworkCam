using System;
using System.Threading;
using Tedd.VirtualNetworkCam.Client;
using Tedd.RandomUtils;
using Xunit;

namespace Tedd.VirtualNetworkCam.Tests
{
    public class TcpServerTests
    {
        private const int NetworkCamPortNumber = 59387;
        //private const int NetworkCamPortNumber = 9090;
        [Fact]
        public void Test1()
        {
            var camFilter = new VirtualCamFilter();
            var tcpServer = new NetworkCamServer(NetworkCamPortNumber, camFilter);
            var startTask = tcpServer.StartAsync();

            Thread.Sleep(1000);

            var client = new NetworkCamDriverClient();
            client.Connect("127.0.0.1", NetworkCamPortNumber);


            var b = new byte[1920 * 1080 * 4];
            var image = new Memory<byte>(b);

            //for (var i = 0; i < 10; i++)
            //{
            //    //Thread.Sleep(100);
            //    ConcurrentRandom.NextBytes(b);
            //    client.SendImage(image).Wait();
            //}

            Thread.Sleep(2_000);

            var b1 = VirtualCamFilter.FrontBuffer.ToArray();
            for (var i = 0; i < b.Length; i++)
            {
                Assert.Equal(b[i], b1[i]);
            }
            Assert.Equal(b, b1);

        }
    }
}
