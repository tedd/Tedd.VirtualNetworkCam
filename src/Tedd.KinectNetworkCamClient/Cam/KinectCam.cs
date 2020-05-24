using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace Tedd.KinectNetworkCamClient.Cam
{
    class KinectCam : IDisposable
    {
        private KinectSensor _kinectSensor;
        //private DepthFrameReader _depthFrameReader;
        private FrameDescription _depthFrameDescription;
        private byte[] _depthPixels;
        private MultiSourceFrameReader _multiFrameSourceReader;
        private CoordinateMapper _coordinateMapper;
        public readonly FrameDescription ColorFrameDescription;
        private DepthSpacePoint[] _colorMappedToDepthPoints;
        public int ColorImageLength;
        //public IntPtr ColorImagePtr;
        public byte[] ColorImage;
        public readonly ReaderWriterLockSlim ColorImageLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public delegate void NewImageDelegate(KinectCam sender, byte[] imagePointer);
        public event NewImageDelegate NewImage;
        private Thread EventQueueThread;
        private bool ColorImageIsQueued = false;
        private readonly ManualResetEventSlim _nextImageWaiter = new ManualResetEventSlim(false);
        private ConcurrentQueue<MultiSourceFrame> _frameQueue = new ConcurrentQueue<MultiSourceFrame>();

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        public KinectCam()
        {
            _kinectSensor = KinectSensor.GetDefault();
            _kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            // Set up new image event
            _multiFrameSourceReader = _kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex);
            this._multiFrameSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            // Depth buffer
            _depthFrameDescription = _kinectSensor.DepthFrameSource.FrameDescription;
            _depthPixels = new byte[_depthFrameDescription.Width * _depthFrameDescription.Height * _depthFrameDescription.BytesPerPixel];

            // Color image buffer
            ColorFrameDescription = _kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            ColorImageLength = (int)(ColorFrameDescription.Width * ColorFrameDescription.Height * ColorFrameDescription.BytesPerPixel);
            ColorImage = new byte[ColorImageLength];

            // Coordinate mapping
            _coordinateMapper = _kinectSensor.CoordinateMapper;
            _colorMappedToDepthPoints = new DepthSpacePoint[ColorFrameDescription.Width * ColorFrameDescription.Height];

            _kinectSensor.Open();

            // Kinect will by default return data on GUI thread. We queue the frame there and signal this thread to do the actual processing.
            EventQueueThread = new Thread(EventQueueThreadLoop)
            {
                Name = nameof(EventQueueThread),
                IsBackground = true
            };
            EventQueueThread.Start();

        }

        private void EventQueueThreadLoop()
        {
            for (; ; )
            {
                _nextImageWaiter.Reset();
                _nextImageWaiter.Wait();

                // We want only the latest, skipping any if we are behind
                while (_frameQueue.Count > 1)
                    _frameQueue.TryDequeue(out _);

                // Try to get latest frame
                if (!_frameQueue.TryDequeue(out var multiSourceFrame))
                    continue;

                ProcessMultisourceFrame(multiSourceFrame);
            }
        }

        private unsafe void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null)
                return;
            _frameQueue.Enqueue(multiSourceFrame);
            _nextImageWaiter.Set();
        }

        private unsafe void ProcessMultisourceFrame(MultiSourceFrame multiSourceFrame)
        {

            // Aquiure all frames
            using var depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
            using var colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
            using var bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();

            // If any frame has expired by the time we process this event, return.
            if (depthFrame == null || colorFrame == null || bodyIndexFrame == null)
                return;

            // Copy depth frame
            using var depthFrameData = depthFrame.LockImageBuffer();
            _coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(depthFrameData.UnderlyingBuffer, depthFrameData.Size, _colorMappedToDepthPoints);

            var depthFrameDescription = depthFrame.FrameDescription;
            var depthWidth = depthFrameDescription.Width;
            var depthHeight = depthFrameDescription.Height;

            // Copy color frame
            ColorImageLock.EnterWriteLock();
            fixed (byte* ColorImagePtr = ColorImage)
            {
                using var colorFrameData = colorFrame.LockRawImageBuffer();
                var colorFrameSpan = new Span<byte>((void*)colorFrameData.UnderlyingBuffer, (int)colorFrameData.Size);
                var colorImageSpan = new Span<byte>((void*)ColorImagePtr, ColorImageLength);
                var colorImageSpanUInt32 = new Span<UInt32>((void*)ColorImagePtr, ColorImageLength);


                //colorFrameSpan.CopyTo(colorImageSpan);
                colorFrame.CopyConvertedFrameDataToArray(ColorImage, ColorImageFormat.Bgra);

                // Get body index data
                using var bodyIndexData = bodyIndexFrame.LockImageBuffer();
                var bodyIndexDataPointer = (byte*)bodyIndexData.UnderlyingBuffer;
                var colorMappedToDepthPointsPointer = new Span<DepthSpacePoint>(_colorMappedToDepthPoints);

                // Go over and black out all colors that are not a body index
                for (var colorIndex = 0; colorIndex < colorMappedToDepthPointsPointer.Length; ++colorIndex)
                {
                    float colorMappedToDepthX = colorMappedToDepthPointsPointer[colorIndex].X;
                    float colorMappedToDepthY = colorMappedToDepthPointsPointer[colorIndex].Y;


                    // The sentinel value is -inf, -inf, meaning that no depth pixel corresponds to this color pixel.
                    if (!float.IsNegativeInfinity(colorMappedToDepthX) &&
                        !float.IsNegativeInfinity(colorMappedToDepthY))
                    {
                        // Make sure the depth pixel maps to a valid point in color space
                        int depthX = (int)(colorMappedToDepthX + 0.5f);
                        int depthY = (int)(colorMappedToDepthY + 0.5f);

                        // If the point is not valid, there is no body index there.
                        if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                        {
                            int depthIndex = (depthY * depthWidth) + depthX;

                            // If we are tracking a body for the current pixel, do not zero out the pixel
                            if (bodyIndexDataPointer[depthIndex] != 0xff)
                            {
                                // Set alpha to fully opaque
                                // For some reason we are ARGB
                                colorImageSpanUInt32[colorIndex] |= 0xFF000000;
                                continue;
                            }
                        }
                    }

                    // Background
                    colorImageSpanUInt32[colorIndex] = 0x00000000;
                }
            }
            ColorImageLock.ExitWriteLock();
            NewImage?.Invoke(this, ColorImage);


        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {

        }


        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _multiFrameSourceReader?.Dispose();
        }

        #endregion
    }
}
