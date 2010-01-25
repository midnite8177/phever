using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing;

namespace gentleman
{
    public class ThumbHelper
    {
        public static Bitmap GetThumb(string path)
        {            
            // For Window Vista, Window 7
            ShellObject x = ShellObject.FromParsingName(path);            
            return x.Thumbnail.SmallBitmap;
        }
        public  static bool ThumbnailCallback()
        {
            return false;
        }
        public static Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
        public static string ComputeHash(string path)
        {
            Image myThumbnail = GetThumb(path);

            using (System.IO.MemoryStream streamout = new System.IO.MemoryStream())
            {
                myThumbnail.Save(streamout, System.Drawing.Imaging.ImageFormat.Bmp);
                streamout.Position = 0;
                System.Security.Cryptography.SHA1 x = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                return BitConverter.ToString(x.ComputeHash(streamout)).Replace("-", "");
            }            
        }

    }
}
