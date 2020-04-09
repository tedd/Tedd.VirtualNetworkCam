using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using DirectShow.BaseClasses;
//using Microsoft.Kinect;

namespace KinectCam
{
    [ComVisible(true)]
    [Guid("A9017694-6378-4472-B5A2-55A41E476238")]
    public partial class AboutForm : BasePropertyPage
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
   //         cbMirrored.Checked = KinectCamSettigns.Default.Mirrored;
   //         cbDesktop.Checked = KinectCamSettigns.Default.Desktop;
			//cbZoom.Checked = KinectCamSettigns.Default.Zoom;
			//cbTrackHead.Checked = KinectCamSettigns.Default.TrackHead;
		}

        private void cbMirrored_CheckedChanged(object sender, EventArgs e)
        {
            //KinectCamSettigns.Default.Mirrored = cbMirrored.Checked;
        }

        private void cbDesktop_CheckedChanged(object sender, EventArgs e)
        {
            //KinectCamSettigns.Default.Desktop = cbDesktop.Checked;
        }

		private void cbZoom_CheckedChanged(object sender, EventArgs e)
		{
			//KinectCamSettigns.Default.Zoom = cbZoom.Checked;
		}

		private void cbTrackHead_CheckedChanged(object sender, EventArgs e)
		{
			//KinectCamSettigns.Default.TrackHead = cbTrackHead.Checked;
		}
	}
}
