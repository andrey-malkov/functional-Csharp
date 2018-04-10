using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
//using static BackupReader.Catalog;

namespace BackupReader
{
    public partial class MainForm : Form
    {
        private string mFileName;
        private CBackupReader mBackupReader;
        long mLastPosition = 0;
        CancellationTokenSource mCancellation = new CancellationTokenSource();


        public MainForm()
        {
            InitializeComponent();
        }

        void mFile_OnProgressChange(long length, long currentPosition)
        {
            long increment = length / 100;
            if (currentPosition > mLastPosition + increment)
            {
                int progress = (int)(currentPosition / (float)length * 100.0f);
                tsStatus.Text = "Reading backup file. " + progress + "% completed.";
                Application.DoEvents();
                //Thread.Sleep(1);
            }
        }

        bool mFile_IfCancellationRequested()
        {
            try
            {
                mCancellation.Token.ThrowIfCancellationRequested();
                return false;
            }
            catch (OperationCanceledException)
            {
                tsStatus.Text = "The operation was canceled.";
                return true;
            }
        }


        private void PopulateTreeView(TreeNode TNode, List<CatalogNode> flatNodes)
        {
            TreeNode lastSetNode = null;
            TreeNode lastVolumeNode = null;
            TreeNode lastFolderNode = null;

            foreach (var node in flatNodes)
            {
                TreeNode snode = new TreeNode(node.Name);
                switch (node.Type)
                {
                    case ENodeType.Set:
                        lastSetNode = CreateTreeNode(node, 1);
                        TNode.Nodes.Add(lastSetNode);
                        break;
                    case ENodeType.Volume:
                        lastVolumeNode = CreateTreeNode(node, 2);
                        lastSetNode.Nodes.Add(lastVolumeNode);
                        break;
                    case ENodeType.Folder:
                        lastFolderNode = CreateTreeNode(node, 3);
                        lastVolumeNode.Nodes.Add(lastFolderNode);
                        break;
                    case ENodeType.File:
                        lastFolderNode.Nodes.Add(CreateTreeNode(node, 4));
                        break;
                    default:
                        break;
                }
            }

            TreeNode CreateTreeNode(CatalogNode node, int index)
            {
                return new TreeNode(node.Name)
                {
                    ImageIndex = index,
                    SelectedImageIndex = index,
                    Tag = node
                };
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

                var catalogNodes = BackupReader.ReadBackup(new CBackupStream(mFileName), mFile_OnProgressChange, mFile_IfCancellationRequested);
                if (catalogNodes != null)
                {
                    var root = catalogNodes[0];

                    // Populate tree view
                    tvDirs.Nodes.Clear();
                    tvDirs.Nodes.Add("root", root.Name, 0);
                    tvDirs.Nodes[0].Tag = root;
                    PopulateTreeView(tvDirs.Nodes[0], catalogNodes.GetRange(1, catalogNodes.Count - 1));
                }
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

        private void PopulateTreeView(TreeNode TNode, CCatalogNode CNode)
        {
            foreach (CCatalogNode node in CNode.Children)
            {
                TreeNode snode = new TreeNode(node.Name);
                if (node.Type == ENodeType.Set)
                {
                    snode.ImageIndex = 1;
                    snode.SelectedImageIndex = 1;
                    snode.Tag = node;
                    TNode.Nodes.Add(snode);
                }
                else if (node.Type == ENodeType.Volume)
                {
                    snode.ImageIndex = 2;
                    snode.SelectedImageIndex = 2;
                    snode.Tag = node;
                    TNode.Nodes.Add(snode);
                }
                else if (node.Type == ENodeType.Folder)
                {
                    snode.ImageIndex = 3;
                    snode.SelectedImageIndex = 3;
                    snode.Tag = node;
                    TNode.Nodes.Add(snode);
                }
                else if (node.Type == ENodeType.File)
                {
                    snode.ImageIndex = 4;
                    snode.SelectedImageIndex = 4;
                    snode.Tag = node;
                    TNode.Nodes.Add(snode);
                }
                else if (node.Type == ENodeType.Database)
                {
                    snode.ImageIndex = 3;
                    snode.SelectedImageIndex = 3;
                    snode.Tag = node;
                    TNode.Nodes.Add(snode);
                }
                PopulateTreeView(snode, node);
            }
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
            node.ExtractTo(mBackupReader, TargetPath);
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
            node.ExtractTo(mBackupReader, TargetPath);

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
            if (mBackupReader != null) mBackupReader.Close();
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to cancel?", "Backup Reader", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                mCancellation.Cancel();
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

            tsStatus.Text = "Reading catalog...";
            var catalogNodes = BackupReader.ReadCatalog(ofdCatalog.FileName);
            var root = catalogNodes[0];

            tvDirs.Nodes.Clear();
            tvDirs.Nodes.Add("root", root.Name, 0);
            tvDirs.Nodes[0].Tag = root;
            PopulateTreeView(tvDirs.Nodes[0], catalogNodes.GetRange(1, catalogNodes.Count - 1));
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