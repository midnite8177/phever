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
        private const string FolderDBPath = @"C:\var\phever\trunk\gentleman\gentleman\bin\Debug\folder.mdf";
        private const string FileDBPath = @"C:\var\phever\trunk\gentleman\gentleman\bin\Debug\file.mdf";
        private FolderDBDataContext FolderConnect;
        private FileDBDataContext FileConnect;
        private CChangeJournal mft;        
        
        public PathDB()
        {
            FolderConnect = new FolderDBDataContext(FolderDBPath);            
            FileConnect = new FileDBDataContext(FileDBPath);

            mft = new CChangeJournal("c:");
        }

        public void Build()
        {            
            if(!File.Exists(FolderDBPath))
                FolderConnect.CreateDatabase();
            if(!File.Exists(FileDBPath))
                FileConnect.CreateDatabase();

            var root = mft.GetRootFrn();
            FolderConnect.FolderDBs.InsertOnSubmit(new FolderDB { frn = root.Key, name = root.Value.Name, parent_frn = root.Value.ParentFrn });
            
            foreach (var item in mft.EnumVolume(0, long.MaxValue))
            {
                if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    FolderConnect.FolderDBs.InsertOnSubmit(new FolderDB { frn = item.FileReferenceNumber, name = item.FileName, parent_frn=item.ParentFileReferenceNumber });
                else
                    FileConnect.FileDBs.InsertOnSubmit(new FileDB { frn = item.FileReferenceNumber, name = item.FileName, parent_frn = item.ParentFileReferenceNumber });

            }            
            FolderConnect.SubmitChanges();
            FileConnect.SubmitChanges();
        }
             
    }

    //public class PathDBController
    //{
    //    private PathDB mdb;
    //    private CChangeJournal mft;        

    //    public static List<String> ExtFilters = new List<string>() { ".jpg", ".gif", ".jpeg", ".png", ".bmp" };

    //    public PathDBController()
    //    {
    //        mdb = new PathDB();
    //        mft = new CChangeJournal("c:");

    //        bool cached = mdb.Load(".");

    //        if (!cached) Build();
    //        else
    //        {
    //            Update();
    //        }
    //        mdb.Save(".");
    //    }

    //    public void Build()
    //    {                        
    //        var root = mft.GetRootFrn();
    //        mdb.FolderDB[root.Key] = root.Value;

    //        PInvokeWin32.USN_RECORD lastitem = null;
    //        foreach (var item in mft.EnumVolume(0, long.MaxValue))
    //        {
    //            if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
    //                mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
    //            else
    //            {
    //                //var ext = System.IO.Path.GetExtension(item.FileName);
    //                //if (ExtFilters.Contains(ext.ToLower()))
    //                mdb.FileDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
    //            }
    //            lastitem = item;
    //        }

    //        mdb.LastUsn["c:"] = lastitem.Usn;

    //        mdb.Save(".");
    //    }
    //    public void Update()
    //    {
    //        var q = mft.Query();
    //        foreach (var item in mft.ReadUSN(q.UsnJournalID, long.MaxValue, PInvokeWin32.USN_REASON_CLOSE))
    //        {
    //            if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
    //            {
    //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
    //                    mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
    //                if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
    //                    mdb.FolderDB[item.FileReferenceNumber].Name = item.FileName;
    //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
    //                    mdb.FolderDB.Remove(item.FileReferenceNumber);
    //            }
    //            else
    //            {
    //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
    //                    mdb.FileDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
    //                if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
    //                    mdb.FileDB[item.FileReferenceNumber].Name = item.FileName;
    //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
    //                    mdb.FileDB.Remove(item.FileReferenceNumber);
    //            }
    //        }
    //    }
    //}
}
