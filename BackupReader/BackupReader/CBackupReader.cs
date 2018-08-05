
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BackupReader
{
    /// <summary>
    /// Represents a backup file reader.
    /// </summary>
    static class CBackupReader
    {
        /// <summary>
        /// Reads the entire backup file and returns a root catalog node.
        /// The root node contains backup sets/volumes/directories/files
        /// as child nodes.
        /// </summary>
        public static IEnumerable<CatalogNode> ReadBackup(IEnumerable<CDescriptorBlock> descriptorBlocks)
        {
            return descriptorBlocks
                .Where(block => (!(block is CEndOfTapeMarkerDescriptorBlock)))
                .SelectMany(block => toCatalogNode(block));

            IEnumerable<CatalogNode> toCatalogNode(CDescriptorBlock block)
            {
                switch(block)
                {
                    case CTapeHeaderDescriptorBlock tapeHeaderDescriptorBlock:
                        return new List<CatalogNode>() { new RootCatalogNode(tapeHeaderDescriptorBlock) };
                    case CStartOfDataSetDescriptorBlock dataSetDescriptorBlock:
                        return new List<CatalogNode>() { new SetCatalogNode(dataSetDescriptorBlock) };
                    case CVolumeDescriptorBlock volumeDescriptorBlock:
                        return new List<CatalogNode>() { new VolumeCatalogNode(volumeDescriptorBlock) };
                    case CDatabaseDescriptorBlock databaseDescriptorBlock:
                        return new List<CatalogNode>() { new DBCatalogNode(databaseDescriptorBlock) };
                    case CDirectoryDescriptorBlock directoryDescriptorBlock:
                        return directoryDescriptorBlock.DirectoriesName
                            .Select(d => new DirectoryCatalogNode(directoryDescriptorBlock, d));
                    case CFileDescriptorBlock fileDescriptorBlock:
                        return fileDescriptorBlock.FilesName
                            .Select(f => new FileCatalogNode(fileDescriptorBlock, f));
                    default:
                        return new List<CatalogNode>();
                }
            };
        }

        public static void ExtractCatalog(List<CatalogNode> nodes, string targetPath)
        {
            var rootNode = nodes[0];
            var rootNodePath = rootNode.Extract(null, targetPath);
            PopulateFileSystem(rootNode, rootNodePath);

            void PopulateFileSystem(CatalogNode parent, string parentPath)
            {
                nodes.SkipWhile(node => node != parent)
                    .Skip(1)
                    .TakeWhile(node => (int)node.Type > (int)parent.Type)
                    .Where(node => (int)node.Type == (int)(parent.Type) + 1)
                    .ToList()
                    .ForEach(node => {
                        var path = node.Extract(parent.DescriptorBlock, parentPath);
                        PopulateFileSystem(node, path);
                    });
            }
        }
    }

}
