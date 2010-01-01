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

/// 1. File link (solved)
/// 2. Upload Tag
/// 3. Server Side Code
/// 4. Read/Write Tag Attritube of Image
/// http://www.vsj.co.uk/dotnet/display.asp?id=694
/// http://www.dreamincode.net/code/snippet3144.htm
/// http://msdn.microsoft.com/en-us/library/bb643802.aspx#merged_properties__zerh
/// http://msdn.microsoft.com/en-us/library/system.windows.media.imaging.bitmapmetadata.setquery.aspx
/// 
namespace gentleman
{
    public partial class Form1 : Form
    {
        int scaned = 0;
        int jpged = 0;
        int error = 0;

        public Form1()
        {
            InitializeComponent();
            //System.Diagnostics.Process.Start(@"file://C:\var\000a_370ra.jpg");
            //Image image = Image.FromFile(@"C:\var\000a_370ra.jpg");
            StreamWriter writer = new StreamWriter(@"c:\var\test.log");
            writer.WriteLine(string.Format("{0}, {1}, {2}, {3}", DateTime.Now.ToString(), scaned, jpged, error));
            selftest(@"F:\imagehash\data\2\img");
            writer.WriteLine(string.Format("{0}, {1}, {2}, {3}", DateTime.Now.ToString(), scaned, jpged, error));
            writer.Close();
        }
        public void selftest(string dic)
        {
            try
            {
                foreach (var x in Directory.GetDirectories(dic))
                    selftest(x);
            }
            catch(UnauthorizedAccessException)
            {
            }
            foreach (var x in Directory.GetFiles(dic))
            {
                scaned += 1;
                try
                {
                    string ext = System.IO.Path.GetExtension(x).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg")
                    {
                        //continue;
                        JPGKeywordHelper helper = new JPGKeywordHelper(x);
                        jpged += 1;
                        string hashcode = helper.Hash();

                    }
                }
                catch (FormatException)
                {

                }
                catch (Exception)
                {
                    error += 1;
                }
            }

        }        

        public void test2()
        {
            JPGKeywordHelper x = new JPGKeywordHelper(@"C:\var\000a_370ra.jpg");
            x.Keywords = new List<string> { "test3", "test4" };
            x.Save(@"c:\var\000a_370ra_temp.jpg");
            x.Hash();
            Bitmap xx = new Bitmap(@"C:\var\000a_370ra.jpg");
            xx.Save(@"c:\var\test.bmp");            
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
    }
}