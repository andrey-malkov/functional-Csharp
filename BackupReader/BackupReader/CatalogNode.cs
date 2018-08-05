using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BackupReader
{

    class CatalogNode
    {
        public string Name { get; protected set; }
        public CDescriptorBlock DescriptorBlock { get; protected set; }
        public ENodeType Type { get; protected set; }
        public long Offset { get; protected set; }

        public CatalogNode() { }

        public CatalogNode(ENodeType type, string name, long offset)
        {
            Type = type;
            Name = name;
            Offset = offset;
        }

        public CatalogNode(CDescriptorBlock descriptorBlock)
        {
            DescriptorBlock = descriptorBlock;
        }

        public string Details
        {
            get
            {
                var builder = new StringBuilder();

                builder.AppendLine(Name);
                builder.AppendLine();
                builder.AppendLine(DescriptorBlock.GetDetailsString());

                return builder.ToString();
            }
        }

        public virtual string Extract(CDescriptorBlock parent, string targetPath)
        {
            throw new NotImplementedException();
        }
    }

    class RootCatalogNode : CatalogNode
    {
        public RootCatalogNode(CTapeHeaderDescriptorBlock tapeHeaderDescriptorBlock)
            : base(ENodeType.Root, tapeHeaderDescriptorBlock.MediaName, tapeHeaderDescriptorBlock.StartPosition)
            => DescriptorBlock = tapeHeaderDescriptorBlock;

        public override string Extract(CDescriptorBlock parent, string targetPath)
        {
            throw new Exception("Tape nodes can not be extracted. Only volume, folder or file nodes can be extracted.");
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

        public override string Extract(CDescriptorBlock parent, string targetPath)
        {
            throw new Exception("Set node can not be extracted. Only volume, folder or file nodes can be extracted.");
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

    class VolumeCatalogNode : CatalogNode
    {
        public VolumeCatalogNode(CVolumeDescriptorBlock volumeDescriptorBlock)
        {
            DescriptorBlock = volumeDescriptorBlock;
            Name = volumeDescriptorBlock.DeviceName;
            Type = ENodeType.Volume;
        }

        public override string Extract(CDescriptorBlock parent, string targetPath)
        {
            return targetPath;
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

        public override string Extract(CDescriptorBlock parent, string targetPath)
        {
            var validDirectoryName = ValidatePath(((CDirectoryDescriptorBlock)DescriptorBlock).DirectoryName);
            return CreateDir(Path.Combine(targetPath, validDirectoryName));

            string ValidatePath(string path)
            {
                return Path.GetDirectoryName(path).Split('\\').Last();
            }

            string CreateDir(string dirPath)
            {
                if (!Directory.Exists(dirPath))
                {
                    DirectoryInfo dirInfo = Directory.CreateDirectory(targetPath);
                    return dirInfo.FullName;
                }

                return dirPath;
            }
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

        public override string Extract(CDescriptorBlock parent, string targetPath)
        {
            var fileName = Path.Combine(targetPath, ((CFileDescriptorBlock)DescriptorBlock).FileName);
            var file = new FileStream(fileName, FileMode.Create);

            foreach (CDataStream data in DescriptorBlock.Streams)
            {
                if (data.Header.StreamID == "STAN")
                {
                    file.Write(data.Data, 0, data.Data.Length);
                }
            }
            file.Close();
            return fileName;
        }
    }
}