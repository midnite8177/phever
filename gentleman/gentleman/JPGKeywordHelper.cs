using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
using System.Drawing.Imaging;

namespace gentleman
{    
    public class JPGKeywordHelper
    {
        private string jpegPath = null;        
        private BitmapFrame bitmapFrame = null;
        private BitmapFrame bitmapFrameCopy = null;
        private BitmapMetadata metadata = null;
        private BitmapMetadata metadataCopy = null;
        private JpegBitmapDecoder decoder = null;
        private JpegBitmapEncoder encoder = null;
        //private InPlaceBitmapMetadataWriter writer = null;

        public List<string> Keywords = null;
       
        public JPGKeywordHelper(string path)
        {
            if(!File.Exists(path))  throw new Exception();

            jpegPath = path;
            using(Stream jpegStreamIn = File.Open(jpegPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                decoder = new JpegBitmapDecoder(jpegStreamIn, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            }
            bitmapFrame = decoder.Frames[0];            
            metadata = (BitmapMetadata)bitmapFrame.Metadata;
            try
            {
                metadataCopy = (BitmapMetadata)bitmapFrame.Metadata.Clone();
            }
            catch (ArgumentException)
            {
                metadataCopy = new BitmapMetadata("jpg");
            }
            
            Keywords = metadata.Keywords == null? new List<string>():new List<string>(metadata.Keywords);
            

        }

        public void Save(string path)
        {            
            metadataCopy.SetQuery("/app13/irb/8bimiptc/iptc/keywords", String.Join(",", Keywords.ToArray()));
            encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapFrame, bitmapFrame.Thumbnail, metadataCopy, bitmapFrame.ColorContexts));
            using (Stream jpegStreamOut = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                encoder.Save(jpegStreamOut);
            }            
        }
        public bool ThumbnailCallback()
        {
            return false;
        }
        public string Hash()
        {
            Bitmap bmp = new Bitmap(jpegPath);
            int nwidth = bmp.Width > bmp.Height ? 120 : bmp.Width *120/ bmp.Height;
            int nheight = bmp.Width > bmp.Height ? bmp.Height * 120 / bmp.Width : 120;
            nwidth = nwidth == 0 ? 1 : nwidth;
            nheight = nheight == 0 ? 1 : nheight;

            Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
            Image myThumbnail = bmp.GetThumbnailImage(nwidth, nheight, myCallback, IntPtr.Zero);                 

            using (MemoryStream streamout = new MemoryStream())
            {
                myThumbnail.Save(streamout, ImageFormat.Png);
                //myThumbnail.Save(@"c:\var\testpng.png");
                
                SHA1 x = new SHA1CryptoServiceProvider();
                return BitConverter.ToString(x.ComputeHash(streamout)).Replace("-", "");
            }
        }
                                 
    }
}
