using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
//http://www.sqlite.org/cvstrac/wiki?p=SqliteWrappers SQLite with C#
//http://sqlite.phxsoftware.com/forums/t/76.aspx  Sample Code
/// http://msdn.microsoft.com/en-us/vbasic/bb688085.aspx linq to sql

namespace mftdb
{
    /// <summary>
    ///  Using In Memory DB right now
    /// </summary>
    public class DBControl
    {
        private string FOLDER_PATH = @".\folder{0}.db";
        private string FILE_PATH = @".\file{0}.db";
        private string UPDATELOG_PATH = @".\log{0}.db";

        private CChangeJournal mft;
        private Int64 LastUsn
        {
            get
            {
                DateTime LastDate = DateTime.MinValue;
                foreach (var item in UpdateLogs)
                {
                    if (item.Key > LastDate)                    
                        LastDate = item.Key;                    
                }
                return UpdateLogs[LastDate];
            }
        }
        private CChangeJournal.VolumnInfo DriveInfo;        

        private Dictionary<UInt64, FileNameAndFrn> FolderEntries;
        private Dictionary<UInt64, FileNameAndFrn> FileEntries;
        private Dictionary<DateTime, Int64> UpdateLogs;

        public DBControl(char drive)
        {
            mft = new CChangeJournal(drive);
            DriveInfo = mft.GetRootFrn();            

            FolderEntries = new Dictionary<ulong, FileNameAndFrn>();
            FileEntries = new Dictionary<ulong, FileNameAndFrn>();
            UpdateLogs = new Dictionary<DateTime, long>();

            ///1. Try To Load DB
            if (Load())
            {
                // 2. If Load Success, Try Update
                Update();
            }
            else
            {
                // Else Build New DB
                Build();                
            }
            // Save the DB
            Save();
        }

        public bool Load()
        {            
            var FolderPath = string.Format(FOLDER_PATH, DriveInfo.VolumeSerialNumber);
            var FilePath = string.Format(FILE_PATH, DriveInfo.VolumeSerialNumber);
            var LogPath = string.Format(UPDATELOG_PATH, DriveInfo.VolumeSerialNumber);

            if (File.Exists(FolderPath) && File.Exists(FilePath) && File.Exists(LogPath))
            {
                using (FileStream stream = new FileStream(FolderPath, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            ulong frn = reader.ReadUInt64();
                            string name = reader.ReadString();
                            ulong parent_frn = reader.ReadUInt64();

                            FolderEntries[frn] = new FileNameAndFrn(name, parent_frn);
                        }                        
                    }
                }
                using (FileStream stream = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            ulong frn = reader.ReadUInt64();
                            string name = reader.ReadString();
                            ulong parent_frn = reader.ReadUInt64();

                            FileEntries[frn] = new FileNameAndFrn(name, parent_frn);
                        }
                    }
                }
                using (FileStream stream = new FileStream(LogPath, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            long ticks = reader.ReadInt64();                            
                            Int64 usn = reader.ReadInt64();

                            UpdateLogs[new DateTime(ticks)] = usn;
                        }
                    }
                }      
                return true;
            }            
            return false;
        }
        public void Save()
        {
            var FolderPath = string.Format(FOLDER_PATH, DriveInfo.VolumeSerialNumber);
            var FilePath = string.Format(FILE_PATH, DriveInfo.VolumeSerialNumber);
            var LogPath = string.Format(UPDATELOG_PATH, DriveInfo.VolumeSerialNumber);

            using (FileStream stream = new FileStream(FolderPath, FileMode.Create))
            {
                using(BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    foreach(var item in FolderEntries) {
                        writer.Write(item.Key);
                        writer.Write(item.Value.Name);
                        writer.Write(item.Value.ParentFrn);
                    }
                }
            }
            using (FileStream stream = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    foreach (var item in FileEntries)
                    {
                        writer.Write(item.Key);
                        writer.Write(item.Value.Name);
                        writer.Write(item.Value.ParentFrn);
                    }
                }
            }
            using (FileStream stream = new FileStream(LogPath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    foreach (var item in UpdateLogs)
                    {
                        writer.Write(item.Key.Ticks);
                        writer.Write(item.Value);                     
                    }
                }
            }
        }

        public void Trace(UInt64 frn)
        {
            var q = mft.Query();

            using (System.IO.StreamWriter writer = new StreamWriter("dump.log"))
            {
                foreach (var item in mft.ReadUSN(q.UsnJournalID, 0, PInvokeWin32.USN_REASON_CLOSE))
                {
                    if (item.FileReferenceNumber == frn)
                        writer.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6}",
                            item.Usn,
                            item.FileReferenceNumber,
                            item.FileName,
                            item.ParentFileReferenceNumber,
                            item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE,
                            item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME,
                            item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE));
                }
            }

        }
        public void Build()
        {            
            /// Add Drive Info
            FolderEntries[DriveInfo.FileReferenceNumber] = new FileNameAndFrn(string.Format("{0}:",DriveInfo.RootLetter), 0);

            var q = mft.Query();            

            /// Enum all mft from 0 to q.NextUsn
            foreach (var item in mft.EnumVolume(0, q.NextUsn))
            {
                /// In some reason, the usns not return in increase order
                if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)                
                    FolderEntries[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);                
                else                
                    FileEntries[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);                
            }

            UpdateLogs[DateTime.Now] = q.NextUsn;
        }
        public void Update()
        {
            /// 1. Find Out Last USN
            var lastlog = LastUsn;

            var q = mft.Query();
            
            foreach (var item in mft.ReadUSN(q.UsnJournalID, LastUsn, PInvokeWin32.USN_REASON_CLOSE))
            {
                if ((item.Reason & PInvokeWin32.USN_REASON_CLOSE) != 0)
                {
                    if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    {                        
                        /// Insert New Record
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)                        
                            FolderEntries[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                        
                        //mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                        if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
                            FolderEntries[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);

                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
                            FolderEntries.Remove(item.FileReferenceNumber);
                    }
                    else
                    {
                        /// Insert New Record
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
                            FileEntries[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                        if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
                            FileEntries[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
                            FileEntries.Remove(item.FileReferenceNumber);
                    }
                }
            }
            
            UpdateLogs[DateTime.Now] = mft.CurUsn;            
        }

        public IEnumerable<string> Query(Converter<string, bool> query, bool IncludeFolder)
        {
            if (IncludeFolder)
            {
                foreach (var item in FolderEntries)
                {
                    if (query(item.Value.Name))
                        yield return GetFolderPath(item.Key);
                }
            }
            foreach (var item in FileEntries)
            {
                if (query(item.Value.Name))
                    yield return GetFilePath(item.Key);
            }
        }
        public IEnumerable<string> Query(string query, bool IncludeFolder)
        {
            return Query(a => a.ToLower() == query.ToLower(), IncludeFolder);                    
        }        

        public string GetFolderPath(ulong Frn)
        {
            List<String> PathCache = new List<string>();
            do
            {
                PathCache.Insert(0, FolderEntries[Frn].Name);                
                Frn = FolderEntries[Frn].ParentFrn;
            } while (Frn != 0);

            return string.Join(@"\", PathCache.ToArray());
        }
        public string GetFilePath(ulong Frn)
        {            
            return GetFolderPath(FileEntries[Frn].ParentFrn) + @"\" + FileEntries[Frn].Name;
        }
    }
}
