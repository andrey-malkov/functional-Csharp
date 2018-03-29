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
    }
}
