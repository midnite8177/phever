using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel; 

namespace Tagtoo
{
    public class MFT
    {
        IntPtr mDriveHandle = IntPtr.Zero;
        PInvokeWin32.USN_JOURNAL_DATA mUSN;

        Dictionary<UInt64, PInvokeWin32.USN_RECORD> mDictionarys = new Dictionary<ulong, PInvokeWin32.USN_RECORD>();
        Dictionary<UInt64, PInvokeWin32.USN_RECORD> mFiles = new Dictionary<ulong, PInvokeWin32.USN_RECORD>();       

        /// <summary>
        /// Open a drive to read
        /// </summary>
        /// <param name="Drive"></param>
        public void Open(String Drive)
        {
            Close();

            String DriveRoot = @"\\.\" + Drive;// +Path.DirectorySeparatorChar;

            mDriveHandle = PInvokeWin32.CreateFile(DriveRoot,
                PInvokeWin32.GENERIC_READ,
                PInvokeWin32.FILE_SHARE_READ, //| PInvokeWin32.FILE_SHARE_WRITE,
                IntPtr.Zero,
                PInvokeWin32.OPEN_EXISTING,
                //PInvokeWin32.FILE_FLAG_BACKUP_SEMANTICS,
                0,
                IntPtr.Zero);

            if (mDriveHandle.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)
            {
                throw new IOException("Unable to get root", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            GetUSN();
        }

        private unsafe void GetUSN()
        {
            uint bytesReturned = 0;
            mUSN = new PInvokeWin32.USN_JOURNAL_DATA();

            bool retOK = PInvokeWin32.DeviceIoControl(mDriveHandle,
                PInvokeWin32.FSCTL_QUERY_USN_JOURNAL,
                IntPtr.Zero,
                0,
                out mUSN,
                sizeof(PInvokeWin32.USN_JOURNAL_DATA),  // Size Of Out Buffer  
                out bytesReturned,          // Bytes Returned  
                IntPtr.Zero);
            // mUSN.UsnJournalID the id of usn journal
            if (!retOK)
            {
                throw new IOException("DeviceIoControl() returned false", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        public void Close()
        {
            if (mDriveHandle != IntPtr.Zero)
            {
                PInvokeWin32.CloseHandle(mDriveHandle);
            }
            mDriveHandle = IntPtr.Zero;
        }

        public unsafe void EnumFiles()
        {
            /// MFT ENUM DATA
            PInvokeWin32.MFT_ENUM_DATA MedData = new PInvokeWin32.MFT_ENUM_DATA();
            MedData.StartFileReferenceNumber = 0;
            MedData.LowUsn = mUSN.FirstUsn;
            MedData.HighUsn = mUSN.NextUsn;
            IntPtr MedBuffer = Marshal.AllocHGlobal(sizeof(PInvokeWin32.MFT_ENUM_DATA));
            Marshal.StructureToPtr(MedData, MedBuffer, true);

            // Open Buffer
            int OutBufferSize = sizeof(UInt64) + 0x10000;
            IntPtr OutBuffer = Marshal.AllocHGlobal(OutBufferSize);

            uint BytesReturned = 0;

            while (PInvokeWin32.DeviceIoControl(mDriveHandle,
                PInvokeWin32.FSCTL_ENUM_USN_DATA,
                MedBuffer,
                sizeof(PInvokeWin32.MFT_ENUM_DATA),
                OutBuffer,
                OutBufferSize,
                out BytesReturned,
                IntPtr.Zero))
            {
                IntPtr pUsnRecord = new IntPtr(OutBuffer.ToInt32() + sizeof(Int64));

                while (BytesReturned > 60)
                {
                    PInvokeWin32.USN_RECORD usn = new PInvokeWin32.USN_RECORD(pUsnRecord);

                    if (0 != (usn.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY))
                    {
                        /// 
                        /// Handle Directories
                        /// 
                        mDictionarys[usn.FileReferenceNumber] = usn;
                    }
                    else
                    {
                        ///
                        /// Handle Files
                        ///
                        mFiles[usn.FileReferenceNumber] = usn;
                    }
                    pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usn.RecordLength);
                    BytesReturned -= usn.RecordLength;
                }
                Marshal.WriteInt64(MedBuffer, Marshal.ReadInt64(OutBuffer, 0));
            }
            Marshal.FreeHGlobal(MedBuffer);
            Marshal.FreeHGlobal(OutBuffer);
        }

        public static unsafe UInt64 GetPathFRN(string FilePath)
        {
            IntPtr hDir = PInvokeWin32.CreateFile(FilePath, 
                0, 
                PInvokeWin32.FILE_SHARE_READ, 
                IntPtr.Zero, 
                PInvokeWin32.OPEN_EXISTING, 
                PInvokeWin32.FILE_FLAG_BACKUP_SEMANTICS, 
                IntPtr.Zero
                );

            if(hDir.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)
                throw new Exception();

            PInvokeWin32.BY_HANDLE_FILE_INFORMATION fi = new PInvokeWin32.BY_HANDLE_FILE_INFORMATION();
            PInvokeWin32.GetFileInformationByHandle(hDir, out fi);

            PInvokeWin32.CloseHandle(hDir);

            return (fi.FileIndexHigh << 32) | fi.FileIndexLow;
        }                 
       
        System.IO.StreamWriter writer = new StreamWriter("files.log");
        public unsafe List<PInvokeWin32.USN_RECORD> ReadUSN(UInt64 startUsn)
        {
            int buffersize = sizeof(UInt64) + 65535;

            // Set READ_USN_JOURNAL_DATA
            PInvokeWin32.READ_USN_JOURNAL_DATA Rujd = new PInvokeWin32.READ_USN_JOURNAL_DATA();
            Rujd.StartUSN = startUsn;
            Rujd.ReasonMask = uint.MaxValue;
            Rujd.UsnJournalID = mUSN.UsnJournalID;
            Rujd.BytesToWaitFor = (ulong)buffersize;
            
            int SizeOfUsnJournalData = Marshal.SizeOf(Rujd);
            IntPtr UsnBuffer = Marshal.AllocHGlobal(SizeOfUsnJournalData);
            PInvokeWin32.ZeroMemory(UsnBuffer, SizeOfUsnJournalData);
            Marshal.StructureToPtr(Rujd, UsnBuffer, true);  
            
            // Set Output Buffer
            IntPtr pData = Marshal.AllocHGlobal(buffersize);
            PInvokeWin32.ZeroMemory(pData, buffersize);  
            uint outBytesReturned = 0;

            
            List<PInvokeWin32.USN_RECORD> USNRecords = new List<PInvokeWin32.USN_RECORD>();
            while (true)
            {
                Rujd.StartUSN = startUsn;
                Rujd.ReasonMask = uint.MaxValue;
                Rujd.UsnJournalID = mUSN.UsnJournalID;
                Rujd.BytesToWaitFor = (ulong)buffersize;
                Marshal.StructureToPtr(Rujd, UsnBuffer, true);  

                
                bool retOK = PInvokeWin32.DeviceIoControl(mDriveHandle,
                    PInvokeWin32.FSCTL_READ_USN_JOURNAL,
                    UsnBuffer,
                    sizeof(PInvokeWin32.READ_USN_JOURNAL_DATA),
                    pData,
                    buffersize,
                    out outBytesReturned,
                    IntPtr.Zero);

                if (!retOK)
                {
                    throw new IOException();
                }

                /// the first returned USN record
                IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64)); // skip first gap
                

                while (outBytesReturned > 60)
                {                    
                    PInvokeWin32.USN_RECORD p = new PInvokeWin32.USN_RECORD(pUsnRecord);
                    pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + p.RecordLength);
                    outBytesReturned -= p.RecordLength;

                    USNRecords.Add(p);
                    
                }
                if (startUsn == USNRecords[USNRecords.Count - 1].Usn)
                    break;
                startUsn = USNRecords[USNRecords.Count - 1].Usn;
            }
            return USNRecords;
        }
       
        public unsafe PInvokeWin32.USN_RECORD ReadFileUSN(String Path)
        {            
            String DevicePath = @"\\.\" + Path;
            IntPtr handle = PInvokeWin32.CreateFile(DevicePath,
                PInvokeWin32.GENERIC_READ,
                PInvokeWin32.FILE_SHARE_READ,
                IntPtr.Zero,
                PInvokeWin32.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (mDriveHandle.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)
            {
                throw new IOException();
            }
            int BufferSize = 200;
            IntPtr UsnBuffer = Marshal.AllocHGlobal(BufferSize);
            uint outBytesReturned = 0;

            bool retOK = PInvokeWin32.DeviceIoControl(handle, PInvokeWin32.FSCTL_READ_FILE_USN_DATA, IntPtr.Zero, 0, UsnBuffer, BufferSize, out outBytesReturned, IntPtr.Zero);
            if (!retOK)
            {
                throw new Exception();
            }
            PInvokeWin32.USN_RECORD p = new PInvokeWin32.USN_RECORD(UsnBuffer);
            
            Marshal.FreeHGlobal(UsnBuffer);

            return p;
        }

        public static void SelfTest()
        {
            MFT control = new MFT();
            control.Open("f:");            
            control.EnumFiles();
           
            foreach (var m in control.mDictionarys)
            {
                FolderDB.Add(m.Value);
            }

            foreach (var m in control.mFiles)
            {
                FolderDB.Add(m.Value);
            }

            FolderDB.Save();
            FolderDB.Load();
        }
    }
}
