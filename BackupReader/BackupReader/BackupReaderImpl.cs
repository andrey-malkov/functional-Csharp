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
        public static IEnumerable<(EBlockType type, CDescriptorBlock block)> ReadBlocks(CBackupStream stream, Action<long, long> reportProgress)
        {
            var tapeHeaderDescriptorBlock = stream.ReadDBLK();
            var filemarkDescriptorBlock = stream.ReadDBLK();
            yield return (type: EBlockType.ROOT, block: tapeHeaderDescriptorBlock);

            foreach (var x in ReadBlocksRecursive())
            {
                yield return x;

                reportProgress(stream.BaseStream.Length, stream.BaseStream.Position);
            }

            IEnumerable<(EBlockType type, CDescriptorBlock block)> ReadBlocksRecursive()
            {
                var blockType = stream.PeekNextBlockType();

                if (blockType != 0)
                    yield return (type: blockType, block: stream.ReadDBLK());
                else
                    yield break;

                foreach (var nextBlock in ReadBlocksRecursive())
                {
                    yield return nextBlock;
                }
            }
        }

        public static List<CatalogNode> ReadNodes(this CBackupStream file)
        {
            file.ReadString();
            var nodesList = new List<CatalogNode> { new CatalogNode()
                {
                    Type = (ENodeType)file.ReadInt32(),
                    Name = file.ReadString(),
                    Offset = file.ReadInt64()
                }
            };

            ReadSubNodes(nodesList);
            return nodesList;

            void ReadSubNodes(List<CatalogNode> list)
            {
                int count = file.ReadInt32();
                list.Add( new CatalogNode()
                {
                    Type = (ENodeType)file.ReadInt32(),
                    Name = file.ReadString(),
                    Offset = file.ReadInt64()
                });

                for (int i = 0; i < count; i++)
                {
                    ReadSubNodes(list);
                }
            }
        }

        public static IEnumerable<CatalogNode> _ReadNodes(this CBackupStream file)
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