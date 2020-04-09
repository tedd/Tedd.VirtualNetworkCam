namespace KinectCam
{
    partial class AboutForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.gbAuthors = new System.Windows.Forms.GroupBox();
            this.tbAuthors = new System.Windows.Forms.TextBox();
            this.cbMirrored = new System.Windows.Forms.CheckBox();
            this.cbDesktop = new System.Windows.Forms.CheckBox();
            this.gbOptions = new System.Windows.Forms.GroupBox();
            this.cbZoom = new System.Windows.Forms.CheckBox();
            this.cbTrackHead = new System.Windows.Forms.CheckBox();
            this.gbAuthors.SuspendLayout();
            this.gbOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.titleLabel.Location = new System.Drawing.Point(12, 6);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(356, 38);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "KinectCam ver. 2.2";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gbAuthors
            // 
            this.gbAuthors.Controls.Add(this.tbAuthors);
            this.gbAuthors.Location = new System.Drawing.Point(12, 138);
            this.gbAuthors.Name = "gbAuthors";
            this.gbAuthors.Size = new System.Drawing.Size(356, 70);
            this.gbAuthors.TabIndex = 7;
            this.gbAuthors.TabStop = false;
            this.gbAuthors.Text = "Authors";
            // 
            // tbAuthors
            // 
            this.tbAuthors.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbAuthors.Location = new System.Drawing.Point(6, 19);
            this.tbAuthors.Margin = new System.Windows.Forms.Padding(0);
            this.tbAuthors.Multiline = true;
            this.tbAuthors.Name = "tbAuthors";
            this.tbAuthors.ReadOnly = true;
            this.tbAuthors.Size = new System.Drawing.Size(344, 36);
            this.tbAuthors.TabIndex = 0;
            this.tbAuthors.Text = "VirtualCam and BaseClass created by Maxim Kartavenkov aka Sonic\r\nKinect Driver as" +
    " WebCam created by Piotr Sowa";
            // 
            // cbMirrored
            // 
            this.cbMirrored.AutoSize = true;
            this.cbMirrored.Location = new System.Drawing.Point(6, 19);
            this.cbMirrored.Name = "cbMirrored";
            this.cbMirrored.Size = new System.Drawing.Size(64, 17);
            this.cbMirrored.TabIndex = 5;
            this.cbMirrored.Text = "Mirrored";
            this.cbMirrored.UseVisualStyleBackColor = true;
            this.cbMirrored.CheckedChanged += new System.EventHandler(this.cbMirrored_CheckedChanged);
            // 
            // cbDesktop
            // 
            this.cbDesktop.AutoSize = true;
            this.cbDesktop.Location = new System.Drawing.Point(6, 42);
            this.cbDesktop.Name = "cbDesktop";
            this.cbDesktop.Size = new System.Drawing.Size(66, 17);
            this.cbDesktop.TabIndex = 6;
            this.cbDesktop.Text = "Desktop";
            this.cbDesktop.UseVisualStyleBackColor = true;
            this.cbDesktop.CheckedChanged += new System.EventHandler(this.cbDesktop_CheckedChanged);
            // 
            // gbOptions
            // 
            this.gbOptions.Controls.Add(this.cbDesktop);
            this.gbOptions.Controls.Add(this.cbTrackHead);
            this.gbOptions.Controls.Add(this.cbZoom);
            this.gbOptions.Controls.Add(this.cbMirrored);
            this.gbOptions.Location = new System.Drawing.Point(12, 47);
            this.gbOptions.Name = "gbOptions";
            this.gbOptions.Size = new System.Drawing.Size(356, 85);
            this.gbOptions.TabIndex = 6;
            this.gbOptions.TabStop = false;
            this.gbOptions.Text = "Options (only for current session)";
            // 
            // cbZoom
            // 
            this.cbZoom.AutoSize = true;
            this.cbZoom.Location = new System.Drawing.Point(76, 19);
            this.cbZoom.Name = "cbZoom";
            this.cbZoom.Size = new System.Drawing.Size(53, 17);
            this.cbZoom.TabIndex = 5;
            this.cbZoom.Text = "Zoom";
            this.cbZoom.UseVisualStyleBackColor = true;
            this.cbZoom.CheckedChanged += new System.EventHandler(this.cbZoom_CheckedChanged);
            // 
            // cbTrackHead
            // 
            this.cbTrackHead.AutoSize = true;
            this.cbTrackHead.Location = new System.Drawing.Point(76, 42);
            this.cbTrackHead.Name = "cbTrackHead";
            this.cbTrackHead.Size = new System.Drawing.Size(80, 17);
            this.cbTrackHead.TabIndex = 5;
            this.cbTrackHead.Text = "TrackHead";
            this.cbTrackHead.UseVisualStyleBackColor = true;
            this.cbTrackHead.CheckedChanged += new System.EventHandler(this.cbTrackHead_CheckedChanged);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(380, 220);
            this.Controls.Add(this.gbAuthors);
            this.Controls.Add(this.gbOptions);
            this.Controls.Add(this.titleLabel);
            this.Name = "AboutForm";
            this.Text = "About";
            this.Title = "About";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.gbAuthors.ResumeLayout(false);
            this.gbAuthors.PerformLayout();
            this.gbOptions.ResumeLayout(false);
            this.gbOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.GroupBox gbAuthors;
        private System.Windows.Forms.TextBox tbAuthors;
        private System.Windows.Forms.CheckBox cbMirrored;
        private System.Windows.Forms.CheckBox cbDesktop;
        private System.Windows.Forms.GroupBox gbOptions;
        private System.Windows.Forms.CheckBox cbZoom;
        private System.Windows.Forms.CheckBox cbTrackHead;
    }
}