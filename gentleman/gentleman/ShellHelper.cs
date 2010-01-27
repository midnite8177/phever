using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tagtoo;
using System.IO;

namespace gentleman
{
    public class ShellHelper
    {
        static Dictionary<UInt64, FileNameAndFrn> FileDB = new Dictionary<ulong, FileNameAndFrn>();
        static Dictionary<UInt64, FileNameAndFrn> FolderDB = new Dictionary<ulong, FileNameAndFrn>();

        private static string GetFullPath(UInt64 fileid)
        {
            List<string> Path = new List<string>() {FileDB[fileid].Name};
            FileNameAndFrn fileinfo = FileDB[fileid];
            UInt64 parentid = fileinfo.ParentFrn;

            while(parentid != 0)
            {
                var parent = FolderDB[parentid];
                Path.Insert(0, parent.Name);
                parentid = parent.ParentFrn;
            }
            return string.Join(@"\", Path.ToArray());
        }

        public static void ImageFiles()
        {
            //CChangeJournal mft = new CChangeJournal();
            //mft.EnumerateVolume("c:", new string[] { ".jpg", ".gif", ".bmp", ".jpeg", ".png" });
            //FolderDB = mft._directories;

            //foreach (var file in FileDB)
            //{
            //    string path = GetFullPath(file.Key);
            //    path = path.Replace("\\\\.\\c:\\\\", @"c:\");
            //    if (path.Length < 260 && !System.IO.File.Exists(path)) throw new Exception();
            //}
        }
    }

}
