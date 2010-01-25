using System;  
using System.Collections;  
using System.Collections.Generic;  
using System.Text;  
using System.Runtime.InteropServices;  
using System.IO;  
using System.ComponentModel;
//http://www.microsoft.com/msj/1099/journal2/journal2.aspx


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
        public void EnumerateVolume(string drive, out Dictionary<UInt64, FileNameAndFrn> files, string[] fileExtensions)
        {
            _drive = drive;
            files = new Dictionary<ulong, FileNameAndFrn>();
            IntPtr medBuffer = IntPtr.Zero;
            try
            {
                GetRootFrnEntry();
                GetRootHandle();

                CreateChangeJournal();

                SetupMFT_Enum_DataBuffer(ref medBuffer);
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
                    PInvokeWin32.CloseHandle(_changeJournalRootHandle);
                }
                if (medBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(medBuffer);
                }
            }
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
                bool bRtn = PInvokeWin32.GetFileInformationByHandle(hRoot, out fi);
                if (bRtn)
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

            bool fOk = PInvokeWin32.DeviceIoControl(_changeJournalRootHandle, PInvokeWin32.FSCTL_CREATE_USN_JOURNAL,
                cujdBuffer, sizeCujd, IntPtr.Zero, 0, out cb, IntPtr.Zero);
            if (!fOk)
            {
                throw new IOException("DeviceIoControl() returned false", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
        unsafe private void SetupMFT_Enum_DataBuffer(ref IntPtr medBuffer)
        {
            uint bytesReturned = 0;
            ujd = new PInvokeWin32.USN_JOURNAL_DATA();

            bool bOk = PInvokeWin32.DeviceIoControl(_changeJournalRootHandle,                           // Handle to drive  
                PInvokeWin32.FSCTL_QUERY_USN_JOURNAL,   // IO Control Code  
                IntPtr.Zero,                // In Buffer  
                0,                          // In Buffer Size  
                out ujd,                    // Out Buffer  
                sizeof(PInvokeWin32.USN_JOURNAL_DATA),  // Size Of Out Buffer  
                out bytesReturned,          // Bytes Returned  
                IntPtr.Zero);               // lpOverlapped  
            if (bOk)
            {
                PInvokeWin32.MFT_ENUM_DATA med;
                med.StartFileReferenceNumber = 0;
                med.LowUsn = 0;
                med.HighUsn = ujd.NextUsn;
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
        //unsafe private void GetRecords(UInt32 filter)
        //{
        //    filter = 2;
        //    PInvokeWin32.READ_USN_JOURNAL_DATA rujd;
        //    rujd.StartUSN = 0;
        //    rujd.ReasonMask = filter;
        //    rujd.BytesToWaitFor = 2000;
        //    rujd.UsnJournalID = ujd.UsnJournalID;

        //    IntPtr buffer = rujd;
        //    IntPtr pData = Marshal.AllocHGlobal(sizeof(UInt64) + 0x10000);
        //    PInvokeWin32.ZeroMemory(pData, sizeof(UInt64) + 0x10000);
        //    uint outBytesReturned = 0;

        //    PInvokeWin32.DeviceIoControl(_changeJournalRootHandle, PInvokeWin32.FSCTL_READ_USN_JOURNAL, out rujd, sizeof(PInvokeWin32.READ_USN_JOURNAL_DATA), pData, sizeof(UInt64) + 0x10000, out outBytesReturned, IntPtr.Zero);

        //    PInvokeWin32.USN_RECORD usn = new PInvokeWin32.USN_RECORD(pUsnRecord);
        //}
        public static void SelfTest()
        {
            Dictionary<UInt64, FileNameAndFrn> result;
            CChangeJournal mft = new CChangeJournal();

            StreamWriter folderwriter = new StreamWriter("folder.log");            
            mft.EnumerateVolume("c:", out result, new string[] { ".jpg",".gif", ".bmp", ".jpeg", ".png" });

            StreamWriter filewriter = new StreamWriter("files.log");
            foreach (KeyValuePair<UInt64, FileNameAndFrn> entry in result)
            {
                filewriter.WriteLine(string.Format("{0} {1} {2}", entry.Key, entry.Value.Name, entry.Value.ParentFrn));
            }                        
            foreach (var x in mft._directories)  
            {
                folderwriter.WriteLine(string.Format("{0} {1} {2}", x.Key, x.Value.Name, x.Value.ParentFrn));
            }

            
            filewriter.Close();
            folderwriter.Close();
            //writer.ReadKey();
        }
        private IntPtr _changeJournalRootHandle;
        public Dictionary<ulong, FileNameAndFrn> _directories = new Dictionary<ulong, FileNameAndFrn>();
        private string _drive = "";
        private PInvokeWin32.USN_JOURNAL_DATA ujd;        
    }
     
}
