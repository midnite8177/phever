using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tagtoo;
using System.IO;

namespace gentleman.USN
{
    public class PathDB
    {
        public Dictionary<UInt64, FileNameAndFrn> FileDB;
        public Dictionary<UInt64, FileNameAndFrn> FolderDB;

        public Dictionary<string, UInt64> LastUsn;
        
        public PathDB()
        {
            FileDB = new Dictionary<ulong, FileNameAndFrn>();
            FolderDB = new Dictionary<ulong, FileNameAndFrn>();
            LastUsn = new Dictionary<string, ulong>();
        }

        public bool Load(string folderpath)
        {
            if (File.Exists("Files.db") && File.Exists("Folders.db") && File.Exists("LastUsn.db"))
            {
                using (StreamReader reader = new StreamReader("Files.db"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var vs = line.Split('#');
                        FileDB[Convert.ToUInt64(vs[0])] = new FileNameAndFrn(vs[1], Convert.ToUInt64(vs[2]));
                    }
                }
                using (StreamReader reader = new StreamReader("Folders.db"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var vs = line.Split('#');
                        FolderDB[Convert.ToUInt64(vs[0])] = new FileNameAndFrn(vs[1], Convert.ToUInt64(vs[2]));
                    }
                }
                using (StreamReader reader = new StreamReader("LastUsn.db"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var vs = line.Split('#');
                        LastUsn[vs[0]] = Convert.ToUInt64(vs[1]);
                    }
                }
                return true;
            }
            return false;
        }
        public void Save(string folderpath)
        {
            using (StreamWriter writer = new StreamWriter("Files.db"))
            {
                foreach (var item in FileDB)
                {
                    writer.WriteLine(string.Format("{0}#{1}#{2}", item.Key, item.Value.Name, item.Value.ParentFrn));
                }
            }
            using (StreamWriter writer = new StreamWriter("Folders.db"))
            {
                foreach (var item in FileDB)
                {
                    writer.WriteLine(string.Format("{0}#{1}#{2}", item.Key, item.Value.Name, item.Value.ParentFrn));
                }
            }
            using (StreamWriter writer = new StreamWriter("LastUsn.db"))
            {
                foreach (var item in LastUsn)
                {
                    writer.WriteLine(string.Format("{0}#{1}", item.Key, item.Value));
                }
            }
        }
    }

    public class PathDBController
    {
        private PathDB mdb;
        private CChangeJournal mft;

        public static List<String> ExtFilters = new List<string>() { ".jpg", ".gif", ".jpeg", ".png", ".bmp" };

        public PathDBController()
        {
            mdb = new PathDB();
            mft = new CChangeJournal("c:");

            bool cached = mdb.Load(".");

            if (!cached) Build();
            else
            {
                Update();
            }
            mdb.Save(".");
        }

        public void Build()
        {
            

            
            var root = mft.GetRootFrn();
            mdb.FolderDB[root.Key] = root.Value;

            PInvokeWin32.USN_RECORD lastitem = null;
            foreach (var item in mft.EnumVolume(0, long.MaxValue))
            {
                if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                else
                {
                    var ext = System.IO.Path.GetExtension(item.FileName);
                    if (ExtFilters.Contains(ext.ToLower()))
                        mdb.FileDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                }
                lastitem = item;
            }

            mdb.LastUsn["c:"] = lastitem.Usn;

        }
        public void Update()
        {
            var q = mft.Query();
            foreach (var item in mft.ReadUSN(q.UsnJournalID, long.MaxValue, PInvokeWin32.USN_REASON_CLOSE))
            {
                if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                {
                    if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
                        mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                    if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
                        mdb.FolderDB[item.FileReferenceNumber].Name = item.FileName;
                    if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
                        mdb.FolderDB.Remove(item.FileReferenceNumber);
                }
                else
                {
                    if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
                        mdb.FileDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                    if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
                        mdb.FileDB[item.FileReferenceNumber].Name = item.FileName;
                    if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
                        mdb.FileDB.Remove(item.FileReferenceNumber);
                }
            }
        }
    }
}
