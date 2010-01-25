using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
    public class PMetaData
    {
        public string FilePath = null;
        public string Hash = null;
        public long Updated;

        public List<String> Keywords = new List<string>(); // Keywords from server
        //public List<String> UserKeywords = new List<string>(); // Keywords from Local
        public Dictionary<string, string> RawData = new Dictionary<string,string>();
        
    }

}
