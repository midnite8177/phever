using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
//using Michaelis.Drawing;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
/// 1. File link (solved)
/// 2. Upload Tag
/// 3. Server Side Code
/// 4. Read/Write Tag Attritube of Image
/// http://www.vsj.co.uk/dotnet/display.asp?id=694
/// http://www.dreamincode.net/code/snippet3144.htm
/// http://msdn.microsoft.com/en-us/library/bb643802.aspx#merged_properties__zerh
/// http://msdn.microsoft.com/en-us/library/system.windows.media.imaging.bitmapmetadata.setquery.aspx
/// System.Diagnostics.Process.Start(@"file://C:\var\000a_370ra.jpg");
namespace gentleman
{
    public partial class Form1 : Form
    {
        private int ScanedFile = 0;
        private int JpgFile = 0;
        private int ErrFile = 0;
        private const string HashPathDB = "HashPath.db";
        private const string FolderDB = "FolderOption.db";
        private const string CacheFile = ".Gentleman.db";
        /// Path, Hash, tags 
        /// Doesn't need load in memory
        private Dictionary<string, KeyValuePair<string,string>> PathHash = null; 
        /// Hash, [Path]
        private Dictionary<string, List<string>> HashPath = null;
        /// Tag, [Hash]
        private Dictionary<string, List<string>> TagHash = new Dictionary<string, List<string>>();
        /// Folder, option
        private Dictionary<string, bool> FolderOption = null;
        /// Scaned Path
        private Dictionary<string, bool> ParsedPath = new Dictionary<string, bool>();
                
        public Form1()
        {
            InitializeComponent();
            test2();
            //JpegHelper x = new JpegHelper(@"C:\var\000a_370ra.jpg");
            //x.Save(@"c:\var\test1.jpg");
            //TestShow();
            
            //StreamWriter writer = new StreamWriter(@"c:\var\test.log");

            //writer.WriteLine(string.Format("{0}, {1}, {2}, {3}", DateTime.Now.ToString(), ScanedFile, JpgFile, ErrFile));

            //Scan(@"F:\");

            //writer.WriteLine(string.Format("{0}, {1}, {2}, {3}", DateTime.Now.ToString(), ScanedFile, JpgFile, ErrFile));

            //foreach (var i in HashMap)
            //{
            //    if (i.Value.Count > 1)
            //        writer.WriteLine(string.Format("{0} {1}", i.Key, string.Join(",", i.Value.ToArray())));
            //}

            //writer.Close();
        }

        public Dictionary<string,bool> LoadFolderOption()
        {
            if (FolderOption != null) return FolderOption;

            FolderOption = new Dictionary<string, bool>();

            if (System.IO.File.Exists(FolderDB))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(FolderDB))
                {
                    string data = null;
                    string path = null;
                    bool option = false;
                    while ((data = reader.ReadLine()) != null)
                    {
                        var token = data.Split(';');
                        if (token.Length == 2)
                        {
                            path = token[0];
                            option = Convert.ToBoolean(token[1]);
                            FolderOption[path] = option;
                        }
                    }
                }
            }
            return FolderOption;
        }

        public void UpdateFolderOption(Dictionary<string, bool> nFolderOption)
        {
            FolderOption = nFolderOption;
            Scan();
        }

        public void SaveFolderOption()
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(FolderDB))
            {
                foreach (var item in FolderOption)
                {
                    writer.WriteLine(string.Format("{0};{1}", item.Key, item.Value));
                }
            }
        }

        public Dictionary<string, List<string>> LoadHashPath()
        {
            if (HashPath != null) return HashPath;

            HashPath = new Dictionary<string, List<string>>();

            if (System.IO.File.Exists(HashPathDB))
            {

                using (System.IO.StreamReader reader = new System.IO.StreamReader(HashPathDB))
                {
                    string line = null;
                    string hash = null;
                    string[] paths = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var token = line.Split(';');
                        if (token.Length == 2)
                        {
                            hash = token[0];
                            paths = token[1].Split(',');

                            HashPath[hash] = new List<string>(paths);
                        }

                    }

                }
            }
            return HashPath;
        }

        public void SaveHashPath()
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(HashPathDB))
            {
                foreach (var item in HashPath)
                {
                    writer.WriteLine(string.Format("{0};{1}", item.Key, string.Join(",", item.Value.ToArray())));
                }
            }
        }

        public void Scan()
        {
            List<string> Paths = new List<string>(FolderOption.Keys);
            Paths.RemoveAll(a => FolderOption[a] == false);
            Paths.Sort();
            
            backgroundWorker1.RunWorkerAsync(Paths);
        }        

        // Return Path => Hash
        public static Dictionary<string, string> LoadCacheFile(string filepath)
        {
           Dictionary<string, string> FolderTemp = new Dictionary<string, string>();

            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader reader = new StreamReader(filepath))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var token = line.Split(';');
                        if (token.Length == 2)
                        {
                            var Hash = token[0];
                            var Path = token[1];
                            FolderTemp[Path] = Hash;
                        }
                    }
                }
            }
            return FolderTemp;
        }

        public static void SaveCacheFile(string filepath, Dictionary<string, string> FolderResult)
        {
            if (FolderResult.Count > 0)
            {
                using (System.IO.StreamWriter writer = new StreamWriter(filepath))
                {
                    foreach (var i in FolderResult)
                    {
                        writer.WriteLine(string.Format("{0};{1}", i.Value, i.Key));
                    }
                }
            }
        }
        private List<string> SupportType = new List<string>() { ".jpg", ".jpeg", ".png", ".gif" };
        public void ScanFolder(string folderPath)
        {
            if (ParsedPath.ContainsKey(folderPath)) return;

            Dictionary<string, string> FolderTemp = LoadCacheFile(folderPath + @"\" + CacheFile);            
            string[] files = System.IO.Directory.GetFiles(folderPath);
            Dictionary<string, string> FolderResult = new Dictionary<string,string>();

            foreach (string file in files)
            {      
                string ext =  System.IO.Path.GetExtension(file).ToLower();
                if (SupportType.Contains(ext))
                {

                    string hash = null;

                    if (FolderTemp.ContainsKey(file))
                    {
                        hash = FolderTemp[file];
                    }
                    else
                    {
                        try
                        {
                            hash = ComputeHash(file);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    if (!HashPath.ContainsKey(hash))
                        HashPath[hash] = new List<string>();

                    HashPath[hash].Add(file);
                    FolderResult[file] = hash;
                }
            }
            SaveCacheFile(folderPath + @"\" + CacheFile, FolderResult);
            ParsedPath[folderPath] = true;

            foreach (var folder in System.IO.Directory.GetDirectories(folderPath))
            {
                ScanFolder(folder);
            }
        }
        public static bool ThumbnailCallback()
        {
            return false;
        }
        private static Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
        private static string ComputeHash(string path)
        {
            using (Image bmp = new Bitmap(path))
            {

                int nwidth = bmp.Width > bmp.Height ? 120 : bmp.Width * 120 / bmp.Height;
                int nheight = bmp.Width > bmp.Height ? bmp.Height * 120 / bmp.Width : 120;
                nwidth = nwidth == 0 ? 1 : nwidth;
                nheight = nheight == 0 ? 1 : nheight;

                Image myThumbnail = bmp.GetThumbnailImage(nwidth, nheight, myCallback, IntPtr.Zero);

                using (MemoryStream streamout = new MemoryStream())
                {
                    myThumbnail.Save(streamout, ImageFormat.Bmp);
                    streamout.Position = 0;
                    SHA1 x = new SHA1CryptoServiceProvider();
                    return BitConverter.ToString(x.ComputeHash(streamout)).Replace("-", "");
                }
            }

        }
        public void TestShow()
        {            

            StreamReader reader = new StreamReader("test.log");
            ;
            reader.ReadLine();
            reader.ReadLine();
            string line = null;
            int number = 0;
            
            while((line = reader.ReadLine()) != null)
            {
                var hash = line.Substring(0, 40);
                imageList1.Images.Add(hash, new Bitmap(@"C:\var\top-rogo.jpg"));
                imageList1.ImageSize = new Size(120, 120);
                
                var paths = line.Substring(40).Split(',');
                number += 1;
                var group = new ListViewGroup(hash);
                listView1.Groups.Add(group);                
                foreach (var path in paths)
                {                                        
                    var item = new ListViewItem(System.IO.Path.GetFileName(path), group);
                    item.ImageKey = hash;
                    item.Tag = path;
                    listView1.Items.Add(item);                    
                }
                if (number >= 100) break;
            }

            

        }
       


        public void TestThumbData()
        {
            var filename = @"F:\pics\Thumbs.db";
            ThumbDBLib.ThumbDB x = new ThumbDBLib.ThumbDB(filename);
            var thumbData = x.GetThumbData(@"F:\pics\DSC03716.JPG");
            MemoryStream ms = new MemoryStream(thumbData);
            Image img = Image.FromStream(ms);

        }
        public void Scan(string dic)
        {
            //try
            //{
            //    foreach (var dicpath in Directory.GetDirectories(dic))
            //        Scan(dicpath);
            //}
            //catch(UnauthorizedAccessException)
            //{

            //}

            //foreach (var filepath in Directory.GetFiles(dic))
            //{
            //    ScanedFile += 1;
            //    try
            //    {
            //        string ext = System.IO.Path.GetExtension(filepath).ToLower();
            //        if (ext == ".jpg" || ext == ".jpeg")
            //        {                        
            //            JpegHelper helper = new JpegHelper(filepath);
            //            JpgFile += 1;
            //            string hashcode = helper.Hash;
            //            if (!HashPath.ContainsKey(hashcode))
            //                HashPath[hashcode] = new List<string>();
    
            //            HashPath[hashcode].Add(filepath);
                        
            //        }
            //    }
            //    catch (FormatException)
            //    {

            //    }
            //    catch (Exception)
            //    {
            //        ErrFile += 1;
            //    }
            //}

        }        

        public void test2()
        {
            JpegHelper x = new JpegHelper(@"C:\var\000a_370ra.jpg");
            x.Keywords = new List<string> { "test3", "test4" };
            x.Save(@"c:\var\000a_370ra_temp.jpg");
            JpegHelper xx = new JpegHelper(@"c:\var\000a_370ra_temp.jpg");            
        
        }
        public void test1()
        {
            // Get the source image stream
            using (FileStream imageFileStream =
                new FileStream(@"C:\var\JpegTemp.png", FileMode.Open))
            {
                // Load the image in the decoder
                PngBitmapDecoder decoder = new PngBitmapDecoder(imageFileStream,
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

                // Make a copy of the frame, this will also unlock the metadata
                BitmapFrame frameCopy = BitmapFrame.Create(decoder.Frames[0]);
                

                // Now we have a metadata object that is unfrozen
                BitmapMetadata copyMetadata = (BitmapMetadata)frameCopy.Metadata;

                // Set your metadata here, metadata in the source frame
                // will be rewritten to the output frame and any changes
                // will overwrite the metadata in the source frame.
                copyMetadata.Title = "Test Title";
                copyMetadata.Keywords = new System.Collections.ObjectModel.ReadOnlyCollection<string>(new List<string>(){"test3"});

                // Create a new encoder and add the copied frame to it
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frameCopy);

                // Save the new file with the new metadata
                using (FileStream imageFileOutStream =
                    new FileStream(@"c:\var\testOutput.png", FileMode.Create))
                {
                    encoder.Save(imageFileOutStream);
                }
            }

            //// If you want to add a brand new metadata block, you must create
            //// the metadata block first and then pass the metadata to the
            //// BitmapFrame.Create method so it's written to the frame

            //// Get the source image stream
            //using (FileStream imageFileStream =
            //    new FileStream("test.jpg", FileMode.Open))
            //{
            //    // Create new metadata first, here we are making an IPTC block in a JPG
            //    // NOTE: IPTC tags do not get parsed correctly on Windows 7
            //    BitmapMetadata jpgData = new BitmapMetadata("jpg");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/object name", "Test Title");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/keywords", "Test Tag");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/date created", "20090512");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/time created", "115300-0800");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/caption", "Test Comment");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/by-line", "Test Author");
            //    jpgData.SetQuery("/app13/irb/8bimiptc/iptc/copyright notice", "Copyright 2009");

            //    // Load the image in the decoder
            //    JpegBitmapDecoder decoder = new JpegBitmapDecoder(imageFileStream,
            //        BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            //    // Make a copy of the frame and also pass in the new metadata
            //    BitmapFrame frameCopy = BitmapFrame.Create(decoder.Frames[0],
            //        null /* thumbnail */,
            //        jpgData /* new metadata */,
            //        decoder.ColorContexts);

            //    // Now we have the image frame that has a fresh IPTC metadata block

            //    // Create a new encoder and add the frame to it
            //    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            //    encoder.Frames.Add(frameCopy);

            //    // Save the new file with the new metadata
            //    using (FileStream imageFileOutStream =
            //        new FileStream("testOutput2.jpg", FileMode.Create))
            //    {
            //        encoder.Save(imageFileOutStream);
            //    }
            //}

            //// Example of TIFF with EXIF
            //BitmapMetadata tiffData = new BitmapMetadata("tiff");
            //tiffData.SetQuery("/ifd/{ushort=40091}",
            //    UnicodeEncoding.Unicode.GetBytes("Test Title".ToCharArray()));
            //tiffData.SetQuery("/ifd/{ushort=40094}",
            //    UnicodeEncoding.Unicode.GetBytes("Test Tag".ToCharArray()));
            //tiffData.SetQuery("/ifd/exif/{uint=36867}", "2009:05:12 11:53:00");
            //tiffData.SetQuery("/ifd/{ushort=40092}",
            //    UnicodeEncoding.Unicode.GetBytes("Test Comment".ToCharArray()));
            //tiffData.SetQuery("/ifd/{ushort=40093}",
            //    UnicodeEncoding.Unicode.GetBytes("Test Author".ToCharArray()));
            //tiffData.SetQuery("/ifd/{ushort=33432}", "Copyright 2009");


            //// Example of WDP (Windows Media Photo) with XMP
            //// NOTE: This does not work (throws COM Exception) on x64
            //BitmapMetadata wdpMetadata = new BitmapMetadata("wmphoto");

            //// With XMP, you need to create the XMP nodes before you use them
            //// The BitmapMetadata constructor will accept metadata block types as
            //// well as image types as shown below
            //wdpMetadata.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
            //wdpMetadata.SetQuery("/ifd/xmp/dc:title", new BitmapMetadata("xmpalt"));
            //wdpMetadata.SetQuery("/ifd/xmp/exif:UserComment", new BitmapMetadata("xmpalt"));
            //wdpMetadata.SetQuery("/ifd/xmp/dc:rights", new BitmapMetadata("xmpalt"));
            //wdpMetadata.SetQuery("/ifd/xmp/dc:creator", new BitmapMetadata("xmpseq"));
            //wdpMetadata.SetQuery("/ifd/xmp/dc:subject", new BitmapMetadata("xmpbag"));

            //// XMP Alt has a default value x-default where you can set the value
            //wdpMetadata.SetQuery("/ifd/xmp/dc:title/x-default", "Test Title");
            //wdpMetadata.SetQuery("/ifd/xmp/exif:UserComment/x-default", "Test Comment");
            //wdpMetadata.SetQuery("/ifd/xmp/dc:rights/x-default", "Copyright 2009");

            //// XMP Seq/XMP Bag are indexed.  You can set multiple values using {ulong=<offset>}
            //wdpMetadata.SetQuery("/ifd/xmp/dc:creator/{ulong=0}", "Test Author 1");
            //wdpMetadata.SetQuery("/ifd/xmp/dc:creator/{ulong=1}", "Test Author 2");
            //wdpMetadata.SetQuery("/ifd/xmp/dc:subject/{ulong=0}", "Test Tag");

            //// This value is at the root of the XMP block using the XMP date format
            //wdpMetadata.SetQuery("/ifd/xmp/xmp:CreateDate", "2009-05-12T12:05:05");
        }

        public void test()
        {
            //   
            // get the path to some jpg file   
            //   
            //string jpegPath = "C:\\users\\scott\\Pictures\\sample\\xxx.jpg";
            string jpegPath = @"C:\var\000a_370ra.jpg";
            string jpegDirectory = Path.GetDirectoryName(jpegPath);
            string jpegFileName = Path.GetFileNameWithoutExtension(jpegPath);
            string jpegExtension = ".jpg";

            BitmapDecoder decoder = null;
            BitmapFrame bitmapFrame = null;
            BitmapMetadata metadata = null;
            if (File.Exists(jpegPath))
            {
                //   
                // load the jpg file with a JpegBitmapDecoder   
                //   
                using (Stream jpegStreamIn = File.Open(jpegPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    decoder = new JpegBitmapDecoder(jpegStreamIn, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);                    
                }

                bitmapFrame = decoder.Frames[0];
                metadata = (BitmapMetadata)bitmapFrame.Metadata;                

                if (bitmapFrame != null)
                {
                    //   
                    // now get an InPlaceBitmapMetadataWriter, modify the metadata and try to save   
                    //   
                    InPlaceBitmapMetadataWriter writer = bitmapFrame.CreateInPlaceBitmapMetadataWriter();
                    //List<string> tlist = new List<string>{"test1", "test2"};
                    writer.SetQuery("/app13/irb/8bimiptc/iptc/keywords", "test1,test2");
                    writer.SetQuery("/app1/ifd/exif:{uint=306}", "2001:01:01 01:01:01");
                    if (!writer.TrySave() == true)
                    {
                        //   
                        // the size of the metadata has been increased and we can't save it   
                        //    
                        uint padding = 2048;

                        BitmapMetadata metaData = (BitmapMetadata)bitmapFrame.Metadata.Clone();                        
                        metaData.SetQuery("/app13/irb/8bimiptc/iptc/keywords", "test1,test2");

                        if (metaData != null)
                        {
                            //   
                            // Add padding   
                            //   
                            metaData.SetQuery("/app1/ifd/PaddingSchema:Padding", padding);
                            //   
                            // modify the metadata   
                            metaData.SetQuery("/app1/ifd/exif:{uint=36867}", "2003:03:03 03:04:03");
                            metaData.SetQuery("/app1/ifd/exif:{uint=306}", "2001:01:01 01:01:01");
                            metaData.SetQuery("/app1/ifd/exif:{uint=36868}", "2002:02:02 02:02:02");
                            //   
                            // get an encoder to create a new jpg file with the addit'l metadata.   
                            //   
                            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmapFrame, bitmapFrame.Thumbnail, metaData, bitmapFrame.ColorContexts));                            
                            string jpegNewFileName = Path.Combine(jpegDirectory, "JpegTemp.jpg");
                            using (Stream jpegStreamOut = File.Open(jpegNewFileName, FileMode.CreateNew, FileAccess.ReadWrite))
                            {
                                encoder.Save(jpegStreamOut);
                            }
                            //   
                            // see if the metadata was really changed   
                            //   
                            using (Stream jpegStreamIn = File.Open(jpegNewFileName, FileMode.Open, FileAccess.ReadWrite))
                            {
                                decoder = new JpegBitmapDecoder(jpegStreamIn, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                BitmapFrame frame = decoder.Frames[0];
                                BitmapMetadata bmd = (BitmapMetadata)frame.Metadata;
                                string a1 = (string)bmd.GetQuery("/app1/ifd/exif:{uint=36867}");
                                string a2 = (string)bmd.GetQuery("/app1/ifd/exif:{uint=306}");
                                string a3 = (string)bmd.GetQuery("/app1/ifd/exif:{uint=36868}");
                            }
                        }
                    }
                }
            }

        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {            
            System.Diagnostics.Process.Start(string.Format(@"file://{0}",listView1.SelectedItems[0].Tag));
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
         
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            listView1.Width = this.Width - 20;
            listView1.Height = this.Height - 60;
        }
        private Form2 f;        

        private void opToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
                f = new Form2(this);
                f.Show();
            
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveFolderOption();
            SaveHashPath();
        }

        
        
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Dictionary<string, bool> ParsedPath = new Dictionary<string, bool>();

            var Paths = (List<String>)e.Argument;
            List<string> Queue = new List<string>(Paths);

            while (Queue.Count > 0)
            {
                var path = Queue[0];
                
                backgroundWorker1.ReportProgress(0, path);
                if (!ParsedPath.ContainsKey(path))
                {
                    Dictionary<string, string> Picasacache = LoadPicasaCache(path);                    

                    Dictionary<string, string> FolderCache = LoadCacheFile(path + @"\" + CacheFile);                    
                    string[] files = System.IO.Directory.GetFiles(path);
                    Queue.RemoveAt(0);                
                    Queue.InsertRange(0,System.IO.Directory.GetDirectories(path));

                    Dictionary<string, string> FolderCacheNew = new Dictionary<string, string>();

                    foreach (string file in files)
                    {
                        string hash = null;
                        string ext = System.IO.Path.GetExtension(file).ToLower();
                        if (SupportType.Contains(ext))
                        {
                            if (FolderCache.ContainsKey(file)) hash = FolderCache[file];
                            else
                            {
                                try
                                {
                                    hash = ComputeHash(file);
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            if (!HashPath.ContainsKey(hash))
                                HashPath[hash] = new List<string>();

                            HashPath[hash].Add(file);
                            FolderCacheNew[file] = hash;

                            List<string> tags = new List<string>();
                            string filepath = System.IO.Path.GetFileName(path);
                            /// Tags
                            if (Picasacache.ContainsKey(filepath))
                            {
                                var ts = Picasacache[filepath].Split(',');
                                tags.AddRange(ts);
                            }

                            if (ext == ".jpg" || ext == ".jpeg")                            
                            {
                                try
                                {
                                    JpegHelper helper = new JpegHelper(file);

                                    tags.AddRange(helper.Keywords);
                                }
                                catch
                                {
                                }
                            }

                            var filetag = System.IO.Path.GetDirectoryName(file).Split('\\');
                            tags.AddRange(filetag);

                            foreach (var tag in tags)
                            {
                                if(!TagHash.ContainsKey(tag)) 
                                    TagHash[tag] = new List<string>();

                                if(!TagHash[tag].Contains(hash))
                                    TagHash[tag].Add(hash);
                            }

                        }

                    }
                    SaveCacheFile(path + @"\" + CacheFile, FolderCacheNew);
                    ParsedPath[path] = true;
                }
            }            
        }

        private Dictionary<string, string> LoadPicasaCache(string path)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (System.IO.File.Exists(path + @"\.picasa.ini"))
            {
                using(StreamReader reader = new StreamReader(path + @"\.picasa.ini")) 
                {
                    string line = null;
                    string filepath = null;
                    while((line = reader.ReadLine()) != null) {
                        if(line[0] == '[') 
                        {
                            int index = line.IndexOf(']');
                            filepath = path + @"\" + line.Substring(1,index-1);
                        }
                        else if(line.Substring(0,8) == "keywords")
                        {
                            result.Add(filepath, line.Substring(9));
                        }
                    }
                }                
            }
            return result;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripStatusLabelStatus.Text =  "Scanning: " + e.UserState.ToString();            
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabelStatus.Text = "Idle";            
            foreach(var i in TagHash) {
                textBox1.AutoCompleteCustomSource.Add(i.Key);
            }
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageList1.ImageSize = new Size(120, 120);
            int x = 0;
            foreach (var i in HashPath)
            {

                if (x > 100) break;
                if (i.Value.Count > 1)
                {
                    x += 1;
                    imageList1.Images.Add(i.Key, new Bitmap(i.Value[0]));
                    ListViewGroup group = new ListViewGroup(i.Key, i.Key);
                    listView1.Groups.Add(group);
                    foreach (var it in i.Value)
                    {
                        ListViewItem item = new ListViewItem(it, group);
                        item.ImageKey = i.Key;
                        item.Tag = it;
                        listView1.Items.Add(item);
                    }

                }
            }    
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                DoSearch(textBox1.Text);
        }

        private void DoSearch(string p)
        {
            listView1.Items.Clear();
            listView1.Groups.Clear();
            imageList1.Images.Clear();
            imageList1.ImageSize = new Size(120,120);
            foreach (var hash in TagHash[p])
            {
                var paths = HashPath[hash];
                ListViewGroup group = new ListViewGroup(hash, hash);
                listView1.Groups.Add(group);
                imageList1.Images.Add(hash, new Bitmap(paths[0]));
                
                ListViewItem item = new ListViewItem(paths[0], group);
                item.ImageKey = hash;

                listView1.Items.Add(item);

                for (int i = 1; i < paths.Count; i++)
                {
                    ListViewItem item1 = new ListViewItem(paths[i], group);
                    item1.ImageKey = hash;
                    listView1.Items.Add(item1);
                }
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            DoSearch(SearchButton.Text);
        }

    }
}