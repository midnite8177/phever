using System;  
using System.Collections;  
using System.Collections.Generic;  
using System.Text;  
using System.Runtime.InteropServices;  
using System.IO;  
using System.ComponentModel;

// http://www.microsoft.com/msj/1099/journal2/journal2.aspx
// http://www.microsoft.com/msj/0999/journal/journal.aspx
namespace mftdb
{
    public class FileNameAndFrn
    {
        #region Properties
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private UInt64 _parentFrn;
        public UInt64 ParentFrn
        {
            get { return _parentFrn; }
            set { _parentFrn = value; }
        }
        #endregion

        #region Constructor
        public FileNameAndFrn(string name, UInt64 parentFrn)
        {
            if (name != null && name.Length > 0)
            {
                _name = name;
            }
            else
            {
                throw new ArgumentException("Invalid argument: null or Length = zero", "name");
            }
            if (!(parentFrn < 0))
            {
                _parentFrn = parentFrn;
            }
            else
            {
                throw new ArgumentException("Invalid argument: less than zero", "parentFrn");
            }
        }
        #endregion
    }
 
    class CChangeJournal
    {
        public char Drive { get; private set; }
        private IntPtr ChangeJournalRootHandle = IntPtr.Zero;
        public Int64 CurUsn = 0;

        public class VolumnInfo
        {
            public char RootLetter;
            public UInt64 FileReferenceNumber;
            public uint VolumeSerialNumber;
        }

        public CChangeJournal(char drive)
        {           
            Drive = drive;

            string vol = string.Format("\\\\.\\{0}:", drive);

            ChangeJournalRootHandle = PInvokeWin32.CreateFile(vol,
                 PInvokeWin32.GENERIC_READ | PInvokeWin32.GENERIC_WRITE,
                 PInvokeWin32.FILE_SHARE_READ | PInvokeWin32.FILE_SHARE_WRITE,
                 IntPtr.Zero,
                 PInvokeWin32.OPEN_EXISTING,
                 0,
                 IntPtr.Zero);

            if (ChangeJournalRootHandle.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)
            {
                throw new IOException("CreateFile() returned invalid handle",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
        public VolumnInfo GetRootFrn()
        {            
            string driveRoot = string.Format("\\\\.\\{0}:", Drive);
            driveRoot = string.Concat(driveRoot, Path.DirectorySeparatorChar);
            IntPtr hRoot = PInvokeWin32.CreateFile(driveRoot,
                    0,
                    PInvokeWin32.FILE_SHARE_READ | PInvokeWin32.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    PInvokeWin32.OPEN_EXISTING,
                    PInvokeWin32.FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);

            if (hRoot.ToInt32() != PInvokeWin32.INVALID_HANDLE_VALUE)
            {
                PInvokeWin32.BY_HANDLE_FILE_INFORMATION fi = new PInvokeWin32.BY_HANDLE_FILE_INFORMATION();
                var bRtn = PInvokeWin32.GetFileInformationByHandle(hRoot, out fi);
                if (bRtn != false)
                {                    
                    UInt64 fileIndexHigh = (UInt64)fi.FileIndexHigh;
                    UInt64 indexRoot = (fileIndexHigh << 32) | fi.FileIndexLow;

                    return new VolumnInfo
                    {
                        FileReferenceNumber = indexRoot,
                        RootLetter = Drive,
                        VolumeSerialNumber = fi.VolumeSerialNumber
                    };                    
                }
                else
                {
                    throw new IOException("GetFileInformationbyHandle() returned invalid handle",
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }
            else
            {
                throw new IOException("Unable to get root frn entry", new Win32Exception(Marshal.GetLastWin32Error()));
            }                         
        }
        ~CChangeJournal()
        {
            if(ChangeJournalRootHandle.ToInt32() != PInvokeWin32.INVALID_HANDLE_VALUE)
                PInvokeWin32.CloseHandle(ChangeJournalRootHandle);
        }
        public PInvokeWin32.USN_JOURNAL_DATA Query()
        {
            PInvokeWin32.USN_JOURNAL_DATA ujd = new PInvokeWin32.USN_JOURNAL_DATA();

            uint bytesReturned = 0;
            var bOk = PInvokeWin32.DeviceIoControl(ChangeJournalRootHandle,                           // Handle to drive  
                PInvokeWin32.FSCTL_QUERY_USN_JOURNAL,   // IO Control Code  
                IntPtr.Zero,                // In Buffer  
                0,                          // In Buffer Size  
                out ujd,                    // Out Buffer  
                PInvokeWin32.SizeOf_USN_JOURNAL_DATA,        // Size Of Out Buffer  
                out bytesReturned,          // Bytes Returned  
                IntPtr.Zero);               // lpOverlapped  

            if (!bOk)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }


            return ujd;
        }

        public IEnumerable<PInvokeWin32.USN_RECORD> EnumVolume(Int64 lowUsn, Int64 highUsn)
        {
            PInvokeWin32.MFT_ENUM_DATA med;
            med.StartFileReferenceNumber = 0;
            med.LowUsn = lowUsn;
            med.HighUsn = highUsn;

            IntPtr medBuffer = IntPtr.Zero;
            IntPtr pData = IntPtr.Zero;

            try
            {
                medBuffer = Marshal.AllocHGlobal(PInvokeWin32.SizeOf_MFT_ENUM_DATA);
                PInvokeWin32.ZeroMemory(medBuffer, PInvokeWin32.SizeOf_MFT_ENUM_DATA);
                Marshal.StructureToPtr(med, medBuffer, true);

                int buffersize = sizeof(UInt64) + 0x10000;
                pData = Marshal.AllocHGlobal(buffersize);
                PInvokeWin32.ZeroMemory(pData, buffersize);
                uint outBytesReturned = 0;

                while (true)
                {
                    bool stOK = PInvokeWin32.DeviceIoControl(
                    ChangeJournalRootHandle,           // VolumHandler
                    PInvokeWin32.FSCTL_ENUM_USN_DATA,   // Command
                    medBuffer,                          // Command Block, inputer buffer
                    PInvokeWin32.SizeOf_MFT_ENUM_DATA,                // Command Block Size
                    pData,                              // Output buffer
                    buffersize,           // size of output buffer
                    out outBytesReturned,               // Return Value (Error Message)
                    IntPtr.Zero);

                    if (stOK == false) break;

                    IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64));

                    while (outBytesReturned > PInvokeWin32.SizeOf_USN_RECORD)
                    {
                        PInvokeWin32.USN_RECORD usn = new PInvokeWin32.USN_RECORD(pUsnRecord);                        
                        yield return usn;

                        pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usn.RecordLength);
                        outBytesReturned -= usn.RecordLength;
                    }
                    /// Write the new StartFileReferenceNumber
                    Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
                }
            }
            finally
            {
                if (medBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(medBuffer);
                if (pData != IntPtr.Zero)
                    Marshal.FreeHGlobal(pData);                
            }
            yield break;
        }        
        public IEnumerable<PInvokeWin32.USN_RECORD> ReadUSN(ulong USNJournalID, Int64 lowUsn, uint ReasonMask)
        {
            return ReadUSN(USNJournalID, lowUsn, ReasonMask, 0, 0);
        }
        public IEnumerable<PInvokeWin32.USN_RECORD> ReadUSN(ulong USNJournalID, Int64 lowUsn, uint ReasonMask, ulong TimeOut, ulong ByteToWait)
        {
            int buffersize = sizeof(UInt64) + 65535;            
            // Set READ_USN_JOURNAL_DATA
            PInvokeWin32.READ_USN_JOURNAL_DATA Rujd;

            /// Document: 
            /// http://www.microsoft.com/msj/0999/journal/journal.aspx
            /// http://www.microsoft.com/msj/1099/journal2/journal2.aspx
            /// 
            /// Timeout:
            /// Timeout is a value for use with the BytesToWaitFor member. 
            /// It does not guarantee that DeviceIoControl will return after 
            /// the specified timeout, but rather it specifies how often the 
            /// system should check whether requested data is available. 
            /// This member is not like other conventional Win32¨ timeout 
            /// parameters that use milliseconds. Instead, this member uses 
            /// the same resolution as the Win32 FILETIME structure 
            /// (100-nanosecond intervals—one second has ten million intervals). 
            /// A value of zero specifies no timeout (or infinite). 
            /// A fixed timeout is specified using negative values 
            /// (even though this is an unsigned variable). 
            /// For example, a timeout of 25 seconds can be expressed as 
            /// (DWORDLONG)(-2500000000). 
            /// The Timeout member is ignored if DeviceIoControl is 
            /// called with an asynchronous request. 
            /// BytesToWaitFor:
            /// Don't confuse the BytesToWaitFor member with the output buffer 
            /// size or the count of bytes returned by DeviceIoControl. 
            /// If this member is set to zero, the function will return immediately, 
            /// even if it found no matching records in the journal. If this member 
            /// is nonzero, the system will not return until it has found at least 
            /// one record to return. BytesToWaitFor specifies how often the system 
            /// will recheck the journal to see whether any matching records have been 
            /// created. For example, if you specify 16384, the system will only examine 
            /// the journal for new records after a new 16KB block of raw data has been 
            /// added. This prevents a process from using too many resources when many 
            /// records are being added. If the Timeout and BytesToWaitFor members are 
            /// both nonzero, the system also checks records if the timeout period expires 
            /// before the journal has grown by the specified number of bytes. 
            /// If BytesToWaitFor is nonzero, but records are found that match the user's 
            /// request, the DeviceIoControl function will return immediately; that is, the 
            /// BytesToWaitFor and TimeOut members only have an effect when there are not any 
            /// existing records that fulfill the ReasonMask/ReturnOnlyOnClose requirements.

            Rujd.StartUSN = lowUsn;
            Rujd.ReasonMask = ReasonMask;
            Rujd.UsnJournalID = USNJournalID;
            Rujd.BytesToWaitFor = ByteToWait; /// Set to 0 for no waiting, otherwise it will notice new record
            Rujd.Timeout = TimeOut;        /// When BytesToWaitfor is 0, the timeout is ignore
            Rujd.ReturnOnlyOnClose = 0;

            IntPtr UsnBuffer = IntPtr.Zero;
            IntPtr pData = IntPtr.Zero;

            try
            {
                // Set User Buffer
                UsnBuffer = Marshal.AllocHGlobal(PInvokeWin32.SizeOf_READ_USN_JOURNAL_DATA);
                PInvokeWin32.ZeroMemory(UsnBuffer, PInvokeWin32.SizeOf_READ_USN_JOURNAL_DATA);
                Marshal.StructureToPtr(Rujd, UsnBuffer, true);

                // Set Output Buffer
                pData = Marshal.AllocHGlobal(buffersize);
                PInvokeWin32.ZeroMemory(pData, buffersize);
                uint outBytesReturned = 0;

                Int64 startUsn = lowUsn;

                while (true)
                {
                    Rujd.StartUSN = startUsn;
                    Rujd.ReasonMask = ReasonMask;
                    Rujd.UsnJournalID = USNJournalID;
                    
                    Marshal.StructureToPtr(Rujd, UsnBuffer, true);

                    var retOK = PInvokeWin32.DeviceIoControl(ChangeJournalRootHandle,
                        PInvokeWin32.FSCTL_READ_USN_JOURNAL,
                        UsnBuffer,
                        PInvokeWin32.SizeOf_READ_USN_JOURNAL_DATA,
                        pData,
                        buffersize,
                        out outBytesReturned,
                        IntPtr.Zero);

                    if (retOK == false)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    
                    /// the first returned USN record
                    /// The first Int64 are next usn number!!
                    /// If there are no more record, it is the next usn
                    startUsn = Marshal.ReadInt64(pData);
                    CurUsn = startUsn;

                    IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64)); 
                    PInvokeWin32.USN_RECORD p = null;
                    while (outBytesReturned > 60)
                    {
                        p = new PInvokeWin32.USN_RECORD(pUsnRecord);
                        pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + p.RecordLength);
                        outBytesReturned -= p.RecordLength;

                        yield return p;

                    }
                    if (p == null || startUsn == p.Usn)
                        break;                    
                }
            }
            finally
            {
                if (pData != IntPtr.Zero)
                    Marshal.FreeHGlobal(pData);
                if (UsnBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(UsnBuffer);
            }
            yield break;
        }
        /// <summary>
        /// The currect File System status
        /// </summary>
        /// <param name="lowUsn"></param>
        /// <param name="highUsn"></param>
        /// <param name="FileExtensionFilter"></param>
        /// 
        //public Dictionary<UInt64, FileNameAndFrn> Directories { get; private set; }
        //public Dictionary<UInt64, FileNameAndFrn> Files { get; private set; }
        //public void EnumVolume(Int64 lowUsn, Int64 highUsn, string[] FileExtensionFilter)
        //{            
        //    PInvokeWin32.MFT_ENUM_DATA med; // The Control Block
        //    med.StartFileReferenceNumber = 0;
        //    med.LowUsn = lowUsn;
        //    med.HighUsn = highUsn;  // the next usn writting point

        //    IntPtr medBuffer = IntPtr.Zero;
        //    IntPtr pData = IntPtr.Zero;

        //    try
        //    {
        //        medBuffer = Marshal.AllocHGlobal(PInvokeWin32.SizeOf_MFT_ENUM_DATA);
        //        PInvokeWin32.ZeroMemory(medBuffer, PInvokeWin32.SizeOf_MFT_ENUM_DATA);
        //        Marshal.StructureToPtr(med, medBuffer, true);

        //        int buffersize = sizeof(UInt64) + 0x10000;
        //        pData = Marshal.AllocHGlobal(buffersize);
        //        PInvokeWin32.ZeroMemory(pData, buffersize);
        //        uint outBytesReturned = 0;

        //        while (false != PInvokeWin32.DeviceIoControl(
        //            ChangeJournalRootHandle,           // VolumHandler
        //            PInvokeWin32.FSCTL_ENUM_USN_DATA,   // Command
        //            medBuffer,                          // Command Block, inputer buffer
        //            PInvokeWin32.SizeOf_MFT_ENUM_DATA,                // Command Block Size
        //            pData,                              // Output buffer
        //            buffersize,           // size of output buffer
        //            out outBytesReturned,               // Return Value (Error Message)
        //            IntPtr.Zero))                       // For Async Called, not used Now
        //        {
        //            IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64));

        //            while (outBytesReturned > PInvokeWin32.SizeOf_USN_RECORD)
        //            {
        //                PInvokeWin32.USN_RECORD usn = new PInvokeWin32.USN_RECORD(pUsnRecord);

        //                if (0 != (usn.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY))
        //                {
        //                    //  
        //                    // handle directories  
        //                    //  
        //                    if (!Directories.ContainsKey(usn.FileReferenceNumber))
        //                    {
        //                        Directories.Add(usn.FileReferenceNumber,
        //                            new FileNameAndFrn(usn.FileName, usn.ParentFileReferenceNumber));
        //                    }
        //                    else
        //                    {   // this is debug code and should be removed when we are certain that  
        //                        // duplicate frn's don't exist on a given drive.  To date, this exception has  
        //                        // never been thrown.  Removing this code improves performance....  
        //                        throw new Exception(string.Format("Duplicate FRN: {0} for {1}",
        //                            usn.FileReferenceNumber, usn.FileName));
        //                    }
        //                }
        //                else
        //                {
        //                    //   
        //                    // handle files  
        //                    //  
        //                    bool add = true;
        //                    string s = Path.GetExtension(usn.FileName).ToLower();

        //                    // file filter 
        //                    if (FileExtensionFilter != null && FileExtensionFilter.Length != 0)
        //                    {
        //                        add = false;

        //                        foreach (string extension in FileExtensionFilter)
        //                        {
        //                            if (0 == string.Compare(s, extension, true))
        //                            {
        //                                add = true;
        //                                break;
        //                            }
        //                        }
        //                    }
        //                    if (add)
        //                    {
        //                        if (!Files.ContainsKey(usn.FileReferenceNumber))
        //                        {
        //                            Files.Add(usn.FileReferenceNumber,
        //                                new FileNameAndFrn(usn.FileName, usn.ParentFileReferenceNumber));
        //                        }
        //                        else
        //                        {
        //                            FileNameAndFrn frn = Files[usn.FileReferenceNumber];
        //                            if (0 != string.Compare(usn.FileName, frn.Name, true))
        //                            {
        //                                //Log.InfoFormat(  
        //                                //    "Attempt to add duplicate file reference number: {0} for file {1}, file from index {2}",  
        //                                //    usn.FileReferenceNumber, usn.FileName, frn.Name);  
        //                                throw new Exception(string.Format("Duplicate FRN: {0} for {1}",
        //                                    usn.FileReferenceNumber, usn.FileName));
        //                            }
        //                        }
        //                    }
        //                }
        //                pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usn.RecordLength);
        //                outBytesReturned -= usn.RecordLength;
        //            }
        //            Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
        //        }
        //    }
        //    finally
        //    {
        //        if (pData != IntPtr.Zero) 
        //            Marshal.FreeHGlobal(pData);
        //        if(medBuffer != IntPtr.Zero)
        //            Marshal.FreeHGlobal(medBuffer);
        //    }
        //}        

        //public void UpdateVolume(ulong USNJournalID, Int64 lowUsn)
        //{
        //    int buffersize = sizeof(UInt64) + 65535;
        //    uint ReasonMask = PInvokeWin32.USN_REASON_CLOSE;

        //    // Set READ_USN_JOURNAL_DATA
        //    PInvokeWin32.READ_USN_JOURNAL_DATA Rujd;

        //    Rujd.StartUSN = (UInt64)lowUsn;
        //    Rujd.ReasonMask = ReasonMask;
        //    Rujd.UsnJournalID = USNJournalID;
        //    Rujd.BytesToWaitFor = (ulong)buffersize;
        //    Rujd.Timeout = 0;
        //    Rujd.ReturnOnlyOnClose = 0;

        //    IntPtr UsnBuffer = Marshal.AllocHGlobal(PInvokeWin32.SizeOf_READ_USN_JOURNAL_DATA);
        //    PInvokeWin32.ZeroMemory(UsnBuffer, PInvokeWin32.SizeOf_READ_USN_JOURNAL_DATA);
        //    Marshal.StructureToPtr(Rujd, UsnBuffer, true);

        //    // Set Output Buffer
        //    IntPtr pData = Marshal.AllocHGlobal(buffersize);
        //    PInvokeWin32.ZeroMemory(pData, buffersize);
        //    uint outBytesReturned = 0;

        //    UInt64 startUsn = (UInt64)lowUsn;

        //    //List<PInvokeWin32.USN_RECORD> USNRecords = new List<PInvokeWin32.USN_RECORD>();
        //    while (true)
        //    {
        //        Rujd.StartUSN = startUsn;
        //        Rujd.ReasonMask = ReasonMask;
        //        Rujd.UsnJournalID = USNJournalID;
        //        Rujd.BytesToWaitFor = (ulong)buffersize;
        //        Marshal.StructureToPtr(Rujd, UsnBuffer, true);

        //        var retOK = PInvokeWin32.DeviceIoControl(ChangeJournalRootHandle,
        //            PInvokeWin32.FSCTL_READ_USN_JOURNAL,
        //            UsnBuffer,
        //            PInvokeWin32.SizeOf_READ_USN_JOURNAL_DATA,
        //            pData,
        //            buffersize,
        //            out outBytesReturned,
        //            IntPtr.Zero);

        //        if (retOK == false)
        //        {
        //            throw new Win32Exception(Marshal.GetLastWin32Error());
        //        }

        //        /// the first returned USN record
        //        IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64)); // skip first gap
        //        PInvokeWin32.USN_RECORD p = null;
        //        while (outBytesReturned > 60)
        //        {
        //            p = new PInvokeWin32.USN_RECORD(pUsnRecord);
        //            pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + p.RecordLength);
        //            outBytesReturned -= p.RecordLength;

        //            //USNRecords.Add(p);
        //            // The order is important
        //            if ((p.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
        //            {
        //                if ((p.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
        //                    Directories[p.FileReferenceNumber] = new FileNameAndFrn(p.FileName, p.ParentFileReferenceNumber);
        //                if ((p.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
        //                    Directories[p.FileReferenceNumber].Name = p.FileName;
        //                if ((p.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
        //                    Directories.Remove(p.FileReferenceNumber);
        //            }
        //            else
        //            {
        //                if ((p.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
        //                    Files[p.FileReferenceNumber] = new FileNameAndFrn(p.FileName, p.ParentFileReferenceNumber);
        //                if ((p.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
        //                    Files[p.FileReferenceNumber].Name = p.FileName;
        //                if ((p.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
        //                    Files.Remove(p.FileReferenceNumber);
        //            }

        //        }
        //        if (startUsn == p.Usn)
        //            break;
        //        startUsn = p.Usn;
        //    }            
        //}

        //public string GetFilePath(FileNameAndFrn file)
        //{
        //    List<string> path = new List<string>() { file.Name };

        //    var parent = file.ParentFrn;
        //    while (Directories.ContainsKey(parent))
        //    {
        //        var pitem = Directories[parent];
        //        path.Insert(0, pitem.Name);
        //        parent = pitem.ParentFrn;
        //    }
        //    path[0] = path[0].Replace("\\\\.\\", "").Replace("\\", "");
        //    return string.Join(@"\", path.ToArray());
        //}
        //public static void SelfTest()
        //{
        //    CChangeJournal mft = new CChangeJournal("c:");
        //    foreach (var i in mft.EnumVolume(0, long.MaxValue))
        //    {
        //        var x = i;
        //    }
        //    var j = mft.Query();
        //    mft.EnumVolume(0, long.MaxValue, null);
        //    mft.UpdateVolume(j.UsnJournalID, 0);
        //    //mft.EnumVolume(0, 0, null);

        //    //var usn = mft.ReadFileUSN(@"C:\Users\lucemia\Desktop\durarara-trust me [ed].mp3");            
                        
        //    //CChangeJournal mft = new CChangeJournal();

        //    //StreamWriter folderwriter = new StreamWriter("folder.log");            
        //    //var r = mft.EnumerateVolume("c:", usn.Usn, new string[] { ".jpg", ".gif", ".bmp", ".jpeg", ".png" });            

        //    //var r1 = mft.ReadUSN(0);

        //    //StreamWriter filewriter = new StreamWriter("files.log");
        //    //foreach (KeyValuePair<UInt64, FileNameAndFrn> entry in result)
        //    //{
        //    //    filewriter.WriteLine(string.Format("{0} {1} {2}", entry.Key, entry.Value.Name, entry.Value.ParentFrn));
        //    //}                        
        //    //foreach (var x in mft._directories)  
        //    //{
        //    //    folderwriter.WriteLine(string.Format("{0} {1} {2}", x.Key, x.Value.Name, x.Value.ParentFrn));
        //    //}

            
        //    //filewriter.Close();
        //    //folderwriter.Close();
        //    //writer.ReadKey();
        //}           
    }
     
}
