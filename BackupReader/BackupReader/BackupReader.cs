using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static BackupReader.CatalogImpl;

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
                .SelectMany(block => GetNodesByType(block.type, block.data))
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
        public static List<CatalogNode> ReadCatalog(string filename, Action<long, long> onProgressChange, CancellationToken cancelToken)
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
            return GetNodeFactory().Invoke(block);

            Func<CDescriptorBlock, List<CatalogNode>> GetNodeFactory()
            {
                Func<CDescriptorBlock, List<CatalogNode>> factory;
                return new Dictionary<EBlockType, Func<CDescriptorBlock, List<CatalogNode>>>()
                {
                    { EBlockType.ROOT, b => new List<CatalogNode>() { new RootCatalogNode((CTapeHeaderDescriptorBlock)b) } },
                    { EBlockType.MTF_SSET, b => new List<CatalogNode>() { new SetCatalogNode((CStartOfDataSetDescriptorBlock)b) } },
                    { EBlockType.MTF_VOLB, b => new List<CatalogNode>() { new VolumeCatalogNode((CVolumeDescriptorBlock)b) } },
                    { EBlockType.MTF_DBDB, b => new List<CatalogNode>() { new DBCatalogNode((CDatabaseDescriptorBlock)block) } },
                    { EBlockType.MTF_DIRB, b => GetForlders((CDirectoryDescriptorBlock)b).Select(folder => new DirectoryCatalogNode((CDirectoryDescriptorBlock)b, folder)).ToList<CatalogNode>() },
                    { EBlockType.MTF_FILE, b => GetFiles((CFileDescriptorBlock)b).Select(file => new FileCatalogNode((CFileDescriptorBlock)b, file)).ToList<CatalogNode>() }
                }.TryGetValue(eBlockType, out factory) ? factory : x => new List<CatalogNode>();
            }
        }
    }
}
