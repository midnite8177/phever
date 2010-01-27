using System;  
using System.Collections;  
using System.Collections.Generic;  
using System.Text;  
using System.Runtime.InteropServices;  
using System.IO;  
using System.ComponentModel;
// http://www.microsoft.com/msj/1099/journal2/journal2.aspx
// http://www.microsoft.com/msj/0999/journal/journal.aspx
namespace Tagtoo
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
        public CChangeJournal(string drive)
        {

        }

        public Dictionary<UInt64, FileNameAndFrn> EnumerateVolume(string drive,ulong lowUsn,  string[] fileExtensions)
        {            
            _drive = drive;
            Dictionary<UInt64, FileNameAndFrn> files = new Dictionary<UInt64, FileNameAndFrn>();

            IntPtr medBuffer = IntPtr.Zero;
            try
            {
                //GetRootFrnEntry();
                GetRootHandle();

                CreateChangeJournal();

                SetupMFT_Enum_DataBuffer(ref medBuffer, lowUsn);
                EnumerateFiles(medBuffer, ref files, fileExtensions);
            }
            catch (Exception e)
            {
                //Log.Info(e.Message, e);
                Exception innerException = e.InnerException;
                while (innerException != null)
                {
                    //Log.Info(innerException.Message, innerException);
                    innerException = innerException.InnerException;
                }
                throw new ApplicationException("Error in EnumerateVolume()", e);
            }
            finally
            {
                if (_changeJournalRootHandle.ToInt32() != PInvokeWin32.INVALID_HANDLE_VALUE)
                {
                    //PInvokeWin32.CloseHandle(_changeJournalRootHandle);
                }
                if (medBuffer != IntPtr.Zero)
                {
                    //Marshal.FreeHGlobal(medBuffer);
                }
            }
            return files;
        }
        private void GetRootFrnEntry()
        {
            string driveRoot = string.Concat("\\\\.\\", _drive);
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

                    FileNameAndFrn f = new FileNameAndFrn(driveRoot, 0);
                    _directories.Add(indexRoot, f);
                }
                else
                {
                    throw new IOException("GetFileInformationbyHandle() returned invalid handle",
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }
                PInvokeWin32.CloseHandle(hRoot);
            }
            else
            {
                throw new IOException("Unable to get root frn entry", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        } 
        private void GetRootHandle()  
        {  
            string vol = string.Concat("\\\\.\\", _drive);  
            _changeJournalRootHandle = PInvokeWin32.CreateFile(vol,  
                 PInvokeWin32.GENERIC_READ | PInvokeWin32.GENERIC_WRITE,  
                 PInvokeWin32.FILE_SHARE_READ | PInvokeWin32.FILE_SHARE_WRITE,  
                 IntPtr.Zero,  
                 PInvokeWin32.OPEN_EXISTING,  
                 0,  
                 IntPtr.Zero);  
            if (_changeJournalRootHandle.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)  
            {  
                throw new IOException("CreateFile() returned invalid handle",  
                    new Win32Exception(Marshal.GetLastWin32Error()));  
            }  
        }  
        unsafe private void EnumerateFiles(IntPtr medBuffer, ref Dictionary<ulong, FileNameAndFrn> files, string[] fileExtensions)  
        {  
            IntPtr pData = Marshal.AllocHGlobal(sizeof(UInt64) + 0x10000);  
            PInvokeWin32.ZeroMemory(pData, sizeof(UInt64) + 0x10000);  
            uint outBytesReturned = 0;  
 
            while (false != PInvokeWin32.DeviceIoControl(_changeJournalRootHandle,
                PInvokeWin32.FSCTL_ENUM_USN_DATA, 
                medBuffer,  
                sizeof(PInvokeWin32.MFT_ENUM_DATA), 
                pData, sizeof(UInt64) + 0x10000, 
                out outBytesReturned,  
                IntPtr.Zero))  
            {  
                IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64));  
                while (outBytesReturned > 60)  
                {                      
                    PInvokeWin32.USN_RECORD usn = new PInvokeWin32.USN_RECORD(pUsnRecord);
                   
                    if (0 != (usn.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY))  
                    {  
                        //  
                        // handle directories  
                        //  
                        if (!_directories.ContainsKey(usn.FileReferenceNumber))  
                        {  
                            
                            _directories.Add(usn.FileReferenceNumber,  
                                new FileNameAndFrn(usn.FileName, usn.ParentFileReferenceNumber));  
                        }  
                        else  
                        {   // this is debug code and should be removed when we are certain that  
                            // duplicate frn's don't exist on a given drive.  To date, this exception has  
                            // never been thrown.  Removing this code improves performance....  
                            throw new Exception(string.Format("Duplicate FRN: {0} for {1}",   
                                usn.FileReferenceNumber, usn.FileName));  
                        }  
                    }  
                    else  
                    {  
                        //   
                        // handle files  
                        //  
                        bool add = true;
                        string s = Path.GetExtension(usn.FileName).ToLower();  

                        // file filter 
                        if (fileExtensions != null && fileExtensions.Length != 0)  
                        {  
                            add = false;  
                            
                            foreach (string extension in fileExtensions)  
                            {  
                                if (0 == string.Compare(s, extension, true))  
                                {
                                    add = true;  
                                    break;  
                                }  
                            }  
                        }  
                        if (add)  
                        {  
                            if (!files.ContainsKey(usn.FileReferenceNumber))  
                            {  
                                files.Add(usn.FileReferenceNumber,
                                    new FileNameAndFrn(usn.FileName, usn.ParentFileReferenceNumber));  
                            }  
                            else  
                            {  
                                FileNameAndFrn frn = files[usn.FileReferenceNumber];  
                                if (0 != string.Compare(usn.FileName, frn.Name, true))  
                                {  
                                    //Log.InfoFormat(  
                                    //    "Attempt to add duplicate file reference number: {0} for file {1}, file from index {2}",  
                                    //    usn.FileReferenceNumber, usn.FileName, frn.Name);  
                                    throw new Exception(string.Format("Duplicate FRN: {0} for {1}",  
                                        usn.FileReferenceNumber, usn.FileName));  
                                }  
                            }  
                        }  
                    }  
                    pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usn.RecordLength);  
                    outBytesReturned -= usn.RecordLength;  
                }  
                Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));  
            }  
            Marshal.FreeHGlobal(pData);  
        }
        unsafe private void CreateChangeJournal()
        {
            // This function creates a journal on the volume. If a journal already  
            // exists this function will adjust the MaximumSize and AllocationDelta  
            // parameters of the journal  
            UInt64 MaximumSize = 0x800000;
            UInt64 AllocationDelta = 0x100000;
            UInt32 cb;
            PInvokeWin32.CREATE_USN_JOURNAL_DATA cujd;
            cujd.MaximumSize = MaximumSize;
            cujd.AllocationDelta = AllocationDelta;

            int sizeCujd = Marshal.SizeOf(cujd);
            IntPtr cujdBuffer = Marshal.AllocHGlobal(sizeCujd);
            PInvokeWin32.ZeroMemory(cujdBuffer, sizeCujd);
            Marshal.StructureToPtr(cujd, cujdBuffer, true);

            var fOk = PInvokeWin32.DeviceIoControl(_changeJournalRootHandle, PInvokeWin32.FSCTL_CREATE_USN_JOURNAL,
                cujdBuffer, sizeCujd, IntPtr.Zero, 0, out cb, IntPtr.Zero);
            if (fOk == false)
            {
                throw new IOException("DeviceIoControl() returned false", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
        unsafe private void SetupMFT_Enum_DataBuffer(ref IntPtr medBuffer, ulong lowUsn)
        {
            /// This Function query the statistics of volumn
            uint bytesReturned = 0;
            ujd = new PInvokeWin32.USN_JOURNAL_DATA();

            var bOk = PInvokeWin32.DeviceIoControl(_changeJournalRootHandle,                           // Handle to drive  
                PInvokeWin32.FSCTL_QUERY_USN_JOURNAL,   // IO Control Code  
                IntPtr.Zero,                // In Buffer  
                0,                          // In Buffer Size  
                out ujd,                    // Out Buffer  
                sizeof(PInvokeWin32.USN_JOURNAL_DATA),  // Size Of Out Buffer  
                out bytesReturned,          // Bytes Returned  
                IntPtr.Zero);               // lpOverlapped  
            if (bOk != false)
            {
                PInvokeWin32.MFT_ENUM_DATA med;
                med.StartFileReferenceNumber = 0;
                med.LowUsn = (long)lowUsn;
                med.HighUsn = ujd.NextUsn;  // the next usn writting point
                int sizeMftEnumData = Marshal.SizeOf(med);
                medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
                PInvokeWin32.ZeroMemory(medBuffer, sizeMftEnumData);
                Marshal.StructureToPtr(med, medBuffer, true);
            }
            else
            {
                throw new IOException("DeviceIoControl() returned false", new Win32Exception(Marshal.GetLastWin32Error()));
            }
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

            if (hDir.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)
                throw new Exception();

            PInvokeWin32.BY_HANDLE_FILE_INFORMATION fi = new PInvokeWin32.BY_HANDLE_FILE_INFORMATION();
            PInvokeWin32.GetFileInformationByHandle(hDir, out fi);

            PInvokeWin32.CloseHandle(hDir);

            return (fi.FileIndexHigh << 32) | fi.FileIndexLow;
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

            if (_changeJournalRootHandle.ToInt32() == PInvokeWin32.INVALID_HANDLE_VALUE)
            {
                throw new IOException();
            }
            int BufferSize = 200;
            IntPtr UsnBuffer = Marshal.AllocHGlobal(BufferSize);
            uint outBytesReturned = 0;

            var retOK = PInvokeWin32.DeviceIoControl(handle, PInvokeWin32.FSCTL_READ_FILE_USN_DATA, IntPtr.Zero, 0, UsnBuffer, BufferSize, out outBytesReturned, IntPtr.Zero);
            if (retOK == false)
            {
                throw new Exception();
            }
            PInvokeWin32.USN_RECORD p = new PInvokeWin32.USN_RECORD(UsnBuffer);

            Marshal.FreeHGlobal(UsnBuffer);

            return p;
        }
        public unsafe List<PInvokeWin32.USN_RECORD> ReadUSN(UInt64 startUsn)
        {
            int buffersize = sizeof(UInt64) + 65535;

            // Set READ_USN_JOURNAL_DATA
            PInvokeWin32.READ_USN_JOURNAL_DATA Rujd = new PInvokeWin32.READ_USN_JOURNAL_DATA();
            Rujd.StartUSN = startUsn;
            Rujd.ReasonMask = uint.MaxValue;
            Rujd.UsnJournalID = ujd.UsnJournalID;
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
                Rujd.UsnJournalID = ujd.UsnJournalID;
                Rujd.BytesToWaitFor = (ulong)buffersize;
                Marshal.StructureToPtr(Rujd, UsnBuffer, true);

                var retOK = PInvokeWin32.DeviceIoControl(_changeJournalRootHandle,
                    PInvokeWin32.FSCTL_READ_USN_JOURNAL,
                    UsnBuffer,
                    sizeof(PInvokeWin32.READ_USN_JOURNAL_DATA),
                    pData,
                    buffersize,
                    out outBytesReturned,
                    IntPtr.Zero);

                if (retOK == false)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new Win32Exception(err);
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
        public static void SelfTest()
        {
            CChangeJournal mft = new CChangeJournal();
            var usn = mft.ReadFileUSN(@"C:\Users\lucemia\Desktop\durarara-trust me [ed].mp3");            
            
            Dictionary<UInt64, FileNameAndFrn> result;
            //CChangeJournal mft = new CChangeJournal();

            //StreamWriter folderwriter = new StreamWriter("folder.log");            
            var r = mft.EnumerateVolume("c:", usn.Usn, new string[] { ".jpg", ".gif", ".bmp", ".jpeg", ".png" });            

            var r1 = mft.ReadUSN(0);

            //StreamWriter filewriter = new StreamWriter("files.log");
            //foreach (KeyValuePair<UInt64, FileNameAndFrn> entry in result)
            //{
            //    filewriter.WriteLine(string.Format("{0} {1} {2}", entry.Key, entry.Value.Name, entry.Value.ParentFrn));
            //}                        
            //foreach (var x in mft._directories)  
            //{
            //    folderwriter.WriteLine(string.Format("{0} {1} {2}", x.Key, x.Value.Name, x.Value.ParentFrn));
            //}

            
            //filewriter.Close();
            //folderwriter.Close();
            //writer.ReadKey();
        }
        private IntPtr _changeJournalRootHandle;
        public Dictionary<ulong, FileNameAndFrn> _directories = new Dictionary<ulong, FileNameAndFrn>();
        private string _drive = "";
        private PInvokeWin32.USN_JOURNAL_DATA ujd;        
    }
     
}
