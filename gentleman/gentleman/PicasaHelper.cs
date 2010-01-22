using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
  
    public class PicasaHelper
    {
        private const string PICASA_FILE = ".picasa.ini";
        public static Dictionary<string, PMetaData> LoadCache(string folderPath)
        {
            Dictionary<string, PMetaData> CacheResult = new Dictionary<string, PMetaData>();

            if (System.IO.File.Exists(folderPath + @"\" + PICASA_FILE))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(folderPath + @"\" + PICASA_FILE))
                {
                    string line;
                    string filepath = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line[0] == '[')
                        {
                            filepath = folderPath + @"\" + line.Substring(1, line.IndexOf(']') - 1);
                            CacheResult[filepath] = new PMetaData();
                        }
                        else if (line.Substring(0, 8) == "keywords")
                        {
                            var ptags = new List<string>(line.Substring(9).Split(','));
                            ptags = ptags.ConvertAll(a => a.Trim());

                            CacheResult[filepath].Keywords.AddRange(ptags);
                        }
                        else
                        {
                            if (line.Contains("="))
                            {
                                int index = line.IndexOf('=');
                                CacheResult[filepath].RawData[line.Substring(0, index)] = line.Substring(index + 1);
                            }
                        }
                    }

                }
            }
            return CacheResult;
        }
        public static void UpdateCache(string folderPath, Dictionary<string, PMetaData> data)
        {
            if(data.Count > 0) 
            {
                System.IO.FileInfo info;
                if (System.IO.File.Exists(folderPath + @"\" + PICASA_FILE))
                {
                    info = new System.IO.FileInfo(folderPath + @"\" +PICASA_FILE);
                    info.Attributes = System.IO.FileAttributes.Normal;
                }

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(folderPath + @"\" + PICASA_FILE))
                {
                    foreach (var item in data)
                    {
                        if (item.Value.Keywords.Count > 0 || item.Value.RawData.Count > 0)
                        {
                            writer.WriteLine(string.Format("[{0}]", System.IO.Path.GetFileName(item.Key)));                            
                            string tags = string.Join(",", item.Value.Keywords.ToArray());

                            writer.WriteLine(string.Format("keywords={0}", tags));

                            foreach (var raw in item.Value.RawData)
                                writer.WriteLine(string.Format("{0}={1}", raw.Key, raw.Value));
                        }
                    }
                }
                info = new System.IO.FileInfo(folderPath + @"\" + PICASA_FILE);
                info.Attributes = System.IO.FileAttributes.Hidden;
            }
        }
    }
}
