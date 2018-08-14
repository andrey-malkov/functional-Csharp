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

        public virtual string ExtractPath
        {
            get => throw new NotImplementedException();
        }
    }

    class RootCatalogNode : CatalogNode
    {
        public RootCatalogNode(CTapeHeaderDescriptorBlock tapeHeaderDescriptorBlock)
            : base(ENodeType.Root, tapeHeaderDescriptorBlock.MediaName, tapeHeaderDescriptorBlock.StartPosition)
            => DescriptorBlock = tapeHeaderDescriptorBlock;

        public override string ExtractPath
        {
            get => throw new Exception("Tape nodes can not be extracted. Only volume, folder or file nodes can be extracted.");
        }
    }

    class SetCatalogNode : CatalogNode
    {
        public SetCatalogNode(CStartOfDataSetDescriptorBlock dataSetDescriptorBlock)
            : base(ENodeType.Set, "Set: " + dataSetDescriptorBlock.DataSetNumber + " - " + dataSetDescriptorBlock.DataSetName, dataSetDescriptorBlock.StartPosition)
            => DescriptorBlock = dataSetDescriptorBlock;

        public override string ExtractPath
        {
            get => throw new Exception("Set nodes can not be extracted. Only volume, folder or file nodes can be extracted.");
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

        public override string ExtractPath
        {
            get => "";
        }
    }

    class DirectoryCatalogNode : CatalogNode
    {
        private string DirectoryName;

        public DirectoryCatalogNode(CDirectoryDescriptorBlock directoryDescriptorBlock, string folderName)
            : base(ENodeType.Folder, folderName, directoryDescriptorBlock.StartPosition)
        {
            DescriptorBlock = directoryDescriptorBlock;
            DirectoryName = directoryDescriptorBlock.DirectoryName;
        }

        public override string ExtractPath
        {
            get => Path.GetDirectoryName(DirectoryName).Split('\\').Last();
        }
    }

    class FileCatalogNode : CatalogNode
    {
        private string FileName;

        public FileCatalogNode(CFileDescriptorBlock fileDescriptorBlock, string fileName)
            : base(ENodeType.File, fileName, fileDescriptorBlock.StartPosition)
        {
            DescriptorBlock = fileDescriptorBlock;
            FileName = fileDescriptorBlock.FileName;
        }

        public override string ExtractPath
        {
            get => FileName;
        }
    }
}