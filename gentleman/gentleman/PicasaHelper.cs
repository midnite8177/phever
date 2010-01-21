using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
  
    public class PicasaHelper
    {
        public static Dictionary<string, PMetaData> LoadCache(string folderPath)
        {
            Dictionary<string, PMetaData> CacheResult = new Dictionary<string, PMetaData>();

            using (System.IO.StreamReader reader = new System.IO.StreamReader(folderPath))
            {
                string line;
                string filepath = null;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line[0] == '[')
                    {
                        filepath = folderPath + @"\" + line.Substring(1, line.IndexOf(']'));
                        CacheResult[filepath] = new PMetaData();
                    }
                    else if (line.Substring(0, 8) == "keywords")
                    {
                        var ptags = new List<string>(line.Substring(9).Split(','));
                        ptags = ptags.ConvertAll(a => a.Trim());

                        var Keywords = ptags.FindAll(a => a.StartsWith("#"));
                        var UserKeywords = ptags.FindAll(a => !a.StartsWith("#"));

                        CacheResult[filepath].Keywords = Keywords;
                        CacheResult[filepath].UserKeywords = UserKeywords;
                    }
                    else
                    {
                        if (line.Contains("="))
                        {
                            var items = line.Split('=');
                            CacheResult[filepath].RawData[items[0]] = items[1];
                        }
                    }
                }
                return CacheResult;
            }
        }
        public static void UpdateCache(string folderPath, Dictionary<string, PMetaData> data)
        {
            if(data.Count > 0) 
            {
                System.IO.FileInfo info;
                if (System.IO.File.Exists(folderPath + @"\.picasa.ini"))
                {
                    info = new System.IO.FileInfo(folderPath + @"\.picasa.ini");
                    info.Attributes = System.IO.FileAttributes.Normal;
                }

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(folderPath + @"\.picasa.ini"))
                {
                    foreach (var item in data)
                    {
                        if (item.Value.Keywords.Count > 0 || item.Value.UserKeywords.Count > 0 || item.Value.RawData.Count > 0)
                        {
                            writer.WriteLine(string.Format("[{0}]", System.IO.Path.GetFileName(item.Key)));
                            string utags = string.Join(",", item.Value.UserKeywords.ToArray());
                            string tags = string.Join(",#", item.Value.Keywords.ToArray());

                            writer.WriteLine(string.Format("keywords={0}", string.Format("{0},#{1}", utags, tags)));

                            foreach (var raw in item.Value.RawData)
                                writer.WriteLine(string.Format("{0}={1}", raw.Key, raw.Value));
                        }
                    }
                }
                info = new System.IO.FileInfo(folderPath + @"\.picasa.ini");
                info.Attributes = System.IO.FileAttributes.Hidden;
            }
        }
    }
}
