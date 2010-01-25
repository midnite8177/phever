using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Collections;

namespace ThumbLib
{
    class ThumbDbCom : IDisposable
    {
        public string[] ThumbNames { get; private set; }

        public int ThumbWidth { get; private set; }

        public int ThumbHeight { get; private set; }

        private const string THUMB_DB_FILE = "Thumbs.db";
        private const string CATALOG_HEADER_NAME = "Catalog";
        private const int BUFFER_LENGTH = 1024;

        private static ThumbNameComparer m_ThumbNameComparer = new ThumbNameComparer();

        private IStorage m_IStorage;
        private CatalogItem[] m_CatalogItemList;

        /// <summary>
        /// 以只读模式打开Thumb.db文件
        /// </summary>
        /// <param name="storage">已经打开的IStorage指针</param>
        internal ThumbDbCom(IStorage storage)
        {
            if (storage == null)
                throw new ArgumentNullException("storage");

            m_IStorage = storage;
            m_CatalogItemList = InitialThumbNameList(m_IStorage);
            ThumbNames = new string[m_CatalogItemList.Length];

            for (int i = 0; i < ThumbNames.Length; ++i)
            {
                ThumbNames[i] = m_CatalogItemList[i].FileName;
            }

            Array.Sort(m_CatalogItemList, m_ThumbNameComparer);
        }

        internal void AddThumb(PictureThumb thumb)
        {
            if (thumb == null)
                throw new ArgumentNullException("thumb");

            if ( Array.BinarySearch(m_CatalogItemList, thumb.Name, m_ThumbNameComparer) >= 0 )
                throw new ArgumentException(string.Format("图片{0}的缩略图已经存在了!", thumb.Name));

            IStream thumbStream = null;
            m_IStorage.CreateStream(thumb.Name,
                (uint)(Win32.STGM.READWRITE | Win32.STGM.SHARE_EXCLUSIVE),
                0,
                0,
                out thumbStream);
            var buffer = new byte[BUFFER_LENGTH];
            var cbWritten = Marshal.AllocCoTaskMem(4);
            var cbRead = 0;
            var reader = new BinaryReader(thumb.ThumbStream);

            try
            {
                // 既然在读的时候要跳过前12个字节,写的时候也跳过吧
                thumbStream.Seek(12, (int)Win32.STREAM_SEAK.SET, cbWritten);

                do
                {
                    var read = thumb.ThumbStream.Read(buffer, cbRead, BUFFER_LENGTH);
                    cbRead += read;
                    thumbStream.Write(buffer, read, cbWritten);
                    Array.Clear(buffer, 0, read);
                } while (Marshal.ReadInt32(cbWritten) == BUFFER_LENGTH);
            }
            finally
            {
                Marshal.FreeCoTaskMem(cbWritten);
                Marshal.ReleaseComObject(thumbStream);
            }
        }

        internal void Save()
        {
            m_IStorage.Commit((uint)Win32.STGC.DEFAULT);
        }

        internal PictureThumb ReadThumb(string name)
        {
            var stream = ReadThumbStream(name);

            return new PictureThumb(name) { ThumbStream = stream };
        }

        internal Stream ReadThumbStream(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            var idx = Array.BinarySearch(m_CatalogItemList, name, m_ThumbNameComparer);
            if (idx < 0)
                return null;

            var itemId = m_CatalogItemList[idx].ItemIdString;
            IStream thumbStream = null;
            m_IStorage.OpenStream(itemId,
                IntPtr.Zero,
                (uint)(Win32.STGM.READWRITE | Win32.STGM.SHARE_EXCLUSIVE),
                0,
                out thumbStream);
            var buffer = new byte[BUFFER_LENGTH];
            var cbRead = Marshal.AllocCoTaskMem(4);
            var result = new MemoryStream();
            var writer = new BinaryWriter(result);

            try
            {
                // 放弃前12个字节-3个整形（int）
                // 
                // - 第一个int不知道是什么东西
                // - 第二个int是缩略图的索引号
                // - 第三个int是缩略图的文件大小
                thumbStream.Read(buffer, 12, cbRead);
                var count = 0;

                do
                {
                    thumbStream.Read(buffer, BUFFER_LENGTH, cbRead);
                    count = Marshal.ReadInt32(cbRead);
                    writer.Write(buffer, 0, count);
                    Array.Clear(buffer, 0, buffer.Length);
                } while (count == BUFFER_LENGTH);
            }
            finally
            {
                Marshal.FreeCoTaskMem(cbRead);
                Marshal.ReleaseComObject(thumbStream);
            }

            result.Seek(0, SeekOrigin.Begin);
            return result;
        }

        internal static bool IsThumbDbAvailable(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException("directory");
            Debug.Assert(Directory.Exists(directory), "IsThumbDbAvailable的调用方应该确认directory是否存在！");

            string path = string.Format(@"{0}\{1}", Path.GetFullPath(directory), THUMB_DB_FILE);
            var ret = Win32.StgIsStorageFile(path);
            return ret == Win32.S_OK;
        }

        internal static ThumbDbCom OpenThumbDb(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException("directory");
            Debug.Assert(Directory.Exists(directory), "OpenThumbDb的调用方应该确认directory是否存在！");
            Debug.Assert(IsThumbDbAvailable(directory), 
                string.Format("调用OpenThumbDb之前请先调用IsThumbDbAvailable以确认{0}里面有Thumb.db文件存在！", directory));

            string path = string.Format(@"{0}\{1}", Path.GetFullPath(directory), THUMB_DB_FILE);
            IStorage storage = null;
            var ret = Win32.StgOpenStorage(
                path, null, Win32.STGM.READWRITE | Win32.STGM.SHARE_EXCLUSIVE, IntPtr.Zero, 0, out storage);

            if (ret != Win32.S_OK)
                throw new COMException(string.Format("在尝试打开文件夹{0}里的图片缓存时，StgOpenStorage发生了错误！", directory), Marshal.GetExceptionForHR(ret));

            return new ThumbDbCom(storage);
        }

        private CatalogItem[] InitialThumbNameList(IStorage m_IStorage)
        {
            IStream catalogHeaderStream;
            m_IStorage.OpenStream(CATALOG_HEADER_NAME,
                IntPtr.Zero,
                (uint)(Win32.STGM.READWRITE | Win32.STGM.SHARE_EXCLUSIVE),
                0,
                out catalogHeaderStream);
            var header = new CatalogHeader();
            var size = Marshal.SizeOf(typeof(CatalogHeader));
            var buffer = new byte[size];
            var cbRead = Marshal.AllocCoTaskMem(4);

            try
            {
                catalogHeaderStream.Read(buffer, size, cbRead);
                if (size != Marshal.ReadInt32(cbRead))
                    throw new InvalidOperationException("在从Thumb.db读取CatalogHeader失败！");
            }
            finally
            {
                Marshal.FreeCoTaskMem(cbRead);
            }

            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                header.Reserved1 = reader.ReadInt16();
                header.Reserved2 = reader.ReadInt16();
                header.ThumbCount = reader.ReadInt32();
                header.ThumbHeight = reader.ReadInt32();
                header.ThumbWidth = reader.ReadInt32();
            }

            ThumbHeight = header.ThumbHeight;
            ThumbWidth = header.ThumbWidth;

            return ReadNameList(catalogHeaderStream, header);
        }

        private CatalogItem[] ReadNameList(IStream catalogHeaderStream, CatalogHeader header)
        {
            // 在Windows中，文件名最长好像只有256个Unicode字符
            // 所以1K的缓存应该很足够了
            var buffer = new byte[1024];
            var cbRead = Marshal.AllocCoTaskMem(4);
            var results = new CatalogItem[header.ThumbCount];
            var builder = new StringBuilder();

            try
            {
                for (int i = 0; i < header.ThumbCount; ++i)
                {
                    catalogHeaderStream.Read(buffer, 16, cbRead);
                    if (Marshal.ReadInt32(cbRead) != 16)
                        throw new InvalidDataException("文件夹的Thumb.db已经损坏！");

                    results[i].Reserved1 = BitConverter.ToInt32(buffer, 0);
                    results[i].ItemId = BitConverter.ToInt32(buffer, 4);
                    results[i].Modified = DateTime.FromFileTime(
                        BitConverter.ToInt64(buffer, 8));

                    Array.Clear(buffer, 0, 16);

                    do
                    {
                        catalogHeaderStream.Read(buffer, 2, cbRead);
                        builder.Append(BitConverter.ToChar(buffer, 0));

                    } while (buffer[0] != 0 || buffer[1] != 0);

                    builder.Length -= 1;
                    results[i].FileName = builder.ToString();
                    builder.Remove(0, builder.Length);

                    catalogHeaderStream.Read(buffer, 2, cbRead);
                    results[i].Reserved2 = BitConverter.ToInt16(buffer, 0);

                    Array.Clear(buffer, 0, 2);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(cbRead);
            }

            return results;
        }

        class ThumbNameComparer : IComparer
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                var catalogItem = (CatalogItem)x;
                var name = y as string;

                if (name != null)
                {
                    return string.CompareOrdinal(catalogItem.FileName, name);
                }
                else
                {
                    return string.CompareOrdinal(catalogItem.FileName, ((CatalogItem)y).FileName);
                }
            }

            #endregion
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (m_IStorage != null)
            {
                Marshal.ReleaseComObject(m_IStorage);
                m_IStorage = null;
            }
        }

        #endregion
    }
}
