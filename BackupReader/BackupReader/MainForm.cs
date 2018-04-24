using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Linq;
//using static BackupReader.Catalog;

namespace BackupReader
{
    public partial class MainForm : Form
    {
        private string mFileName;
        long mLastPosition = 0;
        CancellationTokenSource mCancellation = new CancellationTokenSource();


        public MainForm()
        {
            InitializeComponent();
        }

        private void mFile_OnProgressChange(long length, long currentPosition)
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

        private bool mFile_IfCancellationRequested()
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
                    PopulateTreeView(tvDirs.Nodes[0], catalogNodes);
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

        private static void PopulateTreeView(TreeNode TNode, List<CatalogNode> flatNodes)
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

        private static List<CatalogNode> GetAllNodes(TreeNode treeNode)
        {
            var nodes = new List<CatalogNode>() { (CatalogNode)treeNode.Tag };
            nodes.AddRange(treeNode.Nodes.Cast<TreeNode>().SelectMany<TreeNode, CatalogNode>(tn => GetNodeBranch(tn)).ToList());
            return nodes;

            IEnumerable<CatalogNode> GetNodeBranch(TreeNode tn)
            {
                yield return (CatalogNode)tn.Tag;

                foreach (TreeNode child in tn.Nodes)
                    foreach (var childChild in GetNodeBranch(child))
                        yield return childChild;
            }
        }

        private void extractToolStripButton_Click(object sender, EventArgs e)
        {
            if (tvDirs.SelectedNode == null) return;

            // Get the selected catalog node from tree node tag
            CatalogNode node = (CatalogNode)tvDirs.SelectedNode.Tag;
            if (node == null) return;
            if ((node.Type == ENodeType.Root) || (node.Type == ENodeType.Set)) return;

            // Get extraction path
            if (fbdBackup.ShowDialog() != DialogResult.OK) return;
            string TargetPath = fbdBackup.SelectedPath;

            // Extract the selected node and child nodes
            BackupReader.ExtractCatalog(GetAllNodes(tvDirs.SelectedNode), TargetPath);
        }

        private void tvDirs_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Get the selected catalog node from tree node tag
            CatalogNode node = (CatalogNode)tvDirs.SelectedNode.Tag;
            if (node == null) return;
            if (node.Type != ENodeType.File) return;

            // Extract the selected node to a temporary folder
            string TargetPath = System.IO.Path.GetTempPath();
            BackupReader.ExtractCatalog(GetAllNodes(tvDirs.SelectedNode), TargetPath);

            // Open the file
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(TargetPath + node.Name);
            psi.UseShellExecute = true;
            psi.ErrorDialog = true;
            psi.ErrorDialogParentHandle = this.Handle;
            try
            {
                System.Diagnostics.Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(this, "Could not open the file '" + node.Name + "'." + ex.ToString(), "Backup Reader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            
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
            BackupReader.SaveCatalog(sfdCatalog.FileName, GetAllNodes(tvDirs.SelectedNode), mFileName);
        }

        private void ofdCatalog_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}