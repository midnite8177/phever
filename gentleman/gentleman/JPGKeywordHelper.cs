using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
using System.Drawing.Imaging;
//http://www.google.com/support/forum/p/Picasa/thread?tid=5c00326898442ab0&hl=en
//
namespace gentleman
{    
    public class JpegHelper
    {
        private string jpegPath = null;        

        //private BitmapFrame bitmapFrame = null;
        //private BitmapFrame bitmapFrameCopy = null;
        //private BitmapMetadata metadata = null;
        //private BitmapMetadata metadataCopy = null;
        //private JpegBitmapDecoder decoder = null;
        //private JpegBitmapEncoder encoder = null;

        public string Hash = null;
        public List<string> Keywords = null;
        public List<string> PathKeywords = null;
        
        public JpegHelper(string path)
        {            
            jpegPath = path;
            
            using (Stream jpegStreamIn = File.Open(jpegPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                JpegBitmapDecoder decoder = null;
                // The orginial decoder method will cause problem
                // if decorder create by delay, it will fast the read speed, but can hardly write
                // if decoder created by Preserve, it will be slow
                try
                {
                    decoder = new JpegBitmapDecoder(jpegStreamIn, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                }
                catch
                {
                    Image x = Image.FromStream(jpegStreamIn);
                    if (x.RawFormat != ImageFormat.Jpeg)
                        throw new FormatException();

                    else throw new Exception();
                }

                var bitmapFrame = decoder.Frames[0];
                //bitmapFrameCopy = BitmapFrame.Create(decoder.Frames[0]);
                
                var orgMetadata = (BitmapMetadata)bitmapFrame.Metadata;

                Keywords = orgMetadata.Keywords == null ? new List<string>() : new List<string>(orgMetadata.Keywords);    
            }
            
            if (Keywords.Count > 0 && Keywords[Keywords.Count - 1].Trim().Length == 46 && Keywords[Keywords.Count - 1].Substring(0, 6) == "#HASH:")
            {
                Hash = Keywords[Keywords.Count - 1].Substring(6, 40);
                Keywords.RemoveAt(Keywords.Count - 1);
            }
            else
            {
                Hash = ComputeHash(jpegPath);
            }
        }
        //metadataCopy.SetQuery("/app13/irb/8bimiptc/iptc/keywords", string.Format("{0}",String.Join(",", Keywords.ToArray()),Hash));            
        public void Save(string path)
        {            
            var klist = new List<string>(Keywords);
            klist.Add(string.Format("#HASH:{0}", Hash));            

            JpegBitmapDecoder decoder = null;
            using (Stream jpegStreamIn = File.Open(jpegPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // The orginial decoder method will cause problem
                // if decorder create by delay, it will fast the read speed, but can hardly write
                // if decoder created by Preserve, it will be slow
                try
                {
                    decoder = new JpegBitmapDecoder(jpegStreamIn, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                }
                catch
                {
                    Image x = Image.FromStream(jpegStreamIn);
                    if (x.RawFormat != ImageFormat.Jpeg)
                        throw new FormatException();

                    else throw new Exception();
                }
            }

            var encoder = new JpegBitmapEncoder();
            var Frame = BitmapFrame.Create(decoder.Frames[0]);
            ((BitmapMetadata)Frame.Metadata).Keywords = new System.Collections.ObjectModel.ReadOnlyCollection<string>(klist);
                
            //var xx = BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, metadata, decoder.Frames[0].ColorContexts);            
            encoder.Frames.Add(Frame);
            using (Stream jpegStreamOut = File.Open(path, FileMode.Create))
            {
                encoder.Save(jpegStreamOut);
            }            
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
        public static bool ThumbnailCallback()
        {
            return false;
        }
    }
}
