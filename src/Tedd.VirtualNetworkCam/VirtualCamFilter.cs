using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DirectShow;
using DirectShow.BaseClasses;
using Sonic;
using Tedd.VirtualNetworkCam;
using Tedd.VirtualNetworkCam.Models.Settings;

namespace KinectCam
{
    [ComVisible(true)]
    [Guid("E48ECF1A-A5E7-4EB0-8BF7-E15185D66FA4")]
    [AMovieSetup(Merit.Normal, AMovieSetup.CLSID_VideoInputDeviceCategory)]
    [PropPageSetup(typeof(AboutForm))]
    public class VirtualCamFilter : BaseSourceFilter, IAMFilterMiscFlags
    {
        #region Constants

        private const int c_iDefaultWidth = 1920;
        private const int c_iDefaultHeight = 1080;
        private const int c_nDefaultBitCount = 16;
        private const int c_iDefaultFPS = 30;
        private const int c_iFormatsCount = 8;
        private const int c_nGranularityW = 160;
        private const int c_nGranularityH = 120;
        private const int c_nMinWidth = 1920;
        private const int c_nMinHeight = 1080;
        private const int c_nMaxWidth = c_nMinWidth + c_nGranularityW * (c_iFormatsCount - 1);
        private const int c_nMaxHeight = c_nMinHeight + c_nGranularityH * (c_iFormatsCount - 1);
        private const int c_nMinFPS = 30;
        private const int c_nMaxFPS = 60;

        #endregion

        #region Variables

        internal int m_nWidth = c_iDefaultWidth;
        internal int m_nHeight = c_iDefaultHeight;
        internal int m_nBitCount = c_nDefaultBitCount;
        internal long m_nAvgTimePerFrame = UNITS / c_iDefaultFPS;

        protected IntPtr m_hScreenDC = IntPtr.Zero;
        protected IntPtr m_hMemDC = IntPtr.Zero;
        protected IntPtr m_hBitmap = IntPtr.Zero;
        protected BitmapInfo m_bmi = new BitmapInfo();

        protected int m_nMaxWidth = 0;
        protected int m_nMaxHeight = 0;
        private static NetworkCamServer _tcpServer;
        private static Task _tcpServerTask;
        private AppSettings _settings;

        internal static Memory<byte> FrontBuffer;
        internal static Memory<byte> BackBuffer;

        #endregion

        #region Constructor
        public VirtualCamFilter()
            : base("Tedd.VirtualNetworkCam")
        {
            Logger.Info("VirtualCamFilter.Ctn()");

            if (_tcpServer == null)
            {
                _tcpServer = new NetworkCamServer(9090, this);
                _tcpServerTask = _tcpServer.StartAsync();
                _settings = new AppSettings();
                var size = m_nWidth * m_nHeight * ((m_nBitCount + 7) / 8);
                var b1 = new byte[size];
                var b2 = new byte[size];
                //rnd.NextBytes(b1);
                //rnd.NextBytes(b2);
                FrontBuffer = new Memory<byte>(b1);
                BackBuffer = new Memory<byte>(b2);
            }

            m_bmi.bmiHeader = new BitmapInfoHeader();
            AddPin(new VirtualCamStream("Capture", this));
        }

        #endregion


        private unsafe void ShowNextImage(IntPtr ptr, int length)
        {
            var p = new Span<byte>((void*)ptr, length);
            if (FrontBuffer.Length > length)
            {
                Logger.Info($"Warning: Frontbuffer {FrontBuffer.Length} larger than destination {length}");
                FrontBuffer.Span.Slice(0, length).CopyTo(p);
                return;
            }

            FrontBuffer.Span.CopyTo(p);
        }

        #region Overridden Methods

        protected override int OnInitializePins()
        {
            return NOERROR;
        }

        public override int Pause()
        {
            if (m_State == FilterState.Stopped)
            {
                m_hScreenDC = CreateDC("DISPLAY", null, null, IntPtr.Zero);
                m_nMaxWidth = GetDeviceCaps(m_hScreenDC, 8); // HORZRES
                m_nMaxHeight = GetDeviceCaps(m_hScreenDC, 10); // VERTRES
                m_hMemDC = CreateCompatibleDC(m_hScreenDC);
                m_hBitmap = CreateCompatibleBitmap(m_hScreenDC, m_nWidth, Math.Abs(m_nHeight));
            }
            return base.Pause();
        }

        public override int Stop()
        {
            //KinectHelper.DisposeSensor();
            int hr = base.Stop();
            if (m_hBitmap != IntPtr.Zero)
            {
                DeleteObject(m_hBitmap);
                m_hBitmap = IntPtr.Zero;
            }
            if (m_hScreenDC != IntPtr.Zero)
            {
                DeleteDC(m_hScreenDC);
                m_hScreenDC = IntPtr.Zero;
            }
            if (m_hMemDC != IntPtr.Zero)
            {
                DeleteDC(m_hMemDC);
                m_hMemDC = IntPtr.Zero;
            }
            return hr;
        }

        #endregion

        #region Methods

        public int CheckMediaType(AMMediaType pmt)
        {
            if (pmt == null) return E_POINTER;
            if (pmt.formatPtr == IntPtr.Zero) return VFW_E_INVALIDMEDIATYPE;
            if (pmt.majorType != MediaType.Video)
            {
                return VFW_E_INVALIDMEDIATYPE;
            }
            if (
                pmt.subType != MediaSubType.RGB24
                && pmt.subType != MediaSubType.RGB32
                && pmt.subType != MediaSubType.ARGB32
                && pmt.subType != MediaSubType.YUY2
            )
            {
                return VFW_E_INVALIDMEDIATYPE;
            }
            BitmapInfoHeader _bmi = pmt;
            if (_bmi == null)
            {
                return E_UNEXPECTED;
            }
            if (_bmi.Compression != BI_RGB)
            {
                return VFW_E_TYPE_NOT_ACCEPTED;
            }
            if (_bmi.BitCount != 24 && _bmi.BitCount != 32)
            {
                return VFW_E_TYPE_NOT_ACCEPTED;
            }
            VideoStreamConfigCaps _caps;
            GetDefaultCaps(0, out _caps);
            if (
                _bmi.Width < _caps.MinOutputSize.Width
                || _bmi.Width > _caps.MaxOutputSize.Width
            )
            {
                return VFW_E_INVALIDMEDIATYPE;
            }
            long _rate = 0;
            {
                VideoInfoHeader _pvi = pmt;
                if (_pvi != null)
                {
                    _rate = _pvi.AvgTimePerFrame;
                }
            }
            {
                VideoInfoHeader2 _pvi = pmt;
                if (_pvi != null)
                {
                    _rate = _pvi.AvgTimePerFrame;
                }
            }
            if (_rate < _caps.MinFrameInterval || _rate > _caps.MaxFrameInterval)
            {
                return VFW_E_INVALIDMEDIATYPE;
            }
            return NOERROR;
        }

        public int SetMediaType(AMMediaType pmt)
        {
            lock (m_Lock)
            {
                if (m_hBitmap != IntPtr.Zero)
                {
                    DeleteObject(m_hBitmap);
                    m_hBitmap = IntPtr.Zero;
                }
                BitmapInfoHeader _bmi = pmt;
                m_bmi.bmiHeader.BitCount = _bmi.BitCount;
                if (_bmi.Height != 0) m_bmi.bmiHeader.Height = _bmi.Height;
                if (_bmi.Width > 0) m_bmi.bmiHeader.Width = _bmi.Width;
                m_bmi.bmiHeader.Compression = BI_RGB;
                m_bmi.bmiHeader.Planes = 1;
                m_bmi.bmiHeader.ImageSize = ALIGN16(m_bmi.bmiHeader.Width) * ALIGN16(Math.Abs(m_bmi.bmiHeader.Height)) * m_bmi.bmiHeader.BitCount / 8;
                m_nWidth = _bmi.Width;
                m_nHeight = _bmi.Height;
                m_nBitCount = _bmi.BitCount;

                {
                    VideoInfoHeader _pvi = pmt;
                    if (_pvi != null)
                    {
                        m_nAvgTimePerFrame = _pvi.AvgTimePerFrame;
                    }
                }
                {
                    VideoInfoHeader2 _pvi = pmt;
                    if (_pvi != null)
                    {
                        m_nAvgTimePerFrame = _pvi.AvgTimePerFrame;
                    }
                }
            }
            return NOERROR;
        }

        public int GetMediaType(int iPosition, ref AMMediaType pMediaType)
        {
            if (iPosition < 0) return E_INVALIDARG;
            VideoStreamConfigCaps _caps;
            GetDefaultCaps(0, out _caps);

            int nWidth = 0;
            int nHeight = 0;

            if (iPosition == 0)
            {
                if (Pins.Count > 0 && Pins[0].CurrentMediaType.majorType == MediaType.Video)
                {
                    pMediaType.Set(Pins[0].CurrentMediaType);
                    return NOERROR;
                }
                nWidth = _caps.InputSize.Width;
                nHeight = _caps.InputSize.Height;
            }
            else
            {
                iPosition--;
                nWidth = _caps.MinOutputSize.Width + _caps.OutputGranularityX * iPosition;
                nHeight = _caps.MinOutputSize.Height + _caps.OutputGranularityY * iPosition;
                if (nWidth > _caps.MaxOutputSize.Width || nHeight > _caps.MaxOutputSize.Height)
                {
                    return VFW_S_NO_MORE_ITEMS;
                }
            }

            pMediaType.majorType = DirectShow.MediaType.Video;
            pMediaType.formatType = DirectShow.FormatType.VideoInfo;

            VideoInfoHeader vih = new VideoInfoHeader();
            vih.AvgTimePerFrame = m_nAvgTimePerFrame;
            vih.BmiHeader.Compression = BI_RGB;
            vih.BmiHeader.BitCount = (short)m_nBitCount;
            vih.BmiHeader.Width = nWidth;
            vih.BmiHeader.Height = nHeight;
            vih.BmiHeader.Planes = 1;
            vih.BmiHeader.ImageSize = vih.BmiHeader.Width * Math.Abs(vih.BmiHeader.Height) * vih.BmiHeader.BitCount / 8;

            if (vih.BmiHeader.BitCount == 16)
            {
                pMediaType.subType = DirectShow.MediaSubType.YUY2;
            }        
            if (vih.BmiHeader.BitCount == 32)
            {
                pMediaType.subType = DirectShow.MediaSubType.RGB32;
            }
            if (vih.BmiHeader.BitCount == 24)
            {
                pMediaType.subType = DirectShow.MediaSubType.RGB24;
            }
            AMMediaType.SetFormat(ref pMediaType, ref vih);
            pMediaType.fixedSizeSamples = true;
            pMediaType.sampleSize = vih.BmiHeader.ImageSize;

            return NOERROR;
        }

        public int DecideBufferSize(ref IMemAllocatorImpl pAlloc, ref AllocatorProperties prop)
        {
            AllocatorProperties _actual = new AllocatorProperties();

            BitmapInfoHeader _bmi = (BitmapInfoHeader)Pins[0].CurrentMediaType;
            prop.cbBuffer = _bmi.GetBitmapSize();

            if (prop.cbBuffer < _bmi.ImageSize)
            {
                prop.cbBuffer = _bmi.ImageSize;
            }
            if (prop.cbBuffer < m_bmi.bmiHeader.ImageSize)
            {
                prop.cbBuffer = m_bmi.bmiHeader.ImageSize;
            }

            prop.cBuffers = 1;
            prop.cbAlign = 1;
            prop.cbPrefix = 0;
            int hr = pAlloc.SetProperties(prop, _actual);
            return hr;
        }

        public unsafe int FillBuffer(ref IMediaSampleImpl _sample)
        {
            IntPtr _ptr;
            _sample.GetPointer(out _ptr);
            int length = _sample.GetSize();

            ShowNextImage(_ptr, length);

            //if (_settings.ShowDesktop)
            //{
            //    if (m_hBitmap == IntPtr.Zero)
            //    {
            //        m_hBitmap = CreateCompatibleBitmap(m_hScreenDC, m_nWidth, Math.Abs(m_nHeight));
            //    }
            //    IntPtr hOldBitmap = SelectObject(m_hMemDC, m_hBitmap);
            //    StretchBlt(m_hMemDC, 0, 0, m_nWidth, Math.Abs(m_nHeight), m_hScreenDC, 0, 0, m_nMaxWidth, m_nMaxHeight, TernaryRasterOperations.SRCCOPY);
            //    SelectObject(m_hMemDC, hOldBitmap);
            //    GetDIBits(m_hMemDC, m_hBitmap, 0, (uint)Math.Abs(m_nHeight), _ptr, ref m_bmi, 0);
            //}

            _sample.SetActualDataLength(_sample.GetSize());
            _sample.SetSyncPoint(true);

            return NOERROR;
        }



        public int GetLatency(out long prtLatency)
        {
            prtLatency = UNITS / 30;
            AMMediaType mt = Pins[0].CurrentMediaType;
            if (mt.majorType == MediaType.Video)
            {
                {
                    VideoInfoHeader _pvi = mt;
                    if (_pvi != null)
                    {
                        prtLatency = _pvi.AvgTimePerFrame;
                    }
                }
                {
                    VideoInfoHeader2 _pvi = mt;
                    if (_pvi != null)
                    {
                        prtLatency = _pvi.AvgTimePerFrame;
                    }
                }
            }
            return NOERROR;
        }

        public int GetNumberOfCapabilities(out int iCount, out int iSize)
        {
            iCount = 0;
            AMMediaType mt = new AMMediaType();
            while (GetMediaType(iCount, ref mt) == S_OK) { mt.Free(); iCount++; };
            iSize = Marshal.SizeOf(typeof(VideoStreamConfigCaps));
            return NOERROR;
        }

        public int GetStreamCaps(int iIndex, out AMMediaType ppmt, out VideoStreamConfigCaps _caps)
        {
            ppmt = null;
            _caps = null;
            if (iIndex < 0) return E_INVALIDARG;

            ppmt = new AMMediaType();
            HRESULT hr = (HRESULT)GetMediaType(iIndex, ref ppmt);
            if (FAILED(hr)) return hr;
            if (hr == VFW_S_NO_MORE_ITEMS) return S_FALSE;
            hr = (HRESULT)GetDefaultCaps(iIndex, out _caps);
            return hr;
        }

        public int SuggestAllocatorProperties(AllocatorProperties pprop)
        {
            AllocatorProperties _properties = new AllocatorProperties();
            HRESULT hr = (HRESULT)GetAllocatorProperties(_properties);
            if (FAILED(hr)) return hr;
            if (pprop.cbBuffer != -1)
            {
                if (pprop.cbBuffer < _properties.cbBuffer) return E_FAIL;
            }
            if (pprop.cbAlign != -1 && pprop.cbAlign != _properties.cbAlign) return E_FAIL;
            if (pprop.cbPrefix != -1 && pprop.cbPrefix != _properties.cbPrefix) return E_FAIL;
            if (pprop.cBuffers != -1 && pprop.cBuffers < 1) return E_FAIL;
            return NOERROR;
        }

        public int GetAllocatorProperties(AllocatorProperties pprop)
        {
            AMMediaType mt = Pins[0].CurrentMediaType;
            if (mt.majorType == MediaType.Video)
            {
                int lSize = mt.sampleSize;
                BitmapInfoHeader _bmi = mt;
                if (_bmi != null)
                {
                    if (lSize < _bmi.GetBitmapSize())
                    {
                        lSize = _bmi.GetBitmapSize();
                    }
                    if (lSize < _bmi.ImageSize)
                    {
                        lSize = _bmi.ImageSize;
                    }
                }
                pprop.cbBuffer = lSize;
                pprop.cBuffers = 1;
                pprop.cbAlign = 1;
                pprop.cbPrefix = 0;

            }
            return NOERROR;
        }

        public int GetDefaultCaps(int nIndex, out VideoStreamConfigCaps _caps)
        {
            _caps = new VideoStreamConfigCaps();

            _caps.guid = FormatType.VideoInfo;
            _caps.VideoStandard = AnalogVideoStandard.None;
            _caps.InputSize.Width = c_iDefaultWidth;
            _caps.InputSize.Height = c_iDefaultHeight;
            _caps.MinCroppingSize.Width = c_nMinWidth;
            _caps.MinCroppingSize.Height = c_nMinHeight;

            _caps.MaxCroppingSize.Width = c_nMaxWidth;
            _caps.MaxCroppingSize.Height = c_nMaxHeight;
            _caps.CropGranularityX = c_nGranularityW;
            _caps.CropGranularityY = c_nGranularityH;
            _caps.CropAlignX = 0;
            _caps.CropAlignY = 0;

            _caps.MinOutputSize.Width = _caps.MinCroppingSize.Width;
            _caps.MinOutputSize.Height = _caps.MinCroppingSize.Height;
            _caps.MaxOutputSize.Width = _caps.MaxCroppingSize.Width;
            _caps.MaxOutputSize.Height = _caps.MaxCroppingSize.Height;
            _caps.OutputGranularityX = _caps.CropGranularityX;
            _caps.OutputGranularityY = _caps.CropGranularityY;
            _caps.StretchTapsX = 0;
            _caps.StretchTapsY = 0;
            _caps.ShrinkTapsX = 0;
            _caps.ShrinkTapsY = 0;
            _caps.MinFrameInterval = UNITS / c_nMaxFPS;
            _caps.MaxFrameInterval = UNITS / c_nMinFPS;
            _caps.MinBitsPerSecond = (_caps.MinOutputSize.Width * _caps.MinOutputSize.Height * c_nDefaultBitCount) * c_nMinFPS;
            _caps.MaxBitsPerSecond = (_caps.MaxOutputSize.Width * _caps.MaxOutputSize.Height * c_nDefaultBitCount) * c_nMaxFPS;

            return NOERROR;
        }

        #endregion

        #region IAMFilterMiscFlags Members

        public int GetMiscFlags()
        {
            return (int)AMFilterMiscFlags.IsSource;
        }

        #endregion

        #region API

        [StructLayout(LayoutKind.Sequential)]
        protected struct BitmapInfo
        {
            public BitmapInfoHeader bmiHeader;
            public int[] bmiColors;
        }

        private enum TernaryRasterOperations : uint
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
            CAPTUREBLT = 0x40000000
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest,
            IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan,
            uint cScanLines, [Out] IntPtr lpvBits, ref BitmapInfo lpbmi, uint uUsage);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        #endregion
    }
}