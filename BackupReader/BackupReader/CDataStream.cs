
namespace BackupReader
{
    enum EStreamFileSystemAttributes : ushort
    {
        STREAM_MODIFIED_BY_READ = 1,  // Data in stream has changed after reading, do not attempt to do a verify operation. BIT0
        STREAM_CONTAINS_SECURITY = 2, // Security information is contained in this stream. BIT1
        STREAM_IS_NON_PORTABLE = 4,   // This data can only be restored to the same OS that it was saved from. BIT2
        STREAM_IS_SPARSE = 8,         // The stream data is sparse (see below) BIT3
        //Reserved for future use. BIT4 - BIT15
    }

    enum EStreamMediaFormatAttributes : ushort
    {
        STREAM_CONTINUE = 1, // This is a continuation stream. BIT0
        STREAM_VARIABLE = 2, // Data size for this stream is variable. BIT1
        STREAM_VAR_END = 4, // Last piece of the variable length data. BIT2
        STREAM_ENCRYPTED = 8, // This stream is encrypted. BIT3
        STREAM_COMPRESSED = 16, // This stream is compressed. BIT4
        STREAM_CHECKSUMED = 32, // This stream is followed by a checksum stream. BIT5
        STREAM_EMBEDDED_LENGTH = 64, // The stream length is embedded in the data BIT6
        // Reserved for future use. BIT7 - BIT15
    }

    /// <summary>
    /// Each block in the backup file is followed by one or more data streams.
    /// Data streams may be used for alignment purposes, storing file data, or
    /// storing long directory and file names, security information, etc.
    /// </summary>
    class CDataStream
    {
        public CStreamHeader Header;
        public byte[] Data;

        public CDataStream(CBackupStream Reader)
        {
            Header = new CStreamHeader(Reader);
            Data = Reader.ReadBytes((int)Header.StreamLength);
            // Ensure that we are on the 4 byte boundary
            var nullbytecount = (4 - (Reader.BaseStream.Position % 4)) % 4;
            Reader.BaseStream.Seek(nullbytecount, System.IO.SeekOrigin.Current);
        }

        public class CStreamHeader
        {
            public string StreamID;
            public EStreamFileSystemAttributes StreamFileSystemAttributes;
            public EStreamMediaFormatAttributes StreamMediaFormatAttributes;
            public ulong StreamLength;
            public ushort DataEncryptionAlgorithm;
            public ushort DataCompressionAlgorithm;
            public ushort Checksum; // Stream data follow immediately after the Checksum field.

            public CStreamHeader(CBackupStream reader)
            {
                // Check for EOF
                if (reader.BaseStream.Position + 22 >= reader.BaseStream.Length)
                {
                    StreamID = "";
                    return;
                }

                StreamID = reader.ReadFixedSizeString(4, EStringType.ANSI);

                StreamFileSystemAttributes = (EStreamFileSystemAttributes)reader.ReadUInt16();
                StreamMediaFormatAttributes = (EStreamMediaFormatAttributes)reader.ReadUInt16();
                StreamLength = reader.ReadUInt64();
                DataEncryptionAlgorithm = reader.ReadUInt16();
                DataCompressionAlgorithm = reader.ReadUInt16();
                Checksum = reader.ReadUInt16();
            }
        }

    }

}
