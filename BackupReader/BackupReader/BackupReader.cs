using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static BackupReader.CatalogImpl;

namespace BackupReader
{
    public delegate void ProgressChange(long length, long currentPosition);

    static class BackupReader
    {
        /// <summary>
        /// Reads the backup (.bkf) from the disk.
        /// </summary>
        public static List<CatalogNode> ReadBackup(string filename, ProgressChange onProgressChange, CancellationToken cancelToken)
        {
            try
            {
                return new CBackupStream(filename).ReadBlocks(onProgressChange, cancelToken)
                    .Where(block => (block.type != EBlockType.MTF_EOTM) || (block.type != 0))
                    .SelectMany(block => GetNodesByType(block.type, block.block))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the catalog (.cat) from the disk.
        /// </summary>
        public static List<CatalogNode> ReadCatalog(string filename, ProgressChange onProgressChange, CancellationToken cancelToken)
        {
            try
            {
                return new CBackupStream(filename).ReadNodes().ToList();
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static List<CatalogNode> GetNodesByType(EBlockType eBlockType, CDescriptorBlock block)
        {
            var NodesFactories = new Dictionary<EBlockType, Func<CDescriptorBlock, List<CatalogNode>>>();

            NodesFactories[EBlockType.ROOT] = b => new List<CatalogNode>() { new RootCatalogNode((CTapeHeaderDescriptorBlock)b) };
            NodesFactories[EBlockType.MTF_SSET] = b => new List<CatalogNode>() { new SetCatalogNode((CStartOfDataSetDescriptorBlock)b) };
            NodesFactories[EBlockType.MTF_VOLB] = b => new List<CatalogNode>() { new VolumeCatalogNode((CVolumeDescriptorBlock)b) };
            NodesFactories[EBlockType.MTF_DBDB] = b => new List<CatalogNode>() { new DBCatalogNode((CDatabaseDescriptorBlock)block) };
            NodesFactories[EBlockType.MTF_DIRB] = b => GetForlders((CDirectoryDescriptorBlock)b).Select(folder => new DirectoryCatalogNode((CDirectoryDescriptorBlock)b, folder)).ToList<CatalogNode>();
            NodesFactories[EBlockType.MTF_FILE] = b => GetFiles((CFileDescriptorBlock)b).Select(file => new FileCatalogNode((CFileDescriptorBlock)b, file)).ToList<CatalogNode>();

            
            return NodesFactories.InvokeOneByType(eBlockType, block);
        }
    }
}
