using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ThumbLib;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using Microsoft.WindowsAPICodePack.Shell;

namespace gentleman
{
    public partial class Form1 : Form
    {
        // secret = 24889
        private const string CacheFile = ".Gentleman.db";
        
        private static List<string> SupportFileExt = new List<string>() { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        
        Dictionary<string, Dictionary<string, PMetaData>> HashDB = new Dictionary<string, Dictionary<string, PMetaData>>();

        private FClient uploader = new FClient();

        public Form1()
        {
            InitializeComponent();
            USN.PathDB p = new gentleman.USN.PathDB();
            p.Build();
        }

        public Dictionary<string, PMetaData> ScanFolder(string folder)
        {
            Dictionary<string, PMetaData> Results = new Dictionary<string,PMetaData>();
            Dictionary<string, PMetaData> DataFromPicasa = PicasaHelper.LoadCache(folder);
            Dictionary<string, PMetaData> DataFromGentleman = GHelper.LoadCache(folder);
            int index = 0;
            foreach (var filepath in System.IO.Directory.GetFiles(folder))
            {
                string ext = System.IO.Path.GetExtension(filepath).ToLower();

                if (!SupportFileExt.Contains(ext)) continue;
                index += 1;
                Results[filepath] = new PMetaData();
                Results[filepath].FilePath = filepath;
                Results[filepath].Updated = System.IO.File.GetLastWriteTimeUtc(filepath).Ticks;

                // FolderKeywords
                var folderpaths = folder.Split('\\');
                List<string> folderkeywords = new List<string>();
                if(folderpaths.Length > 3) {
                    folderkeywords.Add("#" + folderpaths[folderpaths.Length - 1]);
                    folderkeywords.Add("#" + folderpaths[folderpaths.Length - 2]);
                }

                // Copy Keywords and RawData from Picasa
                if (DataFromPicasa.ContainsKey(filepath))
                {
                    Results[filepath].Keywords = DataFromPicasa[filepath].Keywords;
                    Results[filepath].RawData = DataFromPicasa[filepath].RawData;
                }

                // Update Keywords from image Raw Data
                if ((ext == ".jpg" || ext ==".jpeg") && (!DataFromGentleman.ContainsKey(filepath)
                    || DataFromGentleman[filepath].Updated != Results[filepath].Updated))
                {
                    try
                    {
                        JpegHelper p = new JpegHelper(filepath);
                        Results[filepath].Keywords.AddRange(p.Keywords);
                    }
                    catch { }
                    // Update Path Keywords     
                    Results[filepath].Keywords.AddRange(folderkeywords);
                }
                if (DataFromGentleman.ContainsKey(filepath) && DataFromGentleman[filepath].Hash != null)
                {
                    Results[filepath].Hash = DataFromGentleman[filepath].Hash;
                }
                else
                {
                    try
                    {
                        Results[filepath].Hash = ThumbHelper.ComputeHash(filepath);
                    }
                    catch
                    {
                        Results.Remove(filepath);
                        continue;
                    }
                }

                Results[filepath].Keywords = Algorithm.TagSet(Results[filepath].Keywords);
                Results[filepath].Hash = Results[filepath].Hash.ToUpper();
            }
            return Results;
        }

       
        private void Paging(int index, Dictionary<string, List<string>> Results)
        {
            //listView1.Items.Clear();
            //listView1.Groups.Clear();
            //imageList1.Images.Clear();

            //int limit = 20;
            //imageList1.ImageSize = new Size(120, 120);

            //var Keys = new List<string>(Results.Keys);

            //for (int i = Page; i < limit+ Page; i++)
            //{
            //    var key = Keys[i];
            //    var value = Results[key];

            //    if (value.Count <= 1) continue;
            //    using (Image img = new Bitmap(value[0]))
            //    {
            //        imageList1.Images.Add(key, img.GetThumbnailImage(120, 120, myCallback, IntPtr.Zero));
            //        ListViewGroup group = new ListViewGroup(key, key);
            //        listView1.Groups.Add(group);
            //        foreach (var im in value)
            //        {
            //            ListViewItem it = new ListViewItem(im, group);
            //            it.ImageKey = key;
            //            listView1.Items.Add(it);
            //        }
            //    }
            //}
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void buttonSync_Click(object sender, EventArgs e)
        {            
            backgroundWorker1.RunWorkerAsync(textBox1.Text);
            this.Enabled = false;
        }
        
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            
            // Start Scan Subfolder
            var root = e.Argument.ToString();

            if (System.IO.Directory.Exists(root))
            {
                List<string> Queue = new List<string>() { root};

                while (Queue.Count > 0)
                {
                    string folderPath = Queue[0];
                    Queue.RemoveAt(0);

                    backgroundWorker1.ReportProgress(50, folderPath);

                    try
                    {
                        var Results = ScanFolder(folderPath);
                        foreach (var r in Results)
                        {
                            if (!HashDB.ContainsKey(r.Value.Hash))
                                HashDB[r.Value.Hash] = new Dictionary<string, PMetaData>();

                            HashDB[r.Value.Hash][r.Value.FilePath] = r.Value;
                        }

                        PicasaHelper.UpdateCache(folderPath, Results);
                        GHelper.UpdateCache(folderPath, Results);
                        Queue.InsertRange(0, System.IO.Directory.GetDirectories(folderPath));
                    }
                    catch
                    {                        
                    }
                                        
                    
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripLabel1.Text = e.UserState.ToString();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripLabel1.Text = "Idle";
            this.Enabled = true;
        }

        int Page = 1;
        private void buttonDuplicate_Click(object sender, EventArgs e)
        {
            Dictionary<string, Dictionary<string, PMetaData>> datas = new Dictionary<string, Dictionary<string, PMetaData>>();
            Dictionary<string, List<string>> tags = new Dictionary<string,List<string>>();
            foreach (var item in HashDB)
            {
                if (item.Value.Count > 1)
                    datas[item.Key] = item.Value;

                tags[item.Key] = new List<string>();
                foreach(var v in item.Value) {
                    tags[item.Key].AddRange(v.Value.Keywords);
                }
                tags[item.Key] = Algorithm.TagSet(tags[item.Key]);
            }
            uploader.secret = "24889";
            uploader.UploadImage(tags);

            //Page = 1;
            //toolStripLabel1.Text = "Page 1";
            //Paging(0, HashPath);

            
            //uploader.UploadImage(HashTags);
            //var result = uploader.QueryImage(new List<string>(HashTags.Keys));
            //var result1 = uploader.QueryUserImage(new List<string>(HashTags.Keys));
        }        

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
          
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            
        }        
    }
}

