using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tagtoo;
using System.IO;
using System.Data.SQLite;
//http://www.sqlite.org/cvstrac/wiki?p=SqliteWrappers SQLite with C#
//http://sqlite.phxsoftware.com/forums/t/76.aspx  Sample Code

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
            //using (StreamWriter writer = new StreamWriter("Files.db"))
            //{
            //    foreach (var item in FileDB)
            //    {
            //        writer.WriteLine(string.Format("{0}#{1}#{2}", item.Key, item.Value.Name, item.Value.ParentFrn));
            //    }
            //}
            //using (StreamWriter writer = new StreamWriter("Folders.db"))
            //{
            //    foreach (var item in FileDB)
            //    {
            //        writer.WriteLine(string.Format("{0}#{1}#{2}", item.Key, item.Value.Name, item.Value.ParentFrn));
            //    }
            //}
            //using (StreamWriter writer = new StreamWriter("LastUsn.db"))
            //{
            //    foreach (var item in LastUsn)
            //    {
            //        writer.WriteLine(string.Format("{0}#{1}", item.Key, item.Value));
            //    }
            //}

            if (!File.Exists("Folder.db"))
            {
                SQLiteConnection.CreateFile("Folder.db");
                using (SQLiteConnection connect = new SQLiteConnection("Data Source=Folder.db;Version=3;"))
                {
                    connect.Open();
                    SQLiteCommand cmd = connect.CreateCommand();
                    cmd.CommandText = @"CREATE TABLE [FolderPath] (
                                        [frn] bigint NOT NULL UNIQUE ON CONFLICT REPLACE,
                                        [name] text NOT NULL,
                                        [parent_frn] bigint NOT NULL
                                        );";
                    cmd.ExecuteNonQuery();
                }
            }
            if (!File.Exists("File.db"))
            {
                SQLiteConnection.CreateFile("File.db");
                using (SQLiteConnection connect = new SQLiteConnection("Data Source=File.db;Version=3;"))
                {
                    connect.Open();
                    SQLiteCommand cmd = connect.CreateCommand();
                    cmd.CommandText = @"CREATE TABLE [main].[FilePath] (
                                        [frn] bigint NOT NULL UNIQUE ON CONFLICT REPLACE,
                                        [name] text NOT NULL,
                                        [parent_frn] bigint NOT NULL);";
                    cmd.ExecuteNonQuery();
                }
            }
            using (SQLiteConnection connect = new SQLiteConnection("Data Source=Folder.db;Version=3;"))
            {
                connect.Open();
                using (SQLiteTransaction dbTrans = connect.BeginTransaction())
                {
                    using (SQLiteCommand cmd = connect.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO FolderPath('frn', 'name', 'parent_frn') VALUES(?, ? ,?)";
                        SQLiteParameter field_frn = cmd.CreateParameter();
                        SQLiteParameter field_name = cmd.CreateParameter();
                        SQLiteParameter field_parent_frn = cmd.CreateParameter();

                        cmd.Parameters.Add(field_frn);
                        cmd.Parameters.Add(field_name);
                        cmd.Parameters.Add(field_parent_frn);

                        foreach (var item in FolderDB)
                        {
                            field_frn.Value = item.Key;
                            field_name.Value = item.Value.Name;
                            field_parent_frn.Value = item.Value.ParentFrn;

                            cmd.ExecuteNonQuery();
                        }
                    }
                    dbTrans.Commit();
                }
            }
            using (SQLiteConnection connect = new SQLiteConnection("Data Source=File.db;Version=3;"))
            {
                connect.Open();
                using (SQLiteTransaction dbTrans = connect.BeginTransaction())
                {
                    using (SQLiteCommand cmd = connect.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO FilePath('frn', 'name', 'parent_frn') VALUES(?, ? ,?)";
                        SQLiteParameter field_frn = cmd.CreateParameter();
                        SQLiteParameter field_name = cmd.CreateParameter();
                        SQLiteParameter field_parent_frn = cmd.CreateParameter();

                        cmd.Parameters.Add(field_frn);
                        cmd.Parameters.Add(field_name);
                        cmd.Parameters.Add(field_parent_frn);

                        foreach (var item in FileDB)
                        {
                            field_frn.Value = item.Key;
                            field_name.Value = item.Value.Name;
                            field_parent_frn.Value = item.Value.ParentFrn;

                            cmd.ExecuteNonQuery();
                        }
                    }
                    dbTrans.Commit();
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
                    //var ext = System.IO.Path.GetExtension(item.FileName);
                    //if (ExtFilters.Contains(ext.ToLower()))
                    mdb.FileDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                }
                lastitem = item;
            }

            mdb.LastUsn["c:"] = lastitem.Usn;

            mdb.Save(".");
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
