using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using static BackupReader.CBackupReader;
using System.Linq;

namespace BackupReader
{
    public partial class MainForm : Form
    {
        private string mFileName;
        
        public MainForm()
        {
            InitializeComponent();
        }

        private List<CatalogNode> ReadCatalog(string fileName)
        {
            bool cancel = false;
            cancelToolStripButton.Click += new EventHandler((sender, e) =>
            {
                if (MessageBox.Show(this, "Are you sure you want to cancel?", "Backup Reader!!!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    cancel = true;
            });

            using (var stream = new CBackupStream(fileName))
            {
                long lastPosition = 0;
                var length = stream.BaseStream.Length;
                var increment = length / 100;
                var blocks = stream.ReadBlocks((currentPosition) =>
                {
                    if (currentPosition > lastPosition + increment)
                    {
                        lastPosition = currentPosition;
                        int progress = (int)(currentPosition / (float)length * 100.0f);
                        tsStatus.Text = "Reading backup file. " + progress + "% completed.";
                        Application.DoEvents();
                        Thread.Sleep(60);
                    }
                }, ()=> cancel);

                return ReadBackup(blocks).ToList();
            }
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            if (ofdBackup.ShowDialog() == DialogResult.OK)
            {
                // Open the backup file
                mFileName = ofdBackup.FileName;
                tsStatus.Text = "Reading " + mFileName;

                // UI cues
                openToolStripButton.Enabled = false;
                extractToolStripButton.Enabled = false;
                cancelToolStripButton.Enabled = true;
                opencatalogToolStripButton.Enabled = false;
                savecatalogToolStripButton.Enabled = false;

                // Open and read the catalog
                var catalogNodes = ReadCatalog(mFileName);

                // Populate tree view
                var root = catalogNodes.FirstOrDefault();
                tvDirs.Nodes.Clear();
                tvDirs.Nodes.Add("root", root.Name, 0);
                tvDirs.Nodes[0].Tag = root;
                PopulateTreeView(tvDirs.Nodes[0], catalogNodes);
                tsStatus.Text = "Select a single volume, folder or file to extract.";

                // UI cues
                openToolStripButton.Enabled = true;
                extractToolStripButton.Enabled = false;
                cancelToolStripButton.Enabled = false;
                savecatalogToolStripButton.Enabled = true;
                opencatalogToolStripButton.Enabled = true;
                savecatalogToolStripButton.Enabled = true;
            }
        }

        private void PopulateTreeView(TreeNode TNode, IEnumerable<CatalogNode> flatNodes)
        {
            var parent = (CatalogNode)TNode.Tag;

            flatNodes.SkipWhile(node => node != parent)
                .Skip(1)
                .TakeWhile(node => (int)node.Type > (int)parent.Type)
                .Where(node => (int)node.Type == (int)(parent.Type) + 1)
                .Select(node => new TreeNode(node.Name)
                {
                    ImageIndex = (int)node.Type,
                    SelectedImageIndex = (int)node.Type,
                    Tag = node
                }).ToList()
                .ForEach(tn => {
                    TNode.Nodes.Add(tn);
                    PopulateTreeView(tn, flatNodes);
                });
        }

        private void extractToolStripButton_Click(object sender, EventArgs e)
        {
            if (tvDirs.SelectedNode == null) return;

            // Get the selected catalog node from tree node tag
            CCatalogNode node = (CCatalogNode)tvDirs.SelectedNode.Tag;
            if (node == null) return;
            if ((node.Type == ENodeType.Root) || (node.Type == ENodeType.Set)) return;

            // Get extraction path
            if (fbdBackup.ShowDialog() != DialogResult.OK) return;
            string TargetPath = fbdBackup.SelectedPath;

            // Extract the selected node and child nodes
            using (var stream = new CBackupStream(mFileName))
                node.ExtractTo(stream, TargetPath);
        }

        private void tvDirs_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Get the selected catalog node from tree node tag
            CCatalogNode node = (CCatalogNode)tvDirs.SelectedNode.Tag;
            if (node == null) return;
            if (node.Type != ENodeType.File) return;

            // Extract the selected node to a temporary folder
            string TargetPath = System.IO.Path.GetTempPath();
            using (var stream = new CBackupStream(mFileName))
                node.ExtractTo(stream, TargetPath);

            // Open the file
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(TargetPath + node.Name);
            psi.UseShellExecute = true;
            psi.ErrorDialog = true;
            psi.ErrorDialogParentHandle = this.Handle;
            try
            {
                System.Diagnostics.Process.Start(psi);
            }
            catch(Win32Exception ex)
            {
                MessageBox.Show(this, "Could not open the file '" + node.Name + "'." + ex.ToString(), "Backup Reader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }

        private void tvDirs_AfterSelect(object sender, TreeViewEventArgs e)
        {
            extractToolStripButton.Enabled = false;

            if (tvDirs.SelectedNode == null) return;
            
            var node = (CatalogNode)tvDirs.SelectedNode.Tag;
            if (node == null) return;

            detailsTextBox.Text = node.Details;
            if ((node.Type == ENodeType.Root) || (node.Type == ENodeType.Set)) return;

            extractToolStripButton.Enabled = true;
        }

        private void opencatalogToolStripButton_Click(object sender, EventArgs e)
        {
            if (ofdCatalog.ShowDialog() == DialogResult.Cancel) return;

            // Read the catalog from the file
            tsStatus.Text = "Reading catalog...";
            mFileName = CCatalogNode.ReadBackupFilename(ofdCatalog.FileName);
            CCatalogNode node = CCatalogNode.ReadCatalog(ofdCatalog.FileName);

            // Populate tree view
            tvDirs.Nodes.Clear();
            tvDirs.Nodes.Add("root", node.Name, 0);
            tvDirs.Nodes[0].Tag = node;
            PopulateTreeView(tvDirs.Nodes[0], node);
            tsStatus.Text = "Select a single volume, folder or file to extract.";

            // UI cues
            extractToolStripButton.Enabled = false;
            cancelToolStripButton.Enabled = false;
            savecatalogToolStripButton.Enabled = true;
        }

        private void savecatalogToolStripButton_Click(object sender, EventArgs e)
        {
            if (tvDirs.Nodes.Count == 0) return;
            if (tvDirs.Nodes[0].Tag == null) return;
            if (sfdCatalog.ShowDialog() == DialogResult.Cancel) return;

            // Save the catalog to the file
            CCatalogNode.SaveCatalog(sfdCatalog.FileName, (CCatalogNode)tvDirs.Nodes[0].Tag, mFileName);
        }

        private void ofdCatalog_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}