using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
    public class PMetaData
    {
        public string FilePath;
        public string Hash;

        public List<String> Keywords; // Keywords from server
        public List<String> UserKeywords; // Keywords from Local
        public Dictionary<string, string> RawData;

        public DateTime Updated;
    }

}
