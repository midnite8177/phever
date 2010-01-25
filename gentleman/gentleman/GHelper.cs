using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
    // Load Thumb http://hi.baidu.com/anynum/blog/item/5cf48b2e3d3c70301f3089ad.html
    //http://hi.baidu.com/%CC%EC%CF%C2%D7%E3%C7%F2001/blog/item/2d6d07c962a39d14bf09e6ff.html
    //http://msdn.microsoft.com/en-us/library/bb774628(VS.85).aspx for vista and later IThumbCache Interface
    // http://code.msdn.microsoft.com/WindowsAPICodePack/Release/ProjectReleases.aspx?ReleaseId=3574 , pack the interface in managed way
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
                                try
                                {
                                    CacheResult[filepath].Updated = Convert.ToInt64(line.Substring(8));
                                }
                                catch
                                {
                                    
                                }
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
