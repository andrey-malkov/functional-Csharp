
using System;
using System.Linq;

namespace BackupReader
{
    /// <summary>
    /// Represents a backup file reader.
    /// </summary>
    class CBackupReader
    {
        private long mLastPos;
        private long mIncrement;
        private bool mCancel;
        private CBackupStream mStream;

        /// <summary>
        /// Returns the underlying stream.
        /// </summary>
        public CBackupStream Stream
        {
            get { return mStream; }
        }
	 
        /// <summary>
        /// Reads the entire backup file and returns a root catalog node.
        /// The root node contains backup sets/volumes/directories/files
        /// as child nodes.
        /// </summary>
        public CCatalogNode ReadCatalog(Action<long, long> onProgressChange)
        {
            // Set to true to cancel reading
            mCancel = false;

            CCatalogNode node = null;
            CCatalogNode lastSetNode = null;
            CCatalogNode lastVolumeNode = null;
            CCatalogNode lastFolderNode = null;

            mStream.ReadBlocks(onProgressChange).Where(block => (!(block.data is CEndOfTapeMarkerDescriptorBlock))).ToList()
                .ForEach(block =>
                {
                    switch (block.data)
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

        /// <summary>
        /// Stops reading the catalog. The nodes that has already been read will still be available.
        /// </summary>
        public void CancelRead()
        {
            mCancel = true;
        }

        /// <summary>
        /// Opens a backup file.
        /// </summary>
        public void Open(string filename)
        {
            mStream = new CBackupStream(filename);
            mIncrement = mStream.BaseStream.Length / 100;
            mLastPos = 0;
            mCancel = false;
        }

        /// <summary>
        /// Closes the backup file.
        /// </summary>
        public void Close()
        {
            mStream.Close();
        }

        public CBackupReader()
        {
        }

        public CBackupReader(string filename)
        {
            Open(filename);
        }

        ~CBackupReader()
        {
            Close();
        }
    }

}
