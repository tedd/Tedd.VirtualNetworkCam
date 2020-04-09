using System;

namespace Tedd.VirtualNetworkCam
{
    internal class VirtualCamFilter
    {
        public int m_nHeight;
        public int m_nWidth;
        public byte m_nBitCount;
        public static Memory<byte> FrontBuffer { get; set; }

        public VirtualCamFilter()
        {
            FrontBuffer = new Memory<byte>(new byte[1920 * 1080 * 4]);
        }
    }

}