using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Tagtoo
{
    public class PInvokeWin32
    {
        #region DllImports and Constants

        public const UInt32 GENERIC_READ = 0x80000000;
        public const UInt32 GENERIC_WRITE = 0x40000000;
        public const UInt32 FILE_SHARE_READ = 0x00000001;
        public const UInt32 FILE_SHARE_WRITE = 0x00000002;
        public const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const UInt32 OPEN_EXISTING = 3;
        public const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public const Int32 INVALID_HANDLE_VALUE = -1;

        // FSCTL Control Code
        public const UInt32 FSCTL_QUERY_USN_JOURNAL = 0x000900f4;
        public const UInt32 FSCTL_ENUM_USN_DATA = 0x000900b3;
        public const UInt32 FSCTL_CREATE_USN_JOURNAL = 0x000900e7;
        public const UInt32 FSCTL_READ_USN_JOURNAL = 590011;
        public const UInt32 FSCTL_READ_FILE_USN_DATA = 590059;

        public const UInt32 DataOverwrite = 0x00000001;
        public const UInt32 DataExtend = 0x00000002;
        public const UInt32 DataTruncation = 0x00000004;
        //public const UInt32 0x00000008=             0x00000008;
        public const UInt32 NamedDataOverwrite = 0x00000010;
        public const UInt32 NamedDataExtend = 0x00000020;
        public const UInt32 NamedDataTruncation = 0x00000040;
        //public const UInt32 0x00000080=             0x00000080;
        public const UInt32 FileCreate = 0x00000100;
        public const UInt32 FileDelete = 0x00000200;
        public const UInt32 PropertyChange = 0x00000400;
        public const UInt32 SecurityChange = 0x00000800;
        public const UInt32 RenameOldName = 0x00001000;
        public const UInt32 RenameNewName = 0x00002000;
        public const UInt32 IndexableChange = 0x00004000;
        public const UInt32 BasicInfoChange = 0x00008000;
        public const UInt32 HardLinkChange = 0x00010000;
        public const UInt32 CompressionChange = 0x00020000;
        public const UInt32 EncryptionChange = 0x00040000;
        public const UInt32 ObjectIdChange = 0x00080000;
        public const UInt32 ReparsePointChange = 0x00100000;
        public const UInt32 StreamChange = 0x00200000;
        //public const UInt32 0x00400000=            // 0x00400000
        //public const UInt32 0x00800000=            // 0x00800000
        ///public const UInt32 0x01000000=            // 0x01000000
        ///public const UInt32 0x02000000=            // 0x02000000
        //public const UInt32 0x04000000=            // 0x04000000
        //public const UInt32 0x08000000=            // 0x08000000
        //public const UInt32 0x10000000=            // 0x10000000
        //public const UInt32 0x20000000=            // 0x20000000
        //public const UInt32 0x40000000=            // 0x40000000
        public const UInt32 Close = 0x80000000;

        static string[] Reasons ={
            "DataOverwrite",         // 0x00000001
            "DataExtend",            // 0x00000002
            "DataTruncation",        // 0x00000004
      "0x00000008",            // 0x00000008
      "NamedDataOverwrite",    // 0x00000010
      "NamedDataExtend",       // 0x00000020
      "NamedDataTruncation",   // 0x00000040
      "0x00000080",            // 0x00000080
      "FileCreate",            // 0x00000100
      "FileDelete",            // 0x00000200
      "PropertyChange",        // 0x00000400
      "SecurityChange",        // 0x00000800
      "RenameOldName",         // 0x00001000
      "RenameNewName",         // 0x00002000
      "IndexableChange",       // 0x00004000
      "BasicInfoChange",       // 0x00008000
      "HardLinkChange",        // 0x00010000
      "CompressionChange",     // 0x00020000
      "EncryptionChange",      // 0x00040000
      "ObjectIdChange",        // 0x00080000
      "ReparsePointChange",    // 0x00100000
      "StreamChange",          // 0x00200000
      "0x00400000",            // 0x00400000
      "0x00800000",            // 0x00800000
      "0x01000000",            // 0x01000000
      "0x02000000",            // 0x02000000
      "0x04000000",            // 0x04000000
      "0x08000000",            // 0x08000000
      "0x10000000",            // 0x10000000
      "0x20000000",            // 0x20000000
      "0x40000000",            // 0x40000000
      "*Close*"                // 0x80000000
        };

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess,
                                                  uint dwShareMode, IntPtr lpSecurityAttributes,
                                                  uint dwCreationDisposition, uint dwFlagsAndAttributes,
                                                  IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(IntPtr hDevice,
                                                      UInt32 dwIoControlCode,
                                                      IntPtr lpInBuffer, Int32 nInBufferSize,
                                                      out USN_JOURNAL_DATA lpOutBuffer, Int32 nOutBufferSize,
                                                      out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(IntPtr hDevice,
                                                      UInt32 dwIoControlCode,
                                                      IntPtr lpInBuffer, Int32 nInBufferSize,
                                                      IntPtr lpOutBuffer, Int32 nOutBufferSize,
                                                      out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern void ZeroMemory(IntPtr ptr, Int32 size);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FILETIME
        {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USN_JOURNAL_DATA
        {
            public UInt64 UsnJournalID;
            public Int64 FirstUsn;
            public Int64 NextUsn;
            public Int64 LowestValidUsn;
            public Int64 MaxUsn;
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MFT_ENUM_DATA
        {
            public UInt64 StartFileReferenceNumber;
            public Int64 LowUsn;
            public Int64 HighUsn;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CREATE_USN_JOURNAL_DATA
        {
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct READ_USN_JOURNAL_DATA
        {
            public UInt64 StartUSN;
            public UInt32 ReasonMask;
            public UInt32 ReturnOnlyOnClose;
            public UInt64 Timeout;
            public UInt64 BytesToWaitFor;
            public UInt64 UsnJournalID;
        }

        public class USN_RECORD
        {
            public UInt32 RecordLength;
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
            public UInt64 FileReferenceNumber;  // 8
            public UInt64 ParentFileReferenceNumber; // 16
            public UInt64 Usn; // Need be care
            public UInt64 TimeStamp; // Need Be care
            public UInt32 Reason;
            public UInt32 SourceInfo;
            public UInt32 SecurityId;
            public UInt32 FileAttributes; // 52
            public UInt16 FileNameLength;
            public UInt16 FileNameOffset;
            public string FileName = string.Empty;

            private const int RecordLength_OFFSET = 0;
            private const int MajorVersion_OFFSET = 4;
            private const int MinorVersion_OFFSET = 6;
            private const int FileReferenceNumber_OFFSET = 8;
            private const int ParentFileReferenceNumber_OFFSET = 16;
            private const int Usn_OFFSET = 24;
            private const int TimeStamp_OFFSET = 32;
            private const int Reason_OFFSET = 40;
            private const int SourceInfo_OFFSET = 44;
            private const int SecurityId_OFFSET = 48;
            private const int FileAttributes_OFFSET = 52;
            private const int FileNameLength_OFFSET = 56;
            private const int FileNameOffset_OFFSET = 58;
            private const int FileName_OFFSET = 60;



            public USN_RECORD(IntPtr p)
            {
                this.RecordLength = (UInt32)Marshal.ReadInt32(p, RecordLength_OFFSET);
                this.MajorVersion = (UInt16)Marshal.ReadInt16(p, MajorVersion_OFFSET);
                this.MinorVersion = (UInt16)Marshal.ReadInt16(p, MinorVersion_OFFSET);
                this.FileReferenceNumber = (UInt64)Marshal.ReadInt64(p, FileReferenceNumber_OFFSET);
                this.ParentFileReferenceNumber = (UInt64)Marshal.ReadInt64(p, ParentFileReferenceNumber_OFFSET);
                this.Usn = (UInt64)Marshal.ReadInt64(p, Usn_OFFSET);
                this.TimeStamp = (UInt64)Marshal.ReadInt64(p, TimeStamp_OFFSET);
                this.Reason = (UInt32)Marshal.ReadInt32(p, Reason_OFFSET);
                this.SourceInfo = (UInt32)Marshal.ReadInt32(p, SourceInfo_OFFSET);
                this.SecurityId = (UInt32)Marshal.ReadInt32(p, SecurityId_OFFSET);
                this.FileAttributes = (UInt32)Marshal.ReadInt32(p, FileAttributes_OFFSET);
                this.FileNameLength = (UInt16)Marshal.ReadInt16(p, FileNameLength_OFFSET);
                this.FileNameOffset = (UInt16)Marshal.ReadInt16(p, FileNameOffset_OFFSET);

                this.FileName = Marshal.PtrToStringUni(new IntPtr(p.ToInt32() + this.FileNameOffset), this.FileNameLength / sizeof(char));
            }
        }

        #endregion
    }
}
