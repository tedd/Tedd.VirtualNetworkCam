using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tedd.KinectNetworkCamClient.Annotations;
using Tedd.RandomUtils;

namespace Tedd.KinectNetworkCamClient
{
    internal class ImageMixer : INotifyPropertyChanged
    {
        public int Width { get; }
        public int Height { get; }
        public Tedd.WriteableBitmap BackgroundImage;
        public Tedd.WriteableBitmap ForegroundImage;
        public Tedd.WriteableBitmap FinishedImage;
        public readonly ReaderWriterLockSlim UpdateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly FastRandom _fr = new FastRandom();

        public ImageSource BackgroundImageSource
        {
            get => BackgroundImage.BitmapSource;
        }

        public ImageSource ForegroundImageSource
        {
            get => ForegroundImage.BitmapSource;
        }

        public ImageSource FinishedImageSource
        {
            get => FinishedImage.BitmapSource;
        }

        public ImageMixer(int width, int height)
        {
            Width = width;
            Height = height;
            BackgroundImage = new WriteableBitmap(width, height, PixelFormats.Bgra32);
            ForegroundImage = new WriteableBitmap(width, height, PixelFormats.Bgra32);
            FinishedImage = new WriteableBitmap(width, height, PixelFormats.Bgra32);
        }

        public unsafe void RepaintFinishedImage()
        {
            UpdateLock.EnterWriteLock();
            try
            {
                var size = Width * Height;

                var background = BackgroundImage.ToSpanUInt32();
                var cameraUInt = ForegroundImage.ToSpanUInt32();
                var finishedUInt = FinishedImage.ToSpanUInt32();

                // Blend
                for (int i = 0; i < size; i++)
                {
                    var src = cameraUInt[i];
                    finishedUInt[i] = Blend(background[i], src);
                }
            }
            finally
            {
                UpdateLock.ExitWriteLock();
            }
        }

        private static UInt32 Blend(UInt32 background, UInt32 foreground)
        {
            var fga = foreground >>24;
            if (fga == 0)
                return (background | 0xFF000000);
            //return foreground | 0xFF000000;
            var alpha = ((fga)) / 255f;
            var alphaR = 1f - alpha;
            var b = (byte)((((background >> 16) & 0xFF) * alpha) + (((foreground >> 16) & 0xFF) * alphaR));
            var g = (byte)((((background >> 8) & 0xFF) * alpha) + (((foreground >> 8) & 0xFF) * alphaR));
            var r = (byte)((((background >> 0) & 0xFF) * alpha) + (((foreground >> 0) & 0xFF) * alphaR));
            //var a = 255;
            return ((uint) b << 16) | ((uint) g << 8) | ((uint) r << 0) | 0xFF000000;

            //if (alpha == 0)
            //    return color1;
            //return color2;
        }

        //private static UInt32 Blend(UInt32 color1, UInt32 color2, UInt32 alpha)
        //{
        //    UInt32 rb = color1 & 0xff00ff;
        //    UInt32 g = color1 & 0x00ff00;
        //    rb += ((color2 & 0xff00ff) - rb) * alpha >> 8;
        //    g += ((color2 & 0x00ff00) - g) * alpha >> 8;
        //    return (rb & 0xff00ff) | (g & 0xff00);
        //}

        public unsafe void CopyTo(Span<byte> target, int width, int height, int bytesPerPixel)
        {
            try
            {
                UpdateLock.EnterReadLock();

                // Source image
                var image = FinishedImage.ToSpanByte();

                //// Special case when dimensions and format match
                //if (width == Width && height == Height && bytesPerPixel == 4)
                //{
                //    image.CopyTo(target);
                //    return;
                //}

                // Copy image to target. Target may not match size or bit depth.
                // So... First of all, lets not resize image. We copy whatever we have.
                // If target cam is too large then surrounding area will be black
                // If target cam is too small then image will be cropped.
                var iWidth = Math.Min(Width, width);
                var iHeight = Math.Min(Height, height);
                var dcl = target.Length - 1;
                for (var x = 0; x < iWidth; x++)
                {
                    for (var y = 0; y < iHeight; y++)
                    {
                        var si = ((y * Width) + x) * 4;

                        var sd = ((y * width) + x) * bytesPerPixel;
                        // Flip y axis
                        //var sd = ((y * _client._width) + (_client._width-1-x)) * _client._bytesPerPixel;

                        // 24-bit
                        if (bytesPerPixel == 3)
                        {
                            target[dcl - (sd + 0)] = image[si + 2];
                            target[dcl - (sd + 1)] = image[si + 1];
                            target[dcl - (sd + 2)] = image[si + 0];
                            //target[dcl - (sd + 0)] = image[si + 0];
                            //target[dcl - (sd + 1)] = image[si + 1];
                            //target[dcl - (sd + 2)] = image[si + 2];
                        }
                        // 32-bit
                        else if (bytesPerPixel == 4)
                        {
                            target[dcl - (sd + 0)] = image[si + 4];
                            target[dcl - (sd + 1)] = image[si + 3];
                            target[dcl - (sd + 2)] = image[si + 2];
                            target[dcl - (sd + 4)] = image[si + 1];
                        }
                        else
                        {
                            // If we do not recognize BytesPerPixel we randomize data so its easier to debug. This will be static.
                            for (var i = 0; i < bytesPerPixel; i++)
                                target[sd + i] = _fr.NextByte();
                        }
                    }
                }

            }
            finally
            {
                UpdateLock.ExitReadLock();
            }
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void UpdateForeground(Span<byte> image)
        {
            UpdateImage(ForegroundImage, image);
        }

        public void UpdateBackground(Span<byte> image)
        {
            UpdateImage(BackgroundImage, image);
        }

        private unsafe void UpdateImage(WriteableBitmap target, Span<byte> image)
        {
            //try
            //{
            //    UpdateLock.EnterWriteLock();

            // Copy image to target
            image.CopyTo(target.ToSpanByte());
            //}
            //finally
            //{
            //    UpdateLock.ExitWriteLock();
            //}
        }
    }
}
