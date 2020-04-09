using System;

namespace Tedd.VirtualNetworkCam
{
    internal class VirtualCamFilter
    {
        public int m_nHeight;
        public int m_nWidth;
        public byte m_nBitCount;
        public static Memory<byte> FrontBuffer { get; set; }
        public static Memory<byte> BackBuffer { get; set; }
        public static readonly object FrontBufferLock = new object();

        public VirtualCamFilter()
        {
            FrontBuffer = new Memory<byte>(new byte[1920 * 1080 * 4]);
            BackBuffer = new Memory<byte>(new byte[1920 * 1080 * 4]);
        }

        public static Memory<byte> GetNextBuffer()
        {
            lock (FrontBufferLock)
            {
                var fb = FrontBuffer;
                FrontBuffer = BackBuffer;
                BackBuffer = fb;
                return fb;
            }
        }
    }

}