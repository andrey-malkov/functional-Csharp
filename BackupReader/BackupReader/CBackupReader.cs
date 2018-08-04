
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
        public static CCatalogNode ReadBackup(IEnumerable<CDescriptorBlock> descriptorBlocks)
        {
            CCatalogNode node = null;
            CCatalogNode lastSetNode = null;
            CCatalogNode lastVolumeNode = null;
            CCatalogNode lastFolderNode = null;

            descriptorBlocks.Where(block => (!(block is CEndOfTapeMarkerDescriptorBlock))).ToList()
                .ForEach(block =>
                {
                    switch (block)
                    {
                        case CTapeHeaderDescriptorBlock tapeHeaderDescriptorBlock:
                            node = new CCatalogNode(tapeHeaderDescriptorBlock, tapeHeaderDescriptorBlock.MediaName, ENodeType.Root);
                            break;
                        case CStartOfDataSetDescriptorBlock dataSetDescriptorBlock:
                            lastSetNode = node.AddSet(dataSetDescriptorBlock);
                            break;
                        case CVolumeDescriptorBlock volumeDescriptorBlock:
                            lastVolumeNode = lastSetNode.AddVolume(volumeDescriptorBlock);
                            break;
                        case CDatabaseDescriptorBlock databaseDescriptorBlock:
                            lastVolumeNode.AddDatabase(databaseDescriptorBlock);
                            break;
                        case CDirectoryDescriptorBlock directoryDescriptorBlock:
                            CCatalogNode directoryNode = null;
                            directoryDescriptorBlock.DirectoriesName.ToList()
                                .ForEach(f => directoryNode = lastVolumeNode.AddFolder(directoryDescriptorBlock, f));

                            if (directoryNode != null) lastFolderNode = directoryNode;
                            break;
                        case CFileDescriptorBlock fileDescriptorBlock:
                            fileDescriptorBlock.FilesName.ToList()
                                .ForEach(f => lastFolderNode.AddFile(fileDescriptorBlock, f));
                            break;
                    }
                });

            return node;
        }
    }

}
