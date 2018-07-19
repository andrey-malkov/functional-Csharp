
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
        /// Provides an event handler for the OnProgressChange event. 
        /// Progress is an integer between 1-100, representing the progress of
        /// the catalog read operation.
        /// </summary>
        public delegate void ProgressChange(int Progress);
        /// <summary>
        /// Occurs when the catalog read progress changes by 1%.
        /// </summary>
        public event ProgressChange OnProgressChange;

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
        public CCatalogNode ReadCatalog()
        {
            // Set to true to cancel reading
            mCancel = false;

            CCatalogNode node = null;
            CCatalogNode lastSetNode = null;
            CCatalogNode lastVolumeNode = null;
            CCatalogNode lastFolderNode = null;

            mStream.ReadBlocks().Where(block => (block.type != EBlockType.MTF_EOTM)).ToList().ForEach(block =>
            {
                if (block.type == EBlockType.MTF_TAPE)
                {
                    var tapeHeaderDescriptorBlock = (CTapeHeaderDescriptorBlock)block.data;
                    node = new CCatalogNode(tapeHeaderDescriptorBlock, tapeHeaderDescriptorBlock.MediaName, ENodeType.Root);
                }
                else if (block.type == EBlockType.MTF_SSET)
                {
                    var dataSetDescriptorBlock = (CStartOfDataSetDescriptorBlock)block.data;
                    var cnode = node.AddSet(dataSetDescriptorBlock);
                    lastSetNode = cnode;
                }
                else if (block.type == EBlockType.MTF_VOLB)
                {
                    var volumeDescriptorBlock = (CVolumeDescriptorBlock)block.data;
                    var cnode = lastSetNode.AddVolume(volumeDescriptorBlock);
                    lastVolumeNode = cnode;
                }
                else if (block.type == EBlockType.MTF_DIRB)
                {
                    var directoryDescriptorBlock = (CDirectoryDescriptorBlock)block.data;

                    // Check if the directory name is contained in a data stream
                    CCatalogNode cnode = null;
                    if ((directoryDescriptorBlock.DIRBAttributes & EDIRBAttributes.DIRB_PATH_IN_STREAM_BIT) != 0)
                    {
                        foreach (CDataStream data in directoryDescriptorBlock.Streams)
                        {
                            if (data.Header.StreamID == "PNAM")
                            {
                                if (directoryDescriptorBlock.StringType == EStringType.ANSI)
                                {
                                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                                    var folderName = encoding.GetString(data.Data);
                                    folderName = folderName.Substring(0, folderName.Length - 1);
                                    cnode = lastVolumeNode.AddFolder(directoryDescriptorBlock, folderName);
                                }
                                else if (directoryDescriptorBlock.StringType == EStringType.Unicode)
                                {
                                    System.Text.UnicodeEncoding encoding = new System.Text.UnicodeEncoding();
                                    var folderName = encoding.GetString(data.Data);
                                    folderName = folderName.Substring(0, folderName.Length - 1);
                                    cnode = lastVolumeNode.AddFolder(directoryDescriptorBlock, folderName);
                                }

                            }
                        }
                    }
                    else
                    {
                        var folderName = directoryDescriptorBlock.DirectoryName.Substring(0, directoryDescriptorBlock.DirectoryName.Length - 1);
                        cnode = lastVolumeNode.AddFolder(directoryDescriptorBlock, folderName);
                    }

                    if (cnode != null) lastFolderNode = cnode;
                }
                else if (block.type == EBlockType.MTF_FILE)
                {
                    var fileDescriptorBlock = (CFileDescriptorBlock)block.data;

                    // Check if the file name is contained in a data stream
                    CCatalogNode cnode = null;
                    if ((fileDescriptorBlock.FileAttributes & EFileAttributes.FILE_NAME_IN_STREAM_BIT) != 0)
                    {
                        foreach (var data in fileDescriptorBlock.Streams)
                        {
                            if (data.Header.StreamID == "FNAM")
                            {
                                if (fileDescriptorBlock.StringType == EStringType.ANSI)
                                {
                                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                                    var fileName = encoding.GetString(data.Data);
                                    lastFolderNode.AddFile(fileDescriptorBlock, fileName);
                                }
                                else if (fileDescriptorBlock.StringType == EStringType.Unicode)
                                {
                                    System.Text.UnicodeEncoding encoding = new System.Text.UnicodeEncoding();
                                    var fileName = encoding.GetString(data.Data);
                                    lastFolderNode.AddFile(fileDescriptorBlock, fileName);
                                }

                            }
                        }
                    }
                    else
                    {
                        lastFolderNode.AddFile(fileDescriptorBlock, fileDescriptorBlock.FileName);
                    }
                }
                else if (block.type == EBlockType.MTF_DBDB)
                {
                    var databaseDescriptorBlock = (CDatabaseDescriptorBlock)block.data;
                    var cnode = lastVolumeNode.AddDatabase(databaseDescriptorBlock);
                    //lastVolumeNode = cnode;
                }


                // Check progress
                if (mStream.BaseStream.Position > mLastPos + mIncrement)
                {
                    mLastPos = mStream.BaseStream.Position;
                    OnProgressChange((int)(mLastPos / (float)mStream.BaseStream.Length * 100.0f));
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
