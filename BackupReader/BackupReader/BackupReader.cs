﻿using System;
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
                    .SelectMany(block => CreateNodeByType(block.type, block.block))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static List<CatalogNode> CreateNodeByType(EBlockType eBlockType, CDescriptorBlock block)
        {
            var nodes = new List<CatalogNode>();

            switch (eBlockType)
            {
                case EBlockType.ROOT:
                    var tapeHeaderDescriptorBlock = (CTapeHeaderDescriptorBlock)block;
                    nodes.Add(new CatalogNode
                    {
                        DescriptorBlock = tapeHeaderDescriptorBlock,
                        Type = ENodeType.Root,
                        Name = tapeHeaderDescriptorBlock.MediaName,
                        Offset = tapeHeaderDescriptorBlock.StartPosition
                    });
                    break;
                case EBlockType.MTF_SSET:
                    var dataSetDescriptorBlock = (CStartOfDataSetDescriptorBlock)block;
                    nodes.Add(new CatalogNode
                    {
                        DescriptorBlock = dataSetDescriptorBlock,
                        Name = "Set: " + dataSetDescriptorBlock.DataSetNumber + " - " + dataSetDescriptorBlock.DataSetName,
                        Type = ENodeType.Set
                    });
                    break;
                case EBlockType.MTF_VOLB:
                    var volumeDescriptorBlock = (CVolumeDescriptorBlock)block;
                    nodes.Add(new CatalogNode
                    {
                        DescriptorBlock = volumeDescriptorBlock,
                        Name = volumeDescriptorBlock.DeviceName,
                        Type = ENodeType.Volume
                    });
                    break;
                case EBlockType.MTF_DIRB:
                    var directoryDescriptorBlock = (CDirectoryDescriptorBlock)block;
                    nodes.AddRange(GetForlders(directoryDescriptorBlock).Select(folder => new CatalogNode
                    {
                        DescriptorBlock = directoryDescriptorBlock,
                        Name = folder,
                        Type = ENodeType.Folder
                    }));
                    break;
                case EBlockType.MTF_FILE:
                    var fileDescriptorBlock = (CFileDescriptorBlock)block;
                    nodes.AddRange(GetFiles(fileDescriptorBlock).Select(file => new CatalogNode
                    {
                        DescriptorBlock = fileDescriptorBlock,
                        Name = file,
                        Type = ENodeType.File
                    }));
                    break;
                case EBlockType.MTF_DBDB:
                    var databaseDescriptorBlock = (CDatabaseDescriptorBlock)block;
                    nodes.Add(new CatalogNode
                    {
                        DescriptorBlock = databaseDescriptorBlock,
                        Name = "Database - not yet implemented",
                        Type = ENodeType.Database
                    });
                    break;
                default:
                    break;
            }

            return nodes;

            List<string> GetFiles(CFileDescriptorBlock fileDescriptorBlock)
            {
                var files = new List<string>();

                if ((fileDescriptorBlock.FileAttributes & EFileAttributes.FILE_NAME_IN_STREAM_BIT) != 0)
                {
                    foreach (var data in fileDescriptorBlock.Streams)
                    {
                        if (data.Header.StreamID == "FNAM")
                        {
                            if (fileDescriptorBlock.StringType == EStringType.ANSI)
                            {
                                ASCIIEncoding encoding = new ASCIIEncoding();
                                files.Add(encoding.GetString(data.Data));
                            }
                            else if (fileDescriptorBlock.StringType == EStringType.Unicode)
                            {
                                UnicodeEncoding encoding = new UnicodeEncoding();
                                files.Add(encoding.GetString(data.Data));
                            }

                        }
                    }
                }
                else
                {
                    files.Add(fileDescriptorBlock.FileName);
                }

                return files;
            }

            List<string> GetForlders(CDirectoryDescriptorBlock directoryDescriptorBlock)
            {
                var folders = new List<string>();

                if ((directoryDescriptorBlock.DIRBAttributes & EDIRBAttributes.DIRB_PATH_IN_STREAM_BIT) != 0)
                {
                    foreach (CDataStream data in directoryDescriptorBlock.Streams)
                    {
                        if (data.Header.StreamID == "PNAM")
                        {
                            if (directoryDescriptorBlock.StringType == EStringType.ANSI)
                            {
                                folders.Add(GetFileName(new ASCIIEncoding(), data));
                            }
                            else if (directoryDescriptorBlock.StringType == EStringType.Unicode)
                            {
                                folders.Add(GetFileName(new UnicodeEncoding(), data));
                            }

                        }
                    }
                }
                else
                {
                    folders.Add(directoryDescriptorBlock.DirectoryName.Substring(0, directoryDescriptorBlock.DirectoryName.Length - 1));
                }

                return folders;

                string GetFileName<T>(T encoding, CDataStream data) where T : Encoding
                {
                    var folderName = encoding.GetString(data.Data);
                    return folderName.Substring(0, folderName.Length - 1);
                }
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
