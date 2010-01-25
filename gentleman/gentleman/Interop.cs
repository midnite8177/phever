using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using Interop = System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ThumbLib
{
    public class Win32
    {
        public const long STG_E_FILENOTFOUND = 0x80030006L;
        public const int S_FALSE = 1;
        public const int S_OK = 0;

        [Flags]
        public enum STGM : int
        {
            DIRECT = 0x00000000,
            TRANSACTED = 0x00010000,
            SIMPLE = 0x08000000,
            READ = 0x00000000,
            WRITE = 0x00000001,
            READWRITE = 0x00000002,
            SHARE_DENY_NONE = 0x00000040,
            SHARE_DENY_READ = 0x00000030,
            SHARE_DENY_WRITE = 0x00000020,
            SHARE_EXCLUSIVE = 0x00000010,
            PRIORITY = 0x00040000,
            DELETEONRELEASE = 0x04000000,
            CREATE = 0x00001000,
            CONVERT = 0x00020000,
            FAILIFTHERE = 0x00000000,
            NOSCRATCH = 0x00100000,
            NOSNAPSHOT = 0x00200000,
            DIRECT_SWMR = 0x00400000
        }

        public enum STREAM_SEAK : int
        {
            SET = 0,
            CUR = 1,
            END = 2
        }

        [Flags]
        public enum STGC : uint
        {
            DEFAULT = 0,
            OVERWRITE = 1,
            ONLYIFCURRENT = 2,
            DANGEROUSLYCOMMITMERELYTODISKCACHE = 4
        }

        [Interop.DllImport("ole32.dll")]
        public static extern int StgIsStorageFile([Interop.MarshalAs(Interop.UnmanagedType.LPWStr)] string pwcsName);

        [Interop.DllImport("ole32.dll")]
        public static extern int StgOpenStorage([Interop.MarshalAs(Interop.UnmanagedType.LPWStr)] string pwcsName,
            IStorage pstgPriority, STGM grfMode, IntPtr snbExclude, uint reserved, out IStorage ppstgOpen);
    }

    [Interop.ComImport]
    [Interop.Guid("0000000b-0000-0000-C000-000000000046")]
    [Interop.InterfaceType(Interop.ComInterfaceType.InterfaceIsIUnknown)]
    public interface IStorage
    {
        void CreateStream(
            /* [string][in] */ string pwcsName,
            /* [in] */ uint grfMode,
            /* [in] */ uint reserved1,
            /* [in] */ uint reserved2,
            /* [out] */ out IStream ppstm);

        void OpenStream(
            /* [string][in] */ string pwcsName,
            /* [unique][in] */ IntPtr reserved1,
            /* [in] */ uint grfMode,
            /* [in] */ uint reserved2,
            /* [out] */ out IStream ppstm);

        void CreateStorage(
            /* [string][in] */ string pwcsName,
            /* [in] */ uint grfMode,
            /* [in] */ uint reserved1,
            /* [in] */ uint reserved2,
            /* [out] */ out IStorage ppstg);

        void OpenStorage(
            /* [string][unique][in] */ string pwcsName,
            /* [unique][in] */ IStorage pstgPriority,
            /* [in] */ uint grfMode,
            /* [unique][in] */ IntPtr snbExclude,
            /* [in] */ uint reserved,
            /* [out] */ out IStorage ppstg);

        void CopyTo(
            /* [in] */ uint ciidExclude,
            /* [size_is][unique][in] */ Guid rgiidExclude, // should this be an array?
            /* [unique][in] */ IntPtr snbExclude,
            /* [unique][in] */ IStorage pstgDest);

        void MoveElementTo(
            /* [string][in] */ string pwcsName,
            /* [unique][in] */ IStorage pstgDest,
            /* [string][in] */ string pwcsNewName,
            /* [in] */ uint grfFlags);

        void Commit(
            /* [in] */ uint grfCommitFlags);

        void Revert();

        void EnumElements(
            /* [in] */ uint reserved1,
            /* [size_is][unique][in] */ IntPtr reserved2,
            /* [in] */ uint reserved3,
            /* [out] */ out IEnumSTATSTG ppenum);

        void DestroyElement(
            /* [string][in] */ string pwcsName);

        void RenameElement(
            /* [string][in] */ string pwcsOldName,
            /* [string][in] */ string pwcsNewName);

        void SetElementTimes(
            /* [string][unique][in] */ string pwcsName,
            /* [unique][in] */ FILETIME pctime,
            /* [unique][in] */ FILETIME patime,
            /* [unique][in] */ FILETIME pmtime);

        void SetClass(
            /* [in] */ Guid clsid);

        void SetStateBits(
            /* [in] */ uint grfStateBits,
            /* [in] */ uint grfMask);

        void Stat(
            /* [out] */ out STATSTG pstatstg,
            /* [in] */ uint grfStatFlag);
    }

    [Interop.ComImport]
    [Interop.Guid("0000000d-0000-0000-C000-000000000046")]
    [Interop.InterfaceType(Interop.ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumSTATSTG
    {
        // The user needs to allocate an STATSTG array whose size is celt.
        [Interop.PreserveSig]
        uint Next(uint celt, 
            [Interop.MarshalAs(Interop.UnmanagedType.LPArray), Interop.Out] STATSTG[] rgelt, 
            out uint pceltFetched);

        void Skip(uint celt);

        void Reset();

        [return: Interop.MarshalAs(Interop.UnmanagedType.Interface)]
        IEnumSTATSTG Clone();
    }

    [Interop.StructLayout(Interop.LayoutKind.Sequential)]
    public struct CatalogHeader
    {
        public short Reserved1;

        public short Reserved2;

        public int ThumbCount;

        public int ThumbWidth;

        public int ThumbHeight;
    }

    [Interop.StructLayout(Interop.LayoutKind.Sequential)]
    public struct CatalogItem
    {
        public int Reserved1;

        private int m_ItemId;
        public int ItemId
        {
            get { return m_ItemId; }
            set
            {
                m_ItemId = value;
                BuildItemIdString(m_ItemId);
            }
        }

        public DateTime Modified;

        public string FileName;

        public short Reserved2;

        // 自己添加的新域
        public string ItemIdString
        {
            get;
            private set;
        }

        private void BuildItemIdString(int itemId)
        {
            var temp = itemId.ToString();
            var buffer = new char[temp.Length];
            for (int i = 0; i < temp.Length; ++i)
                buffer[i] = temp[temp.Length - i - 1];

            ItemIdString = new string(buffer);
        }
    }
}
