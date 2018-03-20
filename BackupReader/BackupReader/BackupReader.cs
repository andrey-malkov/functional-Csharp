using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
                nodesList.Add(new CatalogNode()
                {
                    Type = (ENodeType)file.ReadInt32(),
                    Name = file.ReadString(),
                    Offset = file.ReadInt64()
                });
                int count = file.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    ReadSubNodes();
                }
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
