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
        private readonly ManualResetEventSlim _nextImageWaiter=new ManualResetEventSlim(false);

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
            //_bitmapMem= new Memory<byte>((void*)_bitmap.BackBuffer, _bitmap.PixelWidth * _bitmap.PixelHeight * 4);

            _cam.NewImage += CamOnNewImage;
            
            _client = new NetworkCamDriverClient();
            _client.Connect("127.0.0.1", 9090);
            Thread.Sleep(1000);
            _sendThread = new Thread(SendThreadLoop) { Name = "SendThread", IsBackground = true };
            _sendThread.Start();
        }
        private FastRandom fr = new FastRandom();

        private unsafe void SendThreadLoop()
        {
            byte[] backBuffer = null;
            Span<byte> backBufferSpan = null;
            for (; ; )
            {
                _nextImageWaiter.Wait();
                _cam.ColorImageLock.EnterReadLock();

                var image = new ReadOnlySpan<byte>(_cam.ColorImage);
                //var image = st;
                var cl = _client.Width * _client.Height * _client.BytesPerPixel;
                if (backBuffer == null || backBuffer.Length != cl)
                {
                    backBuffer = new byte[cl];
                    backBufferSpan = new Span<byte>(backBuffer);
                }

                //image.CopyTo(backBufferSpan);

                var width = Math.Min(_cam.ColorFrameDescription.Width, _client.Width);
                var height = Math.Min(_cam.ColorFrameDescription.Height, _client.Height);
                var dcl = backBufferSpan.Length -1;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var si = ((y * _cam.ColorFrameDescription.Width) + x) * 4;

                        var sd = ((y * _client.Width) + x) * _client.BytesPerPixel;
                        // Flip y axis
                        //var sd = ((y * _client._width) + (_client._width-1-x)) * _client._bytesPerPixel;

                        if (_client.BytesPerPixel == 3)
                        {
                            backBufferSpan[dcl - (sd + 0)] = image[si + 2];
                            backBufferSpan[dcl - (sd + 1)] = image[si + 1];
                            backBufferSpan[dcl - (sd + 2)] = image[si + 0];
                        }
                        else if (_client.BytesPerPixel == 4)
                        {
                            backBufferSpan[dcl - (sd + 0)] = image[si + 4];
                            backBufferSpan[dcl - (sd + 1)] = image[si + 3];
                            backBufferSpan[dcl - (sd + 2)] = image[si + 2];
                            backBufferSpan[dcl - (sd + 4)] = image[si + 1];
                        }
                        else
                        {
                            for (var i = 0; i < _client.BytesPerPixel; i++)
                                backBufferSpan[sd + i] = fr.NextByte();
                        }
                    }
                }
                _cam.ColorImageLock.ExitReadLock();

                ////MemoryExtensions.Reverse(ispan);
                //for (var i = 0; i < ispan.Length; i++)
                //{
                //    if (ispan[i] == 0)
                //        ispan[i] = fr.NextByte();
                //}
                //Thread.Sleep(10);
                _client.SendImage(backBuffer);
                //_client.SendImage(new ReadOnlySpan<byte>(_cam.ColorImage));
            }

        }

        private unsafe void CamOnNewImage(KinectCam sender, byte[] image)
        {
            _nextImageWaiter.Set();

            _bitmap.Lock();
            var sp = new Span<byte>(image);
            var st = new Span<byte>((void*)_bitmap.BackBuffer, _bitmap.PixelWidth * _bitmap.PixelHeight * 4);
            _cam.ColorImageLock.EnterReadLock();
            sp.CopyTo(st);
            _cam.ColorImageLock.ExitReadLock();
            _nextImageWaiter.Reset();

            //for (var x = 0; x < 1920; x++)
            //{
            //    for (var y = 0; y < 816; y++)
            //    {
            //        var si = ((y * 1920) + x) * 4;
            //        var sd = ((y * 1920) + x) * 4;
            //        st[sd + 0] = image[si + 0];
            //        st[sd + 1] = image[si + 1];
            //        st[sd + 2] = image[si + 2];
            //    }
            //}

            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            _bitmap.Unlock();

            

            //_continueWaiter.WaitOne();

        }
    }
}
