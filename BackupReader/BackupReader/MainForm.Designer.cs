namespace BackupReader
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ofdBackup = new System.Windows.Forms.OpenFileDialog();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.detailsTextBox = new System.Windows.Forms.TextBox();
            this.tvDirs = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.extractToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripSeparator();
            this.opencatalogToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.savecatalogToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.fbdBackup = new System.Windows.Forms.FolderBrowserDialog();
            this.ofdCatalog = new System.Windows.Forms.OpenFileDialog();
            this.sfdCatalog = new System.Windows.Forms.SaveFileDialog();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ofdBackup
            // 
            this.ofdBackup.DefaultExt = "*.bkf";
            this.ofdBackup.Filter = "Backup Files (*.bkf)|*.bkf|All Files|*.*";
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.tableLayoutPanel1);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(930, 499);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(930, 544);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(930, 22);
            this.statusStrip1.TabIndex = 0;
            // 
            // tsStatus
            // 
            this.tsStatus.Name = "tsStatus";
            this.tsStatus.Size = new System.Drawing.Size(39, 17);
            this.tsStatus.Text = "Ready";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.Controls.Add(this.detailsTextBox, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tvDirs, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(930, 499);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // detailsTextBox
            // 
            this.detailsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detailsTextBox.Location = new System.Drawing.Point(561, 3);
            this.detailsTextBox.Multiline = true;
            this.detailsTextBox.Name = "detailsTextBox";
            this.detailsTextBox.ReadOnly = true;
            this.detailsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.detailsTextBox.Size = new System.Drawing.Size(366, 493);
            this.detailsTextBox.TabIndex = 6;
            // 
            // tvDirs
            // 
            this.tvDirs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvDirs.ImageIndex = 0;
            this.tvDirs.ImageList = this.imageList1;
            this.tvDirs.Location = new System.Drawing.Point(3, 3);
            this.tvDirs.Name = "tvDirs";
            this.tvDirs.SelectedImageIndex = 0;
            this.tvDirs.Size = new System.Drawing.Size(552, 493);
            this.tvDirs.TabIndex = 5;
            this.tvDirs.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvDirs_AfterSelect);
            this.tvDirs.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvDirs_NodeMouseDoubleClick);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "disk.png");
            this.imageList1.Images.SetKeyName(1, "tape.png");
            this.imageList1.Images.SetKeyName(2, "volume.png");
            this.imageList1.Images.SetKeyName(3, "folder.png");
            this.imageList1.Images.SetKeyName(4, "file.png");
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripButton,
            this.extractToolStripButton,
            this.cancelToolStripButton,
            this.toolStripButton1,
            this.opencatalogToolStripButton,
            this.savecatalogToolStripButton});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(930, 23);
            this.toolStrip1.Stretch = true;
            this.toolStrip1.TabIndex = 0;
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(125, 20);
            this.openToolStripButton.Text = "&Read Backup File...";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
            // 
            // extractToolStripButton
            // 
            this.extractToolStripButton.Enabled = false;
            this.extractToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("extractToolStripButton.Image")));
            this.extractToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.extractToolStripButton.Name = "extractToolStripButton";
            this.extractToolStripButton.Size = new System.Drawing.Size(88, 20);
            this.extractToolStripButton.Text = "&Extract To...";
            this.extractToolStripButton.Click += new System.EventHandler(this.extractToolStripButton_Click);
            // 
            // cancelToolStripButton
            // 
            this.cancelToolStripButton.Enabled = false;
            this.cancelToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("cancelToolStripButton.Image")));
            this.cancelToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cancelToolStripButton.Name = "cancelToolStripButton";
            this.cancelToolStripButton.Size = new System.Drawing.Size(72, 20);
            this.cancelToolStripButton.Text = "&Cancel...";
            this.cancelToolStripButton.Click += new System.EventHandler(this.cancelToolStripButton_Click);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(6, 23);
            // 
            // opencatalogToolStripButton
            // 
            this.opencatalogToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.opencatalogToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("opencatalogToolStripButton.Image")));
            this.opencatalogToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.opencatalogToolStripButton.Name = "opencatalogToolStripButton";
            this.opencatalogToolStripButton.Size = new System.Drawing.Size(93, 19);
            this.opencatalogToolStripButton.Text = "Open Catalog...";
            this.opencatalogToolStripButton.Click += new System.EventHandler(this.opencatalogToolStripButton_Click);
            // 
            // savecatalogToolStripButton
            // 
            this.savecatalogToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.savecatalogToolStripButton.Enabled = false;
            this.savecatalogToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("savecatalogToolStripButton.Image")));
            this.savecatalogToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.savecatalogToolStripButton.Name = "savecatalogToolStripButton";
            this.savecatalogToolStripButton.Size = new System.Drawing.Size(88, 19);
            this.savecatalogToolStripButton.Text = "Save Catalog...";
            this.savecatalogToolStripButton.Click += new System.EventHandler(this.savecatalogToolStripButton_Click);
            // 
            // ofdCatalog
            // 
            this.ofdCatalog.DefaultExt = "*.bkf";
            this.ofdCatalog.Filter = "Catalog Files (*.cat)|*.cat|All Files|*.*";
            this.ofdCatalog.FileOk += new System.ComponentModel.CancelEventHandler(this.ofdCatalog_FileOk);
            // 
            // sfdCatalog
            // 
            this.sfdCatalog.DefaultExt = "cat";
            this.sfdCatalog.Filter = "Catalog Files (*.cat)|*.cat|All Files|*.*";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 544);
            this.Controls.Add(this.toolStripContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Backup Reader";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMain_FormClosed);
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog ofdBackup;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ToolStripButton extractToolStripButton;
        private System.Windows.Forms.FolderBrowserDialog fbdBackup;
        private System.Windows.Forms.ToolStripButton cancelToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripButton1;
        private System.Windows.Forms.ToolStripButton opencatalogToolStripButton;
        private System.Windows.Forms.ToolStripButton savecatalogToolStripButton;
        private System.Windows.Forms.OpenFileDialog ofdCatalog;
        private System.Windows.Forms.SaveFileDialog sfdCatalog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox detailsTextBox;
        private System.Windows.Forms.TreeView tvDirs;
    }
}

