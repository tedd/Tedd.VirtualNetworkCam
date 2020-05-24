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
        private KinectCam _cam;
        private NetworkCamDriverClient _client;
        private Thread _sendThread;
        private readonly ManualResetEventSlim _nextImageWaiter = new ManualResetEventSlim(false);
        private ImageMixer _imageMixer;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();


            _cam = new KinectCam();
            _cam.NewImage += CamOnNewImage;

            _imageMixer = new ImageMixer(_cam.ColorFrameDescription.Width, _cam.ColorFrameDescription.Height);
            _imageMixer.BackgroundImage.LoadFile("Background.jpg");
            // TODO: Load background PNG file into BackgroundImage

            DataContext = _imageMixer;

            _client = new NetworkCamDriverClient();
            _client.Connect("127.0.0.1", 9090);
            // TODO: Not best way to connect :)
            Thread.Sleep(1000);

            // Start a separate thread for sending image
            _sendThread = new Thread(SendThreadLoop) { Name = "SendThread", IsBackground = true };
            _sendThread.Start();
        }

        private void SendThreadLoop()
        {
            var backBuffer = new byte[_client.Width * _client.Height * _client.BytesPerPixel];
            var backBufferSpan = (Span<byte>)backBuffer;
            for (; ; )
            {
                // Wait for next image to be ready
                _nextImageWaiter.Reset();
                _nextImageWaiter.Wait();

                // Copy (convert) mixed image to backbuffer
                // CopyTo will put read lock on _imageMixer to avoid tearing
                _imageMixer.CopyTo(backBufferSpan, _client.Width, _client.Height, _client.BytesPerPixel);

                // Send backbuffer
                _client.SendImage(backBuffer);
            }
        }

        private void CamOnNewImage(KinectCam sender, byte[] image)
        {
            // Copy to mixer
            _imageMixer.UpdateForeground(image);
            _imageMixer.RepaintFinishedImage();
            InvalidateImage(_imageMixer.ForegroundImage);
            InvalidateImage(_imageMixer.FinishedImage);

            // Trigger new copy to backbuffer and send
            _nextImageWaiter.Set();
        }

        private void InvalidateImage(WriteableBitmap writeableBitmap)
        {
            // We can only invalidate from GUI thread
            if (Dispatcher.CheckAccess())
                writeableBitmap.Invalidate();
            else
                Dispatcher.Invoke(writeableBitmap.Invalidate);
        }
    }
}
