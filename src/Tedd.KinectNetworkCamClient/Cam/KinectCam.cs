using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectX.Data;
using KinectX.Extensions;
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

        public delegate void NewImageDelegate(KinectCam sender, byte[] imagePointer);

        public event NewImageDelegate NewImage;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        public KinectCam()
        {
            _kinectSensor = KinectSensor.GetDefault();
            _kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            _multiFrameSourceReader = _kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex);

            this._multiFrameSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            //_depthFrameReader = _kinectSensor.DepthFrameSource.OpenReader();
            //_depthFrameReader.FrameArrived += Reader_FrameArrived;
            _depthFrameDescription = _kinectSensor.DepthFrameSource.FrameDescription;
            _depthPixels = new byte[_depthFrameDescription.Width * _depthFrameDescription.Height * _depthFrameDescription.BytesPerPixel];

            //ColorFrameDescription = _kinectSensor.ColorFrameSource.FrameDescription;
            ColorFrameDescription = _kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            ColorImageLength = (int)(ColorFrameDescription.Width * ColorFrameDescription.Height * ColorFrameDescription.BytesPerPixel);
            //_colorImageLength= (int)((_bitmap.BackBufferStride * (_bitmap.PixelHeight - 1)) + (_bitmap.PixelWidth * _bytesPerPixel));
            //ColorImagePtr = Marshal.AllocHGlobal(ColorImageLength);
            ColorImage = new byte[ColorImageLength];

            _coordinateMapper = _kinectSensor.CoordinateMapper;
            _colorMappedToDepthPoints = new DepthSpacePoint[ColorFrameDescription.Width * ColorFrameDescription.Height];

            _kinectSensor.Open();
        }

        private unsafe void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null)
                return;

            using var depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
            using var colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
            
            using var bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();

            // If any frame has expired by the time we process this event, return.
            if (depthFrame == null || colorFrame == null || bodyIndexFrame == null)
                return;

            // Access the depth frame data directly via LockImageBuffer to avoid making a copy
            using var depthFrameData = depthFrame.LockImageBuffer();
            _coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                depthFrameData.UnderlyingBuffer,
                depthFrameData.Size,
                _colorMappedToDepthPoints);
            

            var depthFrameDescription = depthFrame.FrameDescription;
            var depthWidth = depthFrameDescription.Width;
            var depthHeight = depthFrameDescription.Height;

            //var colorFrameDescription = colorFrame.FrameDescription;
            //var colorFrameDescription = colorFrame.CreateFrameDescription(ColorImageFormat.Bgra);
            //colorFrameDescription.
            //colorFrame.CopyConvertedFrameDataToIntPtr(_colorImagePtr, (uint)_colorImageLength, ColorImageFormat.Bgra);
            fixed (byte* ColorImagePtr = ColorImage)
            {

                using var colorFrameData = colorFrame.LockRawImageBuffer();
                var colorFrameSpan = new Span<byte>((void*) colorFrameData.UnderlyingBuffer, (int) colorFrameData.Size);
                var colorImageSpan = new Span<byte>((void*)ColorImagePtr, ColorImageLength);
                //var toImageSpan = new Span<byte>((void*) ColorImagePtr, ColorImageLength);

                //colorFrameSpan.CopyTo(colorImageSpan);
                colorFrame.CopyConvertedFrameDataToArray(ColorImage, ColorImageFormat.Bgra);


                using var bodyIndexData = bodyIndexFrame.LockImageBuffer();
                var bodyIndexDataPointer = (byte*) bodyIndexData.UnderlyingBuffer;
                //int colorMappedToDepthPointCount = _colorMappedToDepthPoints.Length;
                var colorMappedToDepthPointsPointer = new Span<DepthSpacePoint>(_colorMappedToDepthPoints);
                //var colorImageSpan = new Span<byte>((void*) ColorImagePtr, ColorImageLength);

                for (var colorIndex = 0; colorIndex < colorMappedToDepthPointsPointer.Length; ++colorIndex)
                {
                    float colorMappedToDepthX = colorMappedToDepthPointsPointer[colorIndex].X;
                    float colorMappedToDepthY = colorMappedToDepthPointsPointer[colorIndex].Y;


                    // The sentinel value is -inf, -inf, meaning that no depth pixel corresponds to this color pixel.
                    if (!float.IsNegativeInfinity(colorMappedToDepthX) &&
                        !float.IsNegativeInfinity(colorMappedToDepthY))
                    {
                        // Make sure the depth pixel maps to a valid point in color space
                        int depthX = (int) (colorMappedToDepthX + 0.5f);
                        int depthY = (int) (colorMappedToDepthY + 0.5f);

                        // If the point is not valid, there is no body index there.
                        if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                        {
                            int depthIndex = (depthY * depthWidth) + depthX;

                            // If we are tracking a body for the current pixel, do not zero out the pixel
                            if (bodyIndexDataPointer[depthIndex] != 0xff)
                            {
                                //colorImageSpan[ColorImageLength-4-(colorIndex*4)] = colorFrameSpan[colorIndex+1];
                                //colorImageSpan[ColorImageLength-3-(colorIndex*4)] = colorFrameSpan[colorIndex+2];
                                //colorImageSpan[ColorImageLength-2-(colorIndex*4)] = colorFrameSpan[colorIndex+3];
                                //colorImageSpan[ColorImageLength-1-(colorIndex*4)] = colorFrameSpan[colorIndex+4];

                                colorImageSpan[(colorIndex*4)-4] = colorFrameSpan[(colorIndex*2)+1];
                                colorImageSpan[(colorIndex*4)-3] = colorFrameSpan[(colorIndex * 2) + 2];
                                colorImageSpan[(colorIndex*4)-2] = colorFrameSpan[(colorIndex * 2) + 1];
                                colorImageSpan[(colorIndex*4)-1] = colorFrameSpan[(colorIndex * 2) + 2];
                                continue;
                            }
                        }
                    }

                    //colorImageSpan[ColorImageLength - 4 - (colorIndex * 2)] = 0;
                    //colorImageSpan[ColorImageLength - 3 - (colorIndex * 2)] = 0;
                    //colorImageSpan[ColorImageLength - 2 - (colorIndex * 2)] = 0;
                    //colorImageSpan[ColorImageLength - 1 - (colorIndex * 2)] = 0;

                    colorImageSpan[(colorIndex * 4)+0] = 0;
                    colorImageSpan[(colorIndex * 4)+1] = 0;
                    colorImageSpan[(colorIndex * 4)+2] = 0;
                    colorImageSpan[(colorIndex * 4)+3] = 0;
                }
            }

            NewImage?.Invoke(this, ColorImage);
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {

        }

        private unsafe void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using var depthFrame = e.FrameReference.AcquireFrame();
            if (depthFrame == null)
                return;

            using var depthBuffer = depthFrame.LockImageBuffer();
            var frameData = (ushort*)depthBuffer.UnderlyingBuffer;

            var from = new Span<byte>(frameData, (int)depthBuffer.Size);
            var to = new Span<byte>(_depthPixels);
            from.CopyTo(to);
        }

        public void ReadLoop()
        {

            //using (var ks = new KxStream())
            //{
            //    var color = ks.LatestRGBImage();
            //    var cvColor = CvColor.FromBGR(color);
            //    var depth = ks.LatestDepthImage();
            //    cvColor.Show();
            //}
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
