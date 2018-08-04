using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }

    class RootCatalogNode : CatalogNode
    {
        public RootCatalogNode(CTapeHeaderDescriptorBlock tapeHeaderDescriptorBlock)
            : base(ENodeType.Root, tapeHeaderDescriptorBlock.MediaName, tapeHeaderDescriptorBlock.StartPosition)
            => DescriptorBlock = tapeHeaderDescriptorBlock;
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
}