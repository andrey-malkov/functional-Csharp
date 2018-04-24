using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BackupReader
{
    static class BackupReader
    {
        /// <summary>
        /// Reads the backup (.bkf) from the disk.
        /// </summary>
        public static List<CatalogNode> ReadBackup(CBackupStream backupStream, Action<long, long> onProgressChange, Func<bool> ifCancellationRequested)
        {
            return backupStream.ReadBlocks(ShouldContinue)
                .Where(block => (block.type != EBlockType.MTF_EOTM))
                .SelectMany(block => block.data.ToCatalogNodes())
                .ToList();

            bool ShouldContinue(long length, long currentPosition)
            {
                onProgressChange(length, currentPosition);
                return !ifCancellationRequested();
            }
        }

        /// <summary>
        /// Reads the catalog (.cat) from the disk.
        /// </summary>
        public static List<CatalogNode> ReadCatalog(string filename)
        {
            var file = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read));
            file.ReadString();
            var nodesList = new List<CatalogNode>();
            ReadSubNodes();
            file.Close();
            return nodesList;

            void ReadSubNodes()
            {
                nodesList.Add(new CatalogNode((ENodeType)file.ReadInt32(), file.ReadString(), file.ReadInt64()));
                int count = file.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    ReadSubNodes();
                }
            }
        }

        public static void ExtractCatalog(List<CatalogNode> nodes, string targetPath)
        {
            var parentNode = nodes[0];
            var parentNodePath = parentNode.DescriptorBlock.Dump(null, targetPath);
            PopulateFileSystem(parentNode, parentNodePath);

            void PopulateFileSystem(CatalogNode parent, string parentPath)
            {
                nodes.SkipWhile(node => node != parent)
                    .Skip(1)
                    .TakeWhile(node => (int)node.Type > (int)parent.Type)
                    .Where(node => (int)node.Type == (int)(parent.Type) + 1)
                    .ToList()
                    .ForEach(node => {
                        var path = node.DescriptorBlock.Dump(parent.DescriptorBlock, parentPath);
                        PopulateFileSystem(node, path);
                    });
            }
        }

        /// <summary>
        /// Saves the catalog to the disk.
        /// </summary>
        public static void SaveCatalog(string Filename, List<CatalogNode> Nodes, string BackupFilename)
        {
            BinaryWriter file = new BinaryWriter(new FileStream(Filename, FileMode.Create, FileAccess.Write));

            // Write full path to backup file
            file.Write(BackupFilename);

            // Write nodes
            Nodes.ForEach(node => {
                file.Write((int)node.Type);
                file.Write(node.Name);
                file.Write(node.Offset);
            });

            file.Close();
        }
    }
}
