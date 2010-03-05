using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tagtoo;
using System.IO;
using System.Data.SQLite;
using System.Data.Common;
using System.Data.Linq;

//http://www.sqlite.org/cvstrac/wiki?p=SqliteWrappers SQLite with C#
//http://sqlite.phxsoftware.com/forums/t/76.aspx  Sample Code
/// http://msdn.microsoft.com/en-us/vbasic/bb688085.aspx linq to sql

namespace gentleman.USN
{
    public class PathDB
    {
        private string DBPATH = @"\PathsT3{0}.mdf";

        private PathDBDataContext Connect;
        private CChangeJournal mft;
        private Int64 LastUsn;

        public PathDB(string drive)
        {
            mft = new CChangeJournal(drive);
            var root = mft.GetRootFrn();
            string CurrentFolder = System.IO.Path.GetFullPath(".");

            var FilePath = CurrentFolder + string.Format(DBPATH, root.Key);

            /// Open the Database
            /// 
            Connect = new PathDBDataContext(FilePath);

            /// Create the Database if not there are not exist                        
            if (!Connect.DatabaseExists())
            {
                Connect.CreateDatabase();
                Build();
            }
            else
            {
                Update();
            }
        }

        public void Build()
        {
            //System.Environment.GetLogicalDrives();            

            var root = mft.GetRootFrn();
            Connect.FolderEntries.InsertOnSubmit(new FolderEntry { frn = root.Key, name = root.Value.Name, parent_frn = root.Value.ParentFrn });

            LastUsn = 0;

            foreach (var item in mft.EnumVolume(0, long.MaxValue))
            {
                if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                {
                    Connect.FolderEntries.InsertOnSubmit(new FolderEntry { frn = item.FileReferenceNumber, name = item.FileName, parent_frn = item.ParentFileReferenceNumber });
                    LastUsn = item.Usn;
                }
                else
                {
                    Connect.FileEntries.InsertOnSubmit(new FileEntry { frn = item.FileReferenceNumber, name = item.FileName, parent_frn = item.ParentFileReferenceNumber });
                    LastUsn = item.Usn;
                }
            }
            Connect.UpdateLogs.InsertOnSubmit(new UpdateLog { build_date = DateTime.Now, USN = LastUsn });
            Connect.SubmitChanges();
        }
        public void Update()
        {
            ///http://www.hookedonlinq.com/LINQtoSQL5MinuteOverview.ashx
            ///http://www.linqpad.net/ Linq Pad
            ///
            var q = mft.Query();
            var Log = Connect.UpdateLogs.OrderByDescending(c => c.build_date).First();
            LastUsn = Log.USN;

            Dictionary<UInt64, FolderEntry> FolderCreateCache = new Dictionary<ulong, FolderEntry>();
            Dictionary<UInt64, FolderEntry> FolderUpdateCache = new Dictionary<ulong, FolderEntry>();            
            Dictionary<UInt64, FolderEntry> FolderDeleteCache = new Dictionary<ulong, FolderEntry>();

            Dictionary<UInt64, FileEntry> FileCreateCache = new Dictionary<ulong, FileEntry>();
            Dictionary<UInt64, FileEntry> FileUpdateCache = new Dictionary<ulong, FileEntry>();            
            Dictionary<UInt64, FileEntry> FileDeleteCache = new Dictionary<ulong, FileEntry>();

            foreach (var item in mft.ReadUSN(q.UsnJournalID, LastUsn, PInvokeWin32.USN_REASON_CLOSE))
            {
                if ((item.Reason & PInvokeWin32.USN_REASON_CLOSE) != 0)
                {
                    if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    {
                        /// Insert New Record
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
                        {
                            FolderCreateCache[item.FileReferenceNumber] = new FolderEntry
                            {
                                frn = item.FileReferenceNumber,
                                name = item.FileName,
                                parent_frn = item.ParentFileReferenceNumber
                            };
                        }
                        //mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
                        if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
                        {                                                        
                            if (FolderCreateCache.ContainsKey(item.FileReferenceNumber)) {
                                /// Update an uninsert entry
                                FolderCreateCache[item.FileReferenceNumber].name = item.FileName;
                                FolderCreateCache[item.FileReferenceNumber].parent_frn = item.ParentFileReferenceNumber;
                            }
                            else
                                /// Update an inserted entry
                                FolderUpdateCache[item.FileReferenceNumber] = new FolderEntry
                                {
                                    frn = item.FileReferenceNumber,
                                    name = item.FileName,
                                    parent_frn = item.ParentFileReferenceNumber
                                };
                        }
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
                        {
                            if (FolderCreateCache.ContainsKey(item.FileReferenceNumber))
                            {
                                /// Delete an uninsert entry
                                /// So we don't need really delete it
                                FolderCreateCache.Remove(item.FileReferenceNumber);
                            }
                            else
                            {
                                /// Delete the update command
                                if (FolderUpdateCache.ContainsKey(item.FileReferenceNumber))
                                    FolderUpdateCache.Remove(item.FileReferenceNumber);

                                /// Delete an inserted entry                                
                                FolderDeleteCache[item.FileReferenceNumber] = new FolderEntry
                                {
                                    frn = item.FileReferenceNumber,
                                    name = item.FileName,
                                    parent_frn = item.ParentFileReferenceNumber
                                };
                            }                            
                        }
                        
                    }
                    else
                    {
                        /// Insert New Record
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
                        {
                            FileCreateCache[item.FileReferenceNumber] = new FileEntry
                            {
                                frn = item.FileReferenceNumber,
                                name = item.FileName,
                                parent_frn = item.ParentFileReferenceNumber
                            };
                        }                        
                        if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
                        {
                            if (FileCreateCache.ContainsKey(item.FileReferenceNumber))
                                /// Update an uninsert entry
                                FileCreateCache[item.FileReferenceNumber].name = item.FileName;
                            else
                                /// Update an inserted entry
                                FileUpdateCache[item.FileReferenceNumber] = new FileEntry
                                {
                                    frn = item.FileReferenceNumber,
                                    name = item.FileName,
                                    parent_frn = item.ParentFileReferenceNumber
                                };
                        }
                        if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
                        {
                            if (FileCreateCache.ContainsKey(item.FileReferenceNumber))
                                /// Delete an uninsert entry
                                /// So we don't need really delete it
                                FileCreateCache.Remove(item.FileReferenceNumber);
                            else
                            {
                                /// Delete the update command
                                if (FileUpdateCache.ContainsKey(item.FileReferenceNumber))
                                    FileUpdateCache.Remove(item.FileReferenceNumber);

                                /// Delete an inserted entry                                
                                FileDeleteCache[item.FileReferenceNumber] = new FileEntry
                                {
                                    frn = item.FileReferenceNumber,
                                    name = item.FileName,
                                    parent_frn = item.ParentFileReferenceNumber
                                };
                            }
                        }                    
                    }
                    LastUsn = item.Usn;
                }
            }            
            /// Insert, Update, and Delete Folder DB
            Connect.FolderEntries.InsertAllOnSubmit(FolderCreateCache.Values);
            foreach (var item in FolderUpdateCache)
            {
                Connect.FolderEntries.Single(a => a.frn == item.Key).name = item.Value.name;
            }
            foreach (var item in FolderDeleteCache)
            {
                //var v1 = Connect.FolderEntries.Single(a => a.name == item.Value.name);

                var v = Connect.FolderEntries.Single(a=>a.frn == item.Key);
                Connect.FolderEntries.DeleteOnSubmit(v);
            }

            /// Insert, Update, and Delete File DB
            Connect.FileEntries.InsertAllOnSubmit(FileCreateCache.Values);
            foreach (var item in FileUpdateCache)
            {
                Connect.FileEntries.Single(a => a.frn == item.Key).name = item.Value.name;             
            }
            foreach (var item in FileDeleteCache)
            {
                var v = Connect.FileEntries.Single(a => a.frn == item.Key);
                Connect.FileEntries.DeleteOnSubmit(v);
            }
 
            /// Update Log
            Connect.UpdateLogs.InsertOnSubmit(new UpdateLog { build_date = DateTime.Now, USN = LastUsn });
            Connect.SubmitChanges();           
        }
        //public void UpdateFake()
        //{
        //    Dictionary<UInt64, FolderEntry> FolderDB = new Dictionary<ulong, FolderEntry>();
        //    Dictionary<UInt64, FileEntry> FileDB = new Dictionary<ulong, FileEntry>();

        //    foreach (var file in Connect.FileEntries)
        //    {
        //        FileDB.Add(file.frn, file);
        //    }

        //    foreach (var folder in Connect.FolderEntries)
        //    {
        //        FolderDB.Add(folder.frn, folder);
        //    }

        //    var Log = Connect.UpdateLogs.OrderByDescending(c => c.build_date).First();
        //    LastUsn = Log.USN;
        //    var q = mft.Query();

        //    foreach (var item in mft.ReadUSN(q.UsnJournalID, LastUsn, PInvokeWin32.USN_REASON_CLOSE))
        //    {

        //        if ((item.Reason & PInvokeWin32.USN_REASON_CLOSE) != 0)
        //        {
        //            if ((item.FileAttributes & PInvokeWin32.FILE_ATTRIBUTE_DIRECTORY) != 0)
        //            {
        //                /// Insert New Record
        //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
        //                {
        //                    if (FolderDB.ContainsKey(item.FileReferenceNumber)) throw new Exception();
        //                    FolderDB[item.FileReferenceNumber] = new FolderEntry { frn = item.FileReferenceNumber, name = item.FileName, parent_frn = item.ParentFileReferenceNumber };
        //                }
        //                //mdb.FolderDB[item.FileReferenceNumber] = new FileNameAndFrn(item.FileName, item.ParentFileReferenceNumber);
        //                if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
        //                {
        //                    if (!FolderDB.ContainsKey(item.FileReferenceNumber)) throw new Exception();
        //                    FolderDB[item.FileReferenceNumber].name = item.FileName;
        //                }
        //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
        //                {
        //                    if (!FolderDB.ContainsKey(item.FileReferenceNumber)) throw new Exception();
        //                    FolderDB.Remove(item.FileReferenceNumber);
        //                }
        //                LastUsn = item.Usn;
        //            }
        //            else
        //            {
        //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_CREATE) != 0)
        //                {
        //                    if (FileDB.ContainsKey(item.FileReferenceNumber)) throw new Exception();
        //                    FileDB[item.FileReferenceNumber] = new FileEntry { frn = item.FileReferenceNumber, name = item.FileName, parent_frn = item.ParentFileReferenceNumber };
        //                }
        //                if ((item.Reason & PInvokeWin32.USN_REASON_RENAME_NEW_NAME) != 0)
        //                {
        //                    if (!FileDB.ContainsKey(item.FileReferenceNumber)) throw new Exception();
        //                    FileDB[item.FileReferenceNumber].name = item.FileName;
        //                }
        //                if ((item.Reason & PInvokeWin32.USN_REASON_FILE_DELETE) != 0)
        //                {
        //                    if (!FileDB.ContainsKey(item.FileReferenceNumber)) throw new Exception();
        //                    FileDB.Remove(item.FileReferenceNumber);
        //                }
        //                LastUsn = item.Usn;
        //            }
        //        }
        //    }
        //}
        
        public string GetFolderPath(ulong Frn)
        {
            List<String> PathCache = new List<string>();
            do
            {
                FolderEntry Node = Connect.FolderEntries.Single(p => p.frn == Frn);
                PathCache.Insert(0, Node.name);
                Frn = Node.parent_frn;
            } while (Frn != 0);

            return string.Join("//", PathCache.ToArray());
        }
        public string GetFilePath(ulong Frn)
        {
            FileEntry file = Connect.FileEntries.Single(p => p.frn == Frn);
            return GetFolderPath(file.parent_frn) + "//" + file.name;
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
