
using System;
using System.Collections.Generic;
using System.Linq;

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
    }

}
