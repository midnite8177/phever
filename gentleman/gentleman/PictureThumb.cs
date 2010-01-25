using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

namespace ThumbLib
{
    public class PictureThumb
    {
        public PictureThumb(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public Stream ThumbStream { get; set; }
    }
}
