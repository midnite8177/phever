using System;
using System.Collections.Generic;
using System.Linq;
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
                return UpdateLogs.OrderByDescending(a => a.Key).First().Value;
            }
        }
        private UInt64 DriveID;
        private string DriveName;

        private Dictionary<UInt64, FileNameAndFrn> FolderEntries;
        private Dictionary<UInt64, FileNameAndFrn> FileEntries;
        private Dictionary<DateTime, Int64> UpdateLogs;

        public DBControl(string drive)
        {
            mft = new CChangeJournal(drive);
            var root = mft.GetRootFrn();
            DriveID = root.Key;
            DriveName = root.Value.Name;


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
            var FolderPath = string.Format(FOLDER_PATH, DriveID);
            var FilePath = string.Format(FILE_PATH, DriveID);
            var LogPath = string.Format(UPDATELOG_PATH, DriveID);

            if (File.Exists(FolderPath) && File.Exists(FilePath) && File.Exists(LogPath))
            {
                using (StreamReader reader = new StreamReader(FolderPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var tokens = line.Split('/');
                        ulong frn = Convert.ToUInt64(tokens[0]);
                        string name = tokens[1];
                        ulong parent_frn = Convert.ToUInt64(tokens[2]);

                        FolderEntries[frn] = new FileNameAndFrn(name, parent_frn);
                    }
                }
                using (StreamReader reader = new StreamReader(FilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var tokens = line.Split('/');
                        ulong frn = Convert.ToUInt64(tokens[0]);
                        string name = tokens[1];
                        ulong parent_frn = Convert.ToUInt64(tokens[2]);

                        FileEntries[frn] = new FileNameAndFrn(name, parent_frn);
                    }
                }
                using (StreamReader reader = new StreamReader(LogPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var tokens = line.Split(';');
                        DateTime time = Convert.ToDateTime(tokens[0]);
                        Int64 USN = Convert.ToInt64(tokens[1]);

                        UpdateLogs[time] = USN;
                    }
                }                
                return true;
            }            
            return false;
        }
        public void Save()
        {
            var FolderPath = string.Format(FOLDER_PATH, DriveID);
            var FilePath = string.Format(FILE_PATH, DriveID);
            var LogPath = string.Format(UPDATELOG_PATH, DriveID);

            using (StreamWriter writer = new StreamWriter(FolderPath))
            {
                foreach (var item in FolderEntries)
                {
                    writer.WriteLine(string.Format("{0}/{1}/{2}", item.Key, item.Value.Name, item.Value.ParentFrn));
                }                
            }
            using (StreamWriter writer = new StreamWriter(FilePath))
            {
                foreach (var item in FolderEntries)
                {
                    writer.WriteLine(string.Format("{0}/{1}/{2}", item.Key, item.Value.Name, item.Value.ParentFrn));
                }                
            }
            using (StreamWriter writer = new StreamWriter(LogPath))
            {
                foreach (var item in UpdateLogs)
                {
                    writer.WriteLine(string.Format("{0};{1}", item.Key, item.Value));
                }                
            }

        }

        public void Dump(UInt64 frn)
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
            FolderEntries[DriveID] = new FileNameAndFrn(DriveName, 0);

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
            var lastlog = UpdateLogs.OrderByDescending(a => a.Key).First();

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

        public string GetFolderPath(ulong Frn)
        {
            List<String> PathCache = new List<string>();
            do
            {
                PathCache.Insert(0, FolderEntries[Frn].Name);                
                Frn = FolderEntries[Frn].ParentFrn;
            } while (Frn != 0);

            return string.Join("//", PathCache.ToArray());
        }
        public string GetFilePath(ulong Frn)
        {            
            return GetFolderPath(FileEntries[Frn].ParentFrn) + "//" + FileEntries[Frn].Name;
        }
    }
}
