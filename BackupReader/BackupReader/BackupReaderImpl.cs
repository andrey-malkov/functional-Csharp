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