using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
    public class GHelper
    {
        private const string GENTLE_FILE = ".gentleman.ini";

        public static Dictionary<string, PMetaData> LoadCache(string folderPath)
        {
            Dictionary<string, PMetaData> CacheResult = new Dictionary<string, PMetaData>();

            if (System.IO.File.Exists(folderPath + @"\" + GENTLE_FILE ))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(folderPath + @"\" +GENTLE_FILE))
                {
                    string line;
                    string filepath = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line[0] == '[')
                        {
                            int index = line.IndexOf(']');
                            filepath = folderPath + @"\" + line.Substring(1, index - 1);
                            CacheResult[filepath] = new PMetaData();
                        }
                        else if (line.Contains("="))
                        {
                            var values = line.Split('=');
                            string field = values[0].Trim().ToLower();
                            string fieldvalue = values[1].Trim().ToLower();

                            if(field == "hash") {
                                if (fieldvalue.Length == 40)
                                    CacheResult[filepath].Hash = fieldvalue;
                            }                            
                            else if (line.Substring(0, 8) == "keywords")
                            {
                                CacheResult[filepath].Keywords = new List<string>(line.Substring(9).Split(','));
                            }
                            else if (line.Substring(0, 7) == "updated")
                            {
                                CacheResult[filepath].Updated = Convert.ToDateTime(line.Substring(8));
                            }
                        }
                    }
                    reader.Close();
                }
            }
            return CacheResult;
        }
        public static void UpdateCache(string folderPath, Dictionary<string, PMetaData> datas)
        {
            if (datas.Count > 0)
            {                
                System.IO.FileInfo info;
                if (System.IO.File.Exists(folderPath + @"\" + GENTLE_FILE))
                {
                    info = new System.IO.FileInfo(folderPath + @"\" + GENTLE_FILE);
                    info.Attributes = System.IO.FileAttributes.Normal;
                }

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(folderPath + @"\"  + GENTLE_FILE))
                {
                    foreach (var item in datas)
                    {
                        writer.WriteLine(string.Format("[{0}]", System.IO.Path.GetFileName(item.Key)));
                        writer.WriteLine(string.Format("hash={0}", item.Value.Hash));
                        writer.WriteLine(string.Format("keywords={0}", string.Join(",", item.Value.Keywords.ToArray())));                        
                        writer.WriteLine(string.Format("updated={0}", item.Value.Updated));                       
                    }
                }

                info = new System.IO.FileInfo(folderPath + @"\" + GENTLE_FILE);
                info.Attributes = System.IO.FileAttributes.Hidden;
            }
        }
    }   
}
