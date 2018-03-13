namespace AlertIntxnMap
{
    partial class AlertItxnMapForm
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
            // Cleanup the map control
            mapGL.Dispose();

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlertItxnMapForm));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonGridLines = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLoadIntersectionsLogFile = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonClearMap = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelLonLat = new System.Windows.Forms.ToolStripStatusLabel();
            this.comboBoxIntersectionLogEntries = new System.Windows.Forms.ComboBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.mapGL = new WSIMap.MapGL();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonGridLines,
            this.toolStripButtonLoadIntersectionsLogFile,
            this.toolStripButtonClearMap});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(984, 25);
            this.toolStrip.Stretch = true;
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "toolStrip";
            // 
            // toolStripButtonGridLines
            // 
            this.toolStripButtonGridLines.Checked = true;
            this.toolStripButtonGridLines.CheckOnClick = true;
            this.toolStripButtonGridLines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripButtonGridLines.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonGridLines.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonGridLines.Image")));
            this.toolStripButtonGridLines.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonGridLines.Name = "toolStripButtonGridLines";
            this.toolStripButtonGridLines.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonGridLines.Text = "Toggle grid lines";
            this.toolStripButtonGridLines.Click += new System.EventHandler(this.toolStripButtonGridLines_Click);
            // 
            // toolStripButtonLoadIntersectionsLogFile
            // 
            this.toolStripButtonLoadIntersectionsLogFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonLoadIntersectionsLogFile.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLoadIntersectionsLogFile.Image")));
            this.toolStripButtonLoadIntersectionsLogFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLoadIntersectionsLogFile.Name = "toolStripButtonLoadIntersectionsLogFile";
            this.toolStripButtonLoadIntersectionsLogFile.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonLoadIntersectionsLogFile.ToolTipText = "Load intersections log file";
            this.toolStripButtonLoadIntersectionsLogFile.Click += new System.EventHandler(this.toolStripButtonLoadIntersectionsLogFile_Click);
            // 
            // toolStripButtonClearMap
            // 
            this.toolStripButtonClearMap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonClearMap.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonClearMap.Image")));
            this.toolStripButtonClearMap.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonClearMap.Name = "toolStripButtonClearMap";
            this.toolStripButtonClearMap.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonClearMap.ToolTipText = "Clear map";
            this.toolStripButtonClearMap.Click += new System.EventHandler(this.toolStripButtonClearMap_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel,
            this.toolStripStatusLabelLonLat});
            this.statusStrip.Location = new System.Drawing.Point(0, 540);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(984, 22);
            this.statusStrip.TabIndex = 2;
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(901, 17);
            this.toolStripStatusLabel.Spring = true;
            this.toolStripStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabelLonLat
            // 
            this.toolStripStatusLabelLonLat.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripStatusLabelLonLat.Name = "toolStripStatusLabelLonLat";
            this.toolStripStatusLabelLonLat.Size = new System.Drawing.Size(68, 17);
            this.toolStripStatusLabelLonLat.Text = "this is a test";
            // 
            // comboBoxIntersectionLogEntries
            // 
            this.comboBoxIntersectionLogEntries.FormattingEnabled = true;
            this.comboBoxIntersectionLogEntries.Location = new System.Drawing.Point(13, 29);
            this.comboBoxIntersectionLogEntries.Name = "comboBoxIntersectionLogEntries";
            this.comboBoxIntersectionLogEntries.Size = new System.Drawing.Size(959, 21);
            this.comboBoxIntersectionLogEntries.TabIndex = 6;
            this.comboBoxIntersectionLogEntries.SelectedIndexChanged += new System.EventHandler(this.comboBoxIntersectionLogEntries_SelectedIndexChanged);
            // 
            // mapGL
            // 
            this.mapGL.AccumBits = ((byte)(0));
            this.mapGL.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mapGL.AutoCheckErrors = false;
            this.mapGL.AutoFinish = false;
            this.mapGL.AutoMakeCurrent = true;
            this.mapGL.AutoSwapBuffers = true;
            this.mapGL.BackColor = System.Drawing.Color.Black;
            this.mapGL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapGL.ColorBits = ((byte)(32));
            this.mapGL.DepthBits = ((byte)(16));
            this.mapGL.DisableDateLinePanning = false;
            this.mapGL.LatLonGrid = false;
            this.mapGL.Location = new System.Drawing.Point(12, 53);
            this.mapGL.MapViewListSize = ((byte)(5));
            this.mapGL.Name = "mapGL";
            this.mapGL.Size = new System.Drawing.Size(960, 480);
            this.mapGL.StencilBits = ((byte)(1));
            this.mapGL.TabIndex = 0;
            this.mapGL.UseToolTips = true;
            this.mapGL.WinXMove = null;
            this.mapGL.WinYMove = null;
            this.mapGL.KeyDown += new System.Windows.Forms.KeyEventHandler(this.mapGL_KeyDown);
            this.mapGL.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mapGL_MouseDown);
            this.mapGL.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mapGL_MouseMove);
            this.mapGL.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mapGL_MouseUp);
            // 
            // AlertItxnMapForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 562);
            this.Controls.Add(this.comboBoxIntersectionLogEntries);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.mapGL);
            this.Name = "AlertItxnMapForm";
            this.Text = "Alert Intersections";
            this.Load += new System.EventHandler(this.AlertIntxnMapForm_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WSIMap.MapGL mapGL;
        private WSIMap.ContextState contextState1;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonGridLines;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelLonLat;
        private System.Windows.Forms.ComboBox comboBoxIntersectionLogEntries;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolStripButton toolStripButtonLoadIntersectionsLogFile;
        private System.Windows.Forms.ToolStripButton toolStripButtonClearMap;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
    }
}

