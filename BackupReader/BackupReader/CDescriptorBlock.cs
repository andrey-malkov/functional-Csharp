using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BackupReader
{

    enum EBlockType: uint
    {
        ROOT = 0x0,
        /// <summary>
        /// TAPE descriptor block
        /// </summary>
        MTF_TAPE = 0x45504154,
        /// <summary>
        /// Start of data SET descriptor block
        /// </summary>
        MTF_SSET = 0x54455353,
        /// <summary>
        /// VOLume descriptor Block
        /// </summary>
        MTF_VOLB = 0x424C4F56,
        /// <summary>
        /// DIRectory descriptor Block
        /// </summary>
        MTF_DIRB = 0x42524944,
        /// <summary>
        /// FILE descriptor block
        /// </summary>
        MTF_FILE = 0x454C4946,
        /// <summary>
        /// Corrupt object descriptor block
        /// </summary>
        MTF_CFIL = 0x4C494643,
        /// <summary>
        /// End of Set Pad descriptor Block
        /// </summary>
        MTF_ESPB = 0x42505345,
        /// <summary>
        /// End of SET descriptor block
        /// </summary>
        MTF_ESET = 0x54455345,
        /// <summary>
        /// End Of Tape Marker descriptor block
        /// </summary>
        MTF_EOTM = 0x4D544F45,
        /// <summary>
        /// Soft FileMark descriptor Block
        /// </summary>
        MTF_SFMB = 0x424D4653,


        MTF_DBDB = 0x42444244,
    }

    enum EBlockAttributes : uint
    {
        MTF_CONTINUATION = 0x1,        // Bit set if DBLK is a continuation from the previous tape. any BIT0
        MTF_COMPRESSION = 0x4,         // Bit set if compression may be active. any BIT2
        MTF_EOS_AT_EOM = 0x8,          // Bit set if the End Of Medium was hit during end of set processing. any BIT3
        MTF_SET_MAP_EXISTS = 0x10000,  // Bit set if an Media Based Catalog Set Map can be found on the tape. MTF_TAPE BIT16
        MTF_FDD_ALLOWED = 0x20000,     // Bit set if an attempt will be made to put a Media Based Catalog File/Directory Detail section on the tape. MTF_TAPE BIT17
        MTF_FDD_EXISTS = 0x10000,      // Bit set if a Media Based Catalog File/Directory Detail section has been successfully put on the tape for this Data Set. MTF_SSET BIT16
        MTF_ENCRYPTION = 0x20000,      // Bit set if encryption is active for the data streams within this Data Set. MTF_SSET BIT17
        MTF_FDD_ABORTED = 0x10000,     // Bit set if a Media Based Catalog File/Directory Detail section was aborted for any reason during the write operation. MTF_ESET BIT16
        MTF_END_OF_FAMILY = 0x20000,   // Bit set if the Media Based Catalog Set Map has been aborted. This condition means that additional Data Sets cannot be appended to the tape. MTF_ESET BIT17
        MTF_ABORTED_SET = 0x40000,     // Bit set if the Data Set was aborted while being written. This can happen if a fatal error occurs while writing data, or if the user terminates the data management operation. An MTF_ESET DBLK containing this flag is put at the end of the Data Set even if it was aborted. MTF_ESET BIT18
        MTF_NO_ESET_PBA = 0x10000,     // Bit set if no Data Set ends on this tape (i.e. continuation tape must follow this tape). MTF_EOTM BIT16
        MTF_INVALID_ESET_PBA = 0x20000,// Bit set if the Physical Block Address (PBA) of the MTF_ESET is invalid because the tape drive doesn't support physical block addressing. MTF_EOTM BIT17
    }

    enum EOSID : byte 
    {
        NetWare = 1,         // 0
        NetWare_SMS = 13,    // 1, 2
        Windows_NT = 14,     // 0
        DOS_Windows_3X = 24, // 0
        OS_2 = 25,           // 0
        Windows_95 = 26,     // 0
        Macintosh = 27,      // 0
        UNIX = 28,           // 0
        // To Be Assigned 33 - 127
        // Vendor Specific 128 - 255
    }

    enum EStringType : byte
    {
        None = 0,
        ANSI = 1,
        Unicode = 2,
    }

    /// <summary>
    /// Represents a descriptor block. Descriptor blocks define the types and 
    /// attributes of the data in the backup file.
    /// </summary>
    class CDescriptorBlock
    {
        public virtual string GetDetailsString()
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("Block type: " + BlockType);
            builder.AppendLine("Block attributes: " + Attributes);
            builder.AppendLine("OS ID: " + OSID);
            builder.AppendLine("Displayable size: " + DisplayableSize);
            builder.AppendLine("String type: " + StringType);
            builder.AppendLine("Stream count: " + Streams.Count);

            foreach (var stream in Streams)
            {
                builder.AppendLine();
                builder.AppendLine("  Stream ID: " + stream.Header.StreamID);
                builder.AppendLine("  Stream length: " + stream.Header.StreamLength);
                builder.AppendLine("  Media attr.: " + stream.Header.StreamMediaFormatAttributes);
                builder.AppendLine("  File system attr.: " + stream.Header.StreamFileSystemAttributes);
            }

            return builder.ToString();
        }

        public long StartPosition;

        public EBlockType BlockType;
        public EBlockAttributes Attributes;
        public ushort OffsetToFirstEvent; // Obsolete
        public EOSID OSID;
        public byte OSVersion;
        public ulong DisplayableSize;
        public ulong FormatLogicalAddress;
        public ushort ReservedMBC;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public uint ControlBlock;
        public uint Reserved4;
        public COSSpecificData OsSpecificData;
        public EStringType StringType;
        public byte Reserved5;
        public ushort HeaderChecksum;

        public List<CDataStream> Streams; 

        /// <summary>
        /// Read block header.
        /// </summary>
        protected void ReadData(CBackupStream reader)
        {
            StartPosition = reader.BaseStream.Position;
            Streams = new List<CDataStream>();

            BlockType = (EBlockType)reader.ReadUInt32();
            Attributes = (EBlockAttributes)reader.ReadUInt32();
            OffsetToFirstEvent = reader.ReadUInt16();
            OSID = (EOSID)reader.ReadByte();
            OSVersion = reader.ReadByte();
            DisplayableSize = reader.ReadUInt64();
            FormatLogicalAddress = reader.ReadUInt64();
            ReservedMBC = reader.ReadUInt16();
            Reserved1 = reader.ReadUInt16();
            Reserved2 = reader.ReadUInt16();
            Reserved3 = reader.ReadUInt16();
            ControlBlock = reader.ReadUInt32();
            Reserved4 = reader.ReadUInt32();
            OsSpecificData = reader.ReadOsSpecificData(StartPosition, OSID, OSVersion, BlockType);
            StringType = (EStringType)reader.ReadByte();
            Reserved5 = reader.ReadByte();
            HeaderChecksum = reader.ReadUInt16();
        }

        /// <summary>
        /// Read streams following this block.
        /// </summary>
        /// <param name="reader"></param>
        protected void ReadStreams(CBackupStream reader)
        {
            // Move to stream
            var off = OffsetToFirstEvent + StartPosition;
            // Make sure we are at a 4 byte boundary
            var nullbytecount = (4 - (off % 4)) % 4;

            reader.BaseStream.Seek(off + nullbytecount,     SeekOrigin.Begin);
            var streamtype = "";

            do
            {
                // Read next stream
                var stream = new CDataStream(reader);
                streamtype = stream.Header.StreamID;
                Streams.Add(stream);
            } while ((streamtype != "SPAD") && (streamtype != ""));
        }

        public CDescriptorBlock()
        {
        }

        public CDescriptorBlock(CBackupStream reader)
        {
            ReadData(reader);
        }
    }

    enum ETapeAttributes : uint 
    {
        TAPE_SOFT_FILEMARK_BIT = 1,
        TAPE_MEDIA_LABEL_BIT = 2,
        // Reserved 2-23
        // Vendor Specific 24-31
    }

    enum EMediaBasedCatalogType : ushort
    {
        No_MBC = 0,
        Type_1_MBC = 1,
        Type_2_MBC = 2,
    }

    class CTapeHeaderDescriptorBlock : CDescriptorBlock
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("Media family ID: " + MediaFamilyID);
            builder.AppendLine("Media name: " + MediaName);
            builder.AppendLine("Media description: " + MediaDescription);
            builder.AppendLine("Media password: " + MediaPassword);
            builder.AppendLine("Media date: " + MediaDate);
            builder.AppendLine("Software: " + SoftwareName);
            builder.AppendLine("Vendor ID: " + SoftwareVendorID);
            builder.AppendLine("Block size: " + FormatLogicalBlockSize);
            builder.AppendLine("Filemark block size: " + SoftFilemarkBlockSize);
            builder.AppendLine("Encryption alg.: " + PasswordEncryptionAlgorithm);

            return builder.ToString();
        }

        public uint MediaFamilyID;
        public ETapeAttributes TapeAttributes;
        public ushort MediaSequenceNumber;
        public ushort PasswordEncryptionAlgorithm;
        public ushort SoftFilemarkBlockSize;
        public EMediaBasedCatalogType MediaBasedCatalogType;
        public string MediaName;
        public string MediaDescription;
        public string MediaPassword;
        public string SoftwareName;
        public ushort FormatLogicalBlockSize;
        public ushort SoftwareVendorID;
        public DateTime MediaDate;
        public byte MTFMajorVersion;

        public CTapeHeaderDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            MediaFamilyID = backupStream.ReadUInt32();
            TapeAttributes = (ETapeAttributes)backupStream.ReadUInt32();
            MediaSequenceNumber = backupStream.ReadUInt16();
            PasswordEncryptionAlgorithm = backupStream.ReadUInt16();
            SoftFilemarkBlockSize = backupStream.ReadUInt16();
            MediaBasedCatalogType = (EMediaBasedCatalogType)backupStream.ReadUInt16();
            MediaName = backupStream.ReadString(StartPosition, StringType);
            MediaDescription = backupStream.ReadString(StartPosition, StringType);
            MediaPassword = backupStream.ReadString(StartPosition, StringType);
            SoftwareName = backupStream.ReadString(StartPosition, StringType);
            FormatLogicalBlockSize = backupStream.ReadUInt16();
            SoftwareVendorID = backupStream.ReadUInt16();
            MediaDate = backupStream.ReadDate();
            MTFMajorVersion = backupStream.ReadByte();
            base.ReadStreams(backupStream);
        }
}

    class CEndOfTapeMarkerDescriptorBlock : CDescriptorBlock 
    {
        public ulong LastESETPBA;

        public CEndOfTapeMarkerDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            LastESETPBA = backupStream.ReadUInt64();
            base.ReadStreams(backupStream);
        }
    }

    enum ESSETAttributes : uint 
    {
        SSET_TRANSFER_BIT = 0x1,     // This bit is set if the data management operation is a “transfer”. It indicates that the files in this Data Set were removed from the source media after the operation was completed. BIT0
        SSET_COPY_BIT = 0x2,         // This bit is set if the operation is a “copy”. The copy method copies all selected files from the primary storage to the media. The file’s “modified” flag IS NOT reset afterwards. BIT1
        SSET_NORMAL_BIT = 0x4,       // This bit is set if the backup type is “normal”. The normal backup method backs up all selected files. The file’s “modified” flag IS reset afterwards. BIT2
        SSET_DIFFERENTIAL_BIT = 0x8, // This bit is set if the backup type is “differential”. The differential backup method only backs up selected files having their “modified” flag set. The file’s “modified” flag IS NOT reset afterwards. BIT3
        SSET_INCREMENTAL_BIT = 0x10, // This bit is set if the backup type is “incremental”. The incremental backup method only backs up selected files having their “modified” flag set. The file’s “modified” flag IS reset afterwards. BIT4
        SSET_DAILY_BIT = 0x20,       // This bit is set if the backup type is “daily”. The daily backup method only backs up selected files created or modified with today’s date. The file’s “modified” flag IS NOT reset afterwards. BIT5
        // Reserved (set to zero) BIT6 - BIT23
        // Vendor Specific BIT24 - BIT31
    }

    class CStartOfDataSetDescriptorBlock : CDescriptorBlock 
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("Attributes: " + SSETAttributes);
            builder.AppendLine("Dataset number: " + DataSetNumber);
            builder.AppendLine("Dataset name: " + DataSetName);
            builder.AppendLine("Dataset description: " + DataSetDescription);
            builder.AppendLine("Dataset password: " + DataSetPassword);
            builder.AppendLine("User: " + UserName);
            builder.AppendLine("Physical block address: " + PhysicalBlockAddress);
            builder.AppendLine("Media write date: " + MediaWriteDate);
            builder.AppendLine("Media catalog version: " + MediaCatalogVersion);
            builder.AppendLine("Encryption alg.: " + PasswordEncryptionAlgorithm);
            builder.AppendLine("Compression alg.: " + SoftwareCompressionAlgorithm);

            return builder.ToString();
        }

        public ESSETAttributes SSETAttributes;
        public ushort PasswordEncryptionAlgorithm;
        public ushort SoftwareCompressionAlgorithm;
        public ushort SoftwareVendorID;
        public ushort DataSetNumber;
        public string DataSetName;
        public string DataSetDescription;
        public string DataSetPassword;
        public string UserName;
        public ulong PhysicalBlockAddress;
        public DateTime MediaWriteDate;
        public byte SoftwareMajorVersion;
        public byte SoftwareMinorVersion;
        public sbyte MTFTimeZone;
        public byte MTFMinorVersion;
        public byte MediaCatalogVersion;

        public CStartOfDataSetDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            SSETAttributes = (ESSETAttributes)backupStream.ReadUInt32();
            PasswordEncryptionAlgorithm = backupStream.ReadUInt16();
            SoftwareCompressionAlgorithm = backupStream.ReadUInt16();
            SoftwareVendorID = backupStream.ReadUInt16();
            DataSetNumber = backupStream.ReadUInt16();
            DataSetName = backupStream.ReadString(StartPosition, StringType);
            DataSetDescription = backupStream.ReadString(StartPosition, StringType);
            DataSetPassword = backupStream.ReadString(StartPosition, StringType);
            UserName = backupStream.ReadString(StartPosition, StringType);
            PhysicalBlockAddress = backupStream.ReadUInt64();
            MediaWriteDate = backupStream.ReadDate();
            SoftwareMajorVersion = backupStream.ReadByte();
            SoftwareMinorVersion = backupStream.ReadByte();
            MTFTimeZone = backupStream.ReadSByte();
            MTFMinorVersion = backupStream.ReadByte();
            MediaCatalogVersion = backupStream.ReadByte();
            base.ReadStreams(backupStream);
        }
    }

    class CEndOfDataSetDescriptorBlock : CDescriptorBlock 
    {
        public ESSETAttributes ESETAttributes;
        public uint NumberOfCorruptFiles;
        public ulong ReservedforMBC1;
        public ulong ReservedforMBC2;
        public ushort FDDMediaSequenceNumber;
        public ushort DataSetNumber;
        public DateTime MediaWriteDate;

        public CEndOfDataSetDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            ESETAttributes = (ESSETAttributes)backupStream.ReadUInt32();
            NumberOfCorruptFiles = backupStream.ReadUInt32();
            ReservedforMBC1 = backupStream.ReadUInt64();
            ReservedforMBC2 = backupStream.ReadUInt64();
            FDDMediaSequenceNumber = backupStream.ReadUInt16();
            DataSetNumber = backupStream.ReadUInt16();
            MediaWriteDate = backupStream.ReadDate();
            base.ReadStreams(backupStream);
        }
    }

    enum EVOLBAttributes : uint 
    {
        VOLB_NO_REDIRECT_RESTORE_BIT = 0x1, // Objects following this DBLK can only be restored to the device from which they were backed up. BIT0
        VOLB_NON_VOLUME_BIT = 0x2,          // Objects following this DBLK are not associated with a volume. BIT1
        VOLB_DEV_DRIVE_BIT = 0x4,           // Device name format is, “<drive letter>:”. BIT2
        VOLB_DEV_UNC_BIT = 0x8,             // Device name format is UNC. BIT3
        VOLB_DEV_OS_SPEC_BIT = 0x10,        // Device name format is OS specific (refer to Appendix C for details on a given OS). BIT4
        VOLB_DEV_VEND_SPEC_BIT = 0x20       // Device name format is vendor specific. BIT5
        // Reserved (set to zero) BIT6 - BIT23
        // Vendor Specific BIT24 - BIT31
    }

    class CVolumeDescriptorBlock : CDescriptorBlock 
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("Volume name: " + VolumeName);
            builder.AppendLine("Machine name: " + MachineName);
            builder.AppendLine("Device name: " + DeviceName);
            builder.AppendLine("Attributes: " + VOLBAttributes);
            builder.AppendLine("Write date: " + MediaWriteDate);

            return builder.ToString();
        }

        public EVOLBAttributes VOLBAttributes;
        public string DeviceName;
        public string VolumeName;
        public string MachineName;
        public DateTime MediaWriteDate;

        public CVolumeDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            VOLBAttributes = (EVOLBAttributes)backupStream.ReadUInt32();
            DeviceName = backupStream.ReadString(StartPosition, StringType);
            VolumeName = backupStream.ReadString(StartPosition, StringType);
            MachineName = backupStream.ReadString(StartPosition, StringType);
            MediaWriteDate = backupStream.ReadDate();
            base.ReadStreams(backupStream);
        }
    }

    // Dummy implementation
    class CDatabaseDescriptorBlock : CDescriptorBlock
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());
            return builder.ToString();
        }

        public CDatabaseDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            base.ReadStreams(backupStream);
        }
    }

    [Flags]
    enum EDIRBAttributes : uint 
    {
        DIRB_READ_ONLY_BIT = 0x100,        // This bit is set if the directory is marked as read only. BIT8
        DIRB_HIDDEN_BIT = 0x200,           // This bit is set if the directory is hidden from the user. BIT9
        DIRB_SYSTEM_BIT = 0x400,           // This bit is set if the directory is a system directory. BIT10
        DIRB_MODIFIED_BIT = 0x800,         // This bit is set if the directory has been modified. This is also referred to as an “archive” flag. BIT11
        DIRB_EMPTY_BIT = 0x10000,          // This bit set if the directory contained no files or subdirectories. BIT16
        DIRB_PATH_IN_STREAM_BIT = 0x20000, // This bit set if the directory path is stored in a stream associated with this DBLK. BIT17
        DIRE_CORRUPT_BIT = 0x40000,        // This bit set if the data associated with the directory could not be read. BIT18
        // Reserved (set to zero) BIT0 - BIT7, BIT12 - BIT15, BIT19 - BIT23
        // Vendor Specific BIT24 - BIT31
    }

    class CDirectoryDescriptorBlock : CDescriptorBlock 
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("Directory name: " + DirectoryName);
            builder.AppendLine("Attributes: " + DIRBAttributes);
            builder.AppendLine("Directory ID: " + DirectoryID);
            builder.AppendLine("Creation date: " + CreationDate);
            builder.AppendLine("Backup date: " + BackupDate);
            builder.AppendLine("Last access date: " + LastAccessDate);
            builder.AppendLine("Last modification date: " + LastModificationDate);

            return builder.ToString();
        }

        public EDIRBAttributes DIRBAttributes;
        public DateTime LastModificationDate;
        public DateTime CreationDate;
        public DateTime BackupDate;
        public DateTime LastAccessDate;
        public uint DirectoryID;
        public string DirectoryName;

        public CDirectoryDescriptorBlock(CBackupStream reader)
        {
            base.ReadData(reader);
            DIRBAttributes = (EDIRBAttributes)reader.ReadUInt32();
            LastModificationDate = reader.ReadDate();
            CreationDate = reader.ReadDate();
            BackupDate = reader.ReadDate();
            LastAccessDate = reader.ReadDate();
            DirectoryID = reader.ReadUInt32();
            // MTF uses '\0' as the path seperator. Replace them with '\\'
            DirectoryName = reader.ReadString(StartPosition, StringType).Replace('\0','\\');
            base.ReadStreams(reader);
        }
    }

    [Flags]
    enum EFileAttributes : uint 
    {
        FILE_READ_ONLY_BIT = 0x100,        // This bit is set if the file is marked as read only. BIT8
        FILE_HIDDEN_BIT = 0x200,           // This bit is set if the file is hidden from the user. BIT9
        FILE_SYSTEM_BIT = 0x400,           // This bit is set if the file is a system file. BIT10
        FILE_MODIFIED_BIT = 0x800,         // This bit is set if the file has been modified. This is also referred to as an “archive” flag. BIT11
        FILE_IN_USE_BIT = 0x10000,         // This bit set if the file was in use at the time it was backed up. BIT16
        FILE_NAME_IN_STREAM_BIT = 0x20000, // This bit set if the file name is stored in a stream associated with this DBLK. BIT17
        FILE_CORRUPT_BIT = 0x40000,        // This bit set if the data associated with the file could not be read. BIT18
        // Reserved (set to zero) BIT0 - BIT7, BIT12 - BIT15, BIT19 - BIT23
        // Vendor Specific BIT24 - BIT31
    }

    class CFileDescriptorBlock : CDescriptorBlock 
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("File name: " + FileName);
            builder.AppendLine("Attributes: " + FileAttributes);
            builder.AppendLine("File ID: " + FileID);
            builder.AppendLine("Directory ID: " + DirectoryID);

            builder.AppendLine("Creation date: " + CreationDate);
            builder.AppendLine("Backup date: " + BackupDate);
            builder.AppendLine("Last access date: " + LastAccessDate);
            builder.AppendLine("Last modification date: " + LastModificationDate);

            return builder.ToString();
        }

        public EFileAttributes FileAttributes;
        public DateTime LastModificationDate;
        public DateTime CreationDate;
        public DateTime BackupDate;
        public DateTime LastAccessDate;
        public uint DirectoryID;
        public uint FileID;
        public string FileName;

        public CFileDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            FileAttributes = (EFileAttributes)backupStream.ReadUInt32();
            LastModificationDate = backupStream.ReadDate();
            CreationDate = backupStream.ReadDate();
            BackupDate = backupStream.ReadDate();
            LastAccessDate = backupStream.ReadDate();
            DirectoryID = backupStream.ReadUInt32();
            FileID = backupStream.ReadUInt32();
            FileName = backupStream.ReadString(StartPosition, StringType);
            base.ReadStreams(backupStream);
        }
    }

    enum ECFilAttributes : uint 
    {
        CFIL_LENGTH_CHANGE_BIT = 0x10000,  // This bit is set if the file size has changed since the file was opened for the write operation. BIT16
        CFIL_UNREADABLE_BLK_BIT = 0x20000, // This bit is set if a hard error was encountered reading the source media (hard disk). This usually indicates that the media itself is bad (i.e. bad sector). BIT17
        CFIL_DEADLOCK_BIT = 0x40000,       // This bit is set if the file was deadlocked. (i.e. On a system supporting record and file locking, it was not possible to get a region of a file unlocked within a watchdog time interval.) BIT18
       // Reserved (set to zero) BIT0 - BIT15, BIT19 - BIT23
       // Vendor Specific BIT24 - BIT31    
    }

    class CCorruptObjectDescriptorBlock : CDescriptorBlock 
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("Attributes: " + CFilAttributes);
            builder.AppendLine("Corrupted stream number: " + CorrupStreamNumber);
            builder.AppendLine("Offset: " + StreamOffset);

            return builder.ToString();
        }


        public ECFilAttributes CFilAttributes;
        public ulong Reserved;
        public ulong StreamOffset;
        public ushort CorrupStreamNumber;

        public CCorruptObjectDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            CFilAttributes = (ECFilAttributes)backupStream.ReadUInt32();
            Reserved = backupStream.ReadUInt64();
            StreamOffset = backupStream.ReadUInt64();
            CorrupStreamNumber = backupStream.ReadUInt16();
            base.ReadStreams(backupStream);
        }
    }

    class CEndOfPadSetDescriptorBlock : CDescriptorBlock 
    {
        public CEndOfPadSetDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            base.ReadStreams(backupStream);
        }
    }

    class CSoftFilemarkDescriptorBlock : CDescriptorBlock 
    {
        public override string GetDetailsString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(base.GetDetailsString());

            builder.AppendLine("Number of entries: " + NumberOfFilemarkEntries);
            builder.AppendLine("Entries used: " + FilemarkEntriesUsed);

            return builder.ToString();
        }

        public uint NumberOfFilemarkEntries;
        public uint FilemarkEntriesUsed;
        public uint[] PBAofPreviousFilemarksArray;

        public CSoftFilemarkDescriptorBlock(CBackupStream backupStream)
        {
            base.ReadData(backupStream);
            NumberOfFilemarkEntries = backupStream.ReadUInt32();
            FilemarkEntriesUsed = backupStream.ReadUInt32();
            PBAofPreviousFilemarksArray = new uint[FilemarkEntriesUsed];
            for (uint i = 0; i < NumberOfFilemarkEntries; i++)
            {
                uint val = backupStream.ReadUInt32();
                if (i < FilemarkEntriesUsed)
                    PBAofPreviousFilemarksArray.SetValue(val, i);
            }
            //base.ReadStreams(Reader);
        }
    }

}

