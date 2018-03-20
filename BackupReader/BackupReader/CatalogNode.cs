using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackupReader
{
    class CatalogNode
    {
        public string Name { get; set; }
        public CDescriptorBlock DescriptorBlock { get; set; }
        public ENodeType Type { get; set; }
        public long Offset { get; set; }

        public string GetDetailsString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Name);
            builder.AppendLine();
            if(DescriptorBlock != null) { 
                builder.AppendLine(DescriptorBlock.GetDetailsString());
            }

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
}
