using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace BackupReader
{
    class CatalogNode
    {
        public string Name { get; set; }
        public CDescriptorBlock DescriptorBlock { get; set; }
        public ENodeType Type { get; set; }
        public long Offset { get; set; }
        public List<CatalogNode> Nodes { get; set; }

        public string GetDetailsString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Name);
            builder.AppendLine();
            builder.AppendLine(DescriptorBlock.GetDetailsString());

            return builder.ToString();
        }
    }

    class RootCatalogNode : CatalogNode
    {
        public RootCatalogNode(CTapeHeaderDescriptorBlock tapeHeaderDescriptorBlock)
        {
            DescriptorBlock = tapeHeaderDescriptorBlock;
            Type = ENodeType.Root;
            Name = tapeHeaderDescriptorBlock.MediaName;
            Offset = tapeHeaderDescriptorBlock.StartPosition;
        }
    }

    class SetCatalogNode : CatalogNode
    {
        public SetCatalogNode(CStartOfDataSetDescriptorBlock dataSetDescriptorBlock)
        {
            DescriptorBlock = dataSetDescriptorBlock;
            Name = "Set: " + dataSetDescriptorBlock.DataSetNumber + " - " + dataSetDescriptorBlock.DataSetName;
            Type = ENodeType.Set;
        }
    }

    class VolumeCatalogNode : CatalogNode
    {
        public VolumeCatalogNode(CVolumeDescriptorBlock volumeDescriptorBlock)
        {
            DescriptorBlock = volumeDescriptorBlock;
            Name = volumeDescriptorBlock.DeviceName;
            Type = ENodeType.Volume;
        }
    }

    class DirectoryCatalogNode : CatalogNode
    {
        public DirectoryCatalogNode(CDirectoryDescriptorBlock directoryDescriptorBlock, string folderName)
        {
            DescriptorBlock = directoryDescriptorBlock;
            Name = folderName;
            Type = ENodeType.Folder;
        }
    }

    class FileCatalogNode : CatalogNode
    {
        public FileCatalogNode(CFileDescriptorBlock fileDescriptorBlock, string fileName)
        {
            DescriptorBlock = fileDescriptorBlock;
            Name = fileName;
            Type = ENodeType.File;
        }
    }

    class DBCatalogNode : CatalogNode
    {
        public DBCatalogNode(CDatabaseDescriptorBlock databaseDescriptorBlock)
        {
            DescriptorBlock = databaseDescriptorBlock;
            Name = "Database - not yet implemented";
            Type = ENodeType.Database;
        }
    }

    public delegate void ProgressChange(long length, long currentPosition);

    static class Catalog
    {
        /// <summary>
        /// Reads the catalog from the disk.
        /// </summary>
        public static List<CatalogNode> Read(string filename, ProgressChange onProgressChange, CancellationToken cancelToken)
        {
            var stream = new CBackupStream(filename);

            try
            {
                return stream.ReadBlocks(onProgressChange, cancelToken)
                    .Where(block => block.type != EBlockType.MTF_EOTM)
                    .SelectMany(block => GetNodesByType(block.type, block.block))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static List<CatalogNode> InvokeOneByType(this Dictionary<EBlockType, Func<CDescriptorBlock, List<CatalogNode>>> factories, EBlockType eBlockType, CDescriptorBlock block)
        {
            Func<CDescriptorBlock, List<CatalogNode>> factory;
            Func<CDescriptorBlock, List<CatalogNode>> defaultFactory = x => new List<CatalogNode>();
            return (factories.TryGetValue(eBlockType, out factory) ? factory : defaultFactory).Invoke(block);
        }

        private static List<CatalogNode> GetNodesByType(EBlockType eBlockType, CDescriptorBlock block)
        {
            var NodesFactories = new Dictionary<EBlockType, Func<CDescriptorBlock, List<CatalogNode>>>();

            NodesFactories[EBlockType.ROOT] = b => new List<CatalogNode>() { new RootCatalogNode((CTapeHeaderDescriptorBlock)b) };
            NodesFactories[EBlockType.MTF_SSET] = b => new List<CatalogNode>() { new SetCatalogNode((CStartOfDataSetDescriptorBlock)b) };
            NodesFactories[EBlockType.MTF_VOLB] = b => new List<CatalogNode>() { new VolumeCatalogNode((CVolumeDescriptorBlock)b) };
            NodesFactories[EBlockType.MTF_DIRB] = b => GetForlders((CDirectoryDescriptorBlock)b).Select(folder => new DirectoryCatalogNode((CDirectoryDescriptorBlock)b, folder)).ToList<CatalogNode>();
            NodesFactories[EBlockType.MTF_FILE] = b => GetFiles((CFileDescriptorBlock)b).Select(file => new FileCatalogNode((CFileDescriptorBlock)b, file)).ToList<CatalogNode>();
            NodesFactories[EBlockType.MTF_DBDB] = b => new List<CatalogNode>() { new DBCatalogNode((CDatabaseDescriptorBlock)block) };

            
            return NodesFactories.InvokeOneByType(eBlockType, block);
        }

        private static List<CatalogNode> CreateNodeByType(EBlockType eBlockType, CDescriptorBlock block)
        {
            var nodes = new List<CatalogNode>();

            switch (eBlockType)
            {
                case EBlockType.ROOT:
                    nodes.Add(new RootCatalogNode((CTapeHeaderDescriptorBlock)block));
                    break;
                case EBlockType.MTF_SSET:
                    nodes.Add(new SetCatalogNode((CStartOfDataSetDescriptorBlock)block));
                    break;
                case EBlockType.MTF_VOLB:
                    nodes.Add(new VolumeCatalogNode((CVolumeDescriptorBlock)block));
                    break;
                case EBlockType.MTF_DIRB:
                    var directoryDescriptorBlock = (CDirectoryDescriptorBlock)block;
                    nodes.AddRange(GetForlders(directoryDescriptorBlock).Select(folder => new DirectoryCatalogNode(directoryDescriptorBlock, folder)));
                    break;
                case EBlockType.MTF_FILE:
                    var fileDescriptorBlock = (CFileDescriptorBlock)block;
                    nodes.AddRange(GetFiles(fileDescriptorBlock).Select(file => new FileCatalogNode(fileDescriptorBlock, file)));
                    break;
                case EBlockType.MTF_DBDB:
                    nodes.Add(new DBCatalogNode((CDatabaseDescriptorBlock)block));
                    break;
                default:
                    break;
            }

            return nodes;
        }

        private static IEnumerable<string> GetFiles(CFileDescriptorBlock fileDescriptorBlock)
        {
            if ((fileDescriptorBlock.FileAttributes & EFileAttributes.FILE_NAME_IN_STREAM_BIT) != 0)
            {
                foreach (var data in fileDescriptorBlock.Streams)
                {
                    if (data.Header.StreamID == "FNAM")
                    {
                        if (fileDescriptorBlock.StringType == EStringType.ANSI)
                        {
                            ASCIIEncoding encoding = new ASCIIEncoding();
                            yield return encoding.GetString(data.Data);
                        }
                        else if (fileDescriptorBlock.StringType == EStringType.Unicode)
                        {
                            UnicodeEncoding encoding = new UnicodeEncoding();
                            yield return encoding.GetString(data.Data);
                        }

                    }
                }
            }
            else
            {
                yield return fileDescriptorBlock.FileName;
            }
        }

        private static IEnumerable<string> GetForlders(CDirectoryDescriptorBlock directoryDescriptorBlock)
        {
            if ((directoryDescriptorBlock.DIRBAttributes & EDIRBAttributes.DIRB_PATH_IN_STREAM_BIT) != 0)
            {
                foreach (CDataStream data in directoryDescriptorBlock.Streams)
                {
                    if (data.Header.StreamID == "PNAM")
                    {
                        if (directoryDescriptorBlock.StringType == EStringType.ANSI)
                        {
                            yield return GetFileName(new ASCIIEncoding(), data);
                        }
                        else if (directoryDescriptorBlock.StringType == EStringType.Unicode)
                        {
                            yield return GetFileName(new UnicodeEncoding(), data);
                        }

                    }
                }
            }
            else
            {
                yield return directoryDescriptorBlock.DirectoryName.Substring(0, directoryDescriptorBlock.DirectoryName.Length - 1);
            }

            string GetFileName<T>(T encoding, CDataStream data) where T : Encoding
            {
                var folderName = encoding.GetString(data.Data);
                return folderName.Substring(0, folderName.Length - 1);
            }
        }

        private static IEnumerable<(EBlockType type, CDescriptorBlock block)> ReadBlocks(this CBackupStream stream, ProgressChange reportProgress, CancellationToken cancelToken)
        {
            var tapeHeaderDescriptorBlock = stream.ReadDBLK();
            var filemarkDescriptorBlock = stream.ReadDBLK();
            yield return (type: EBlockType.ROOT, block: tapeHeaderDescriptorBlock);

            while (stream.BaseStream.Position + 4 < stream.BaseStream.Length)
            {
                EBlockType et = (EBlockType)stream.ReadUInt32();
                stream.BaseStream.Seek(-4, System.IO.SeekOrigin.Current);
                yield return (type: et, block: stream.ReadDBLK());

                cancelToken.ThrowIfCancellationRequested();
                reportProgress(stream.BaseStream.Length, stream.BaseStream.Position);
            }
        }
    }
}
