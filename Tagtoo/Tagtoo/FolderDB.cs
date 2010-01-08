using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace Tagtoo
{    
    public class FolderDB
    {


        private static UInt64 LastUsn = 0;
        private static Dictionary<UInt64, FileEntry> mRefernumberMap = new Dictionary<ulong, FileEntry>();
        private static Regex DB_PATTERN = new Regex(@"(?<ReferenceNumber>[\d]+);(?<FileName>.+);(?<ParentReferenceNumber>[\d]+)", RegexOptions.IgnoreCase);
      
        public static void Add(PInvokeWin32.USN_RECORD record)
        {
            FileEntry entry = new FileEntry(record.FileReferenceNumber, record.FileName, record.ParentFileReferenceNumber);
            if (LastUsn < record.Usn)
            {
                LastUsn = record.Usn;
            }
            mRefernumberMap.Add(entry.ReferenceNumber, entry);
        }

        public static void Update(PInvokeWin32.USN_RECORD record)
        {
            FileEntry entry = mRefernumberMap[record.FileReferenceNumber];
            entry.FileName = record.FileName;                
            
            entry.ParentReferenceNumber = record.ParentFileReferenceNumber;
        }

        public static void Delete(PInvokeWin32.USN_RECORD record)
        {
            FileEntry entry = mRefernumberMap[record.FileReferenceNumber];

            mRefernumberMap.Remove(entry.ReferenceNumber);
        }

        public static void Clear()
        {
            mRefernumberMap.Clear();
        }

        public static String FullPath(UInt64 FileReferenceNumber)
        {            
            if (mRefernumberMap.ContainsKey(FileReferenceNumber))
            {
                FileEntry entry = mRefernumberMap[FileReferenceNumber];
                return FullPath(entry.ParentReferenceNumber) + "\\" + entry.FileName;                
            }

            return "f:";
        }

        public static void Save()
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter("database.db");

            foreach (var m in mRefernumberMap)
            {
                writer.WriteLine(string.Format("{0};{1};{2}", m.Value.ReferenceNumber, m.Value.FileName, m.Value.ParentReferenceNumber));
            }

            writer.Close();
        }

        public static void Load()
        {
            Clear();
            System.IO.StreamReader reader = new System.IO.StreamReader("database.db");

            while(true)
            {
                string buffer = reader.ReadLine();
                if (buffer != null && DB_PATTERN.IsMatch(buffer))
                {
                    Match m = DB_PATTERN.Match(buffer);
                    FileEntry entry = new FileEntry(Convert.ToUInt64(m.Groups["ReferenceNumber"].Value), Convert.ToString(m.Groups["FileName"].Value), Convert.ToUInt64(m.Groups["ParentReferenceNumber"].Value));

                    mRefernumberMap[entry.ReferenceNumber] = entry;
                }
                else
                    break;
            }

            reader.Close();
        }
    }

    public class FileEntry
    {
        public string FileName = null;
        public UInt64 ParentReferenceNumber = 0;
        public UInt64 ReferenceNumber = 0;

        public FileEntry(UInt64 rn, string name, UInt64 prn)
        {
            FileName = name;
            ParentReferenceNumber = prn;
            ReferenceNumber = rn;
        }


    }

}
