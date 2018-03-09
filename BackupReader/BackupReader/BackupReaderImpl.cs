using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace BackupReader
{
    static class CatalogImpl
    {
        public static IEnumerable<(EBlockType type, CDescriptorBlock block)> ReadBlocks(this CBackupStream stream, ProgressChange reportProgress, CancellationToken cancelToken)
        {
            var tapeHeaderDescriptorBlock = stream.ReadDBLK();
            var filemarkDescriptorBlock = stream.ReadDBLK();
            yield return (type: EBlockType.ROOT, block: tapeHeaderDescriptorBlock);

            while (stream.BaseStream.Position + 4 < stream.BaseStream.Length)
            {
                EBlockType et = (EBlockType)stream.ReadUInt32();
                stream.BaseStream.Seek(-4, SeekOrigin.Current);
                yield return (type: et, block: stream.ReadDBLK());

                cancelToken.ThrowIfCancellationRequested();
                reportProgress(stream.BaseStream.Length, stream.BaseStream.Position);
            }
        }

        public static IEnumerable<CatalogNode> ReadNodes(this CBackupStream file)
        {
            file.ReadString();
            var firstNode = new CatalogNode()
            {
                Type = (ENodeType)file.ReadInt32(),
                Name = file.ReadString(),
                Offset = file.ReadInt64()
            };
            
            foreach (var node in ReadSubNodes(firstNode))
            {
                yield return node;
            }

            IEnumerable<CatalogNode> ReadSubNodes(CatalogNode parentNode)
            {
                yield return parentNode;

                int count = file.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var subNode = new CatalogNode()
                    {
                        Type = (ENodeType)file.ReadInt32(),
                        Name = file.ReadString(),
                        Offset = file.ReadInt64()
                    };

                    foreach (var node in ReadSubNodes(subNode))
                    {
                        yield return node;
                    }
                }
            }

        }

        public static List<CatalogNode> InvokeOneByType(this Dictionary<EBlockType, Func<CDescriptorBlock, List<CatalogNode>>> factories, EBlockType eBlockType, CDescriptorBlock block)
        {
            Func<CDescriptorBlock, List<CatalogNode>> factory;
            Func<CDescriptorBlock, List<CatalogNode>> defaultFactory = x => new List<CatalogNode>();
            return (factories.TryGetValue(eBlockType, out factory) ? factory : defaultFactory).Invoke(block);
        }

        public static IEnumerable<string> GetFiles(CFileDescriptorBlock fileDescriptorBlock)
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

        public static IEnumerable<string> GetForlders(CDirectoryDescriptorBlock directoryDescriptorBlock)
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
    }
}