using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Kinect;
using OpenCvSharp;
using Tedd.KinectNetworkCamClient.Cam;
using Tedd.VirtualNetworkCam.Client;
using Tedd.RandomUtils;
using Window = System.Windows.Window;

namespace Tedd.KinectNetworkCamClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap _bitmap = null;
        private KinectCam _cam;
        private NetworkCamDriverClient _client;
        private Thread _sendThread;
        private ManualResetEvent _sendWaiter;

        public ImageSource ImageSource
        {
            get
            {
                return _bitmap;
            }
        }
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            DataContext = this;
            _cam = new KinectCam();
            _bitmap = new WriteableBitmap(_cam.ColorFrameDescription.Width, _cam.ColorFrameDescription.Height, 75, 75, PixelFormats.Bgra32, null);
            _cam.NewImage += CamOnNewImage;

            _sendWaiter = new ManualResetEvent(false);
            

            _client = new NetworkCamDriverClient();
            _client.Connect("127.0.0.1", 9090);
            Thread.Sleep(1000);
            _sendThread = new Thread(SendThreadLoop) { Name = "SendThread", IsBackground = true };
            _sendThread.Start();
        }
        private FastRandom fr = new FastRandom();

        private unsafe void SendThreadLoop()
        {
            var ibytes = new byte[_cam.ColorImage.Length];
            var ispan = new Span<byte>(ibytes);
            for (; ; )
            {
                _sendWaiter.WaitOne();
                var image = new ReadOnlySpan<byte>(_cam.ColorImage);
                image.CopyTo(ispan);
                //for (var x = 0; x < 1920; x++)
                //{
                //    for (var y = 0; y < 816; y++)
                //    {
                //        var si = ((y * 1080) + x)*4;
                //        var sd = ((y * 816) + x)*4;
                //        ispan[sd+0] = image[si+0];
                //        ispan[sd + 1] = image[si+1];
                //        ispan[sd + 2] = image[si+2];
                //        ispan[sd + 3] = image[si+3];
                //    }
                //}
                
                MemoryExtensions.Reverse(ispan);
                for (var i = 0; i < ispan.Length; i++)
                {
                    if (ispan[i] == 0)
                        ispan[i] = fr.NextByte();
                }
                _client.SendImage(ibytes);
                //_client.SendImage(new ReadOnlySpan<byte>(_cam.ColorImage));
            }

        }

        private unsafe void CamOnNewImage(KinectCam sender, byte[] image)
        {
            _sendWaiter.Set();
            _sendWaiter.Reset();

            _bitmap.Lock();
            var sp = new Span<byte>(image);
            var st = new Span<byte>((void*)_bitmap.BackBuffer, _bitmap.PixelWidth * _bitmap.PixelHeight * 4);
            sp.CopyTo(st);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            _bitmap.Unlock();
        }
    }
}
