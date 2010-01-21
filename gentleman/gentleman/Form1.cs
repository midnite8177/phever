﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace gentleman
{
    public partial class Form1 : Form
    {
        private const string CacheFile = ".Gentleman.db";
        
        private static List<string> SupportFileExt = new List<string>() { ".jpg", ".jpeg", ".png", ".bmp" };

        private Dictionary<string, List<string>> HashPath = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> HashTags = new Dictionary<string, List<string>>();

        private FClient uploader = new FClient();

        public Form1()
        {
            InitializeComponent();
        }

        private static bool ThumbnailCallback()
        {
            return false;
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

                using (System.IO.MemoryStream streamout = new System.IO.MemoryStream())
                {
                    myThumbnail.Save(streamout, System.Drawing.Imaging.ImageFormat.Bmp);
                    streamout.Position = 0;
                    System.Security.Cryptography.SHA1 x = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                    return BitConverter.ToString(x.ComputeHash(streamout)).Replace("-", "");
                }
            }

        }

        private void Paging(int index, Dictionary<string, List<string>> Results)
        {
            listView1.Items.Clear();
            listView1.Groups.Clear();
            imageList1.Images.Clear();

            int limit = 20;
            imageList1.ImageSize = new Size(120, 120);

            var Keys = new List<string>(Results.Keys);

            for (int i = Page; i < limit+ Page; i++)
            {
                var key = Keys[i];
                var value = Results[key];

                if (value.Count <= 1) continue;
                using (Image img = new Bitmap(value[0]))
                {
                    imageList1.Images.Add(key, img.GetThumbnailImage(120, 120, myCallback, IntPtr.Zero));
                    ListViewGroup group = new ListViewGroup(key, key);
                    listView1.Groups.Add(group);
                    foreach (var im in value)
                    {
                        ListViewItem it = new ListViewItem(im, group);
                        it.ImageKey = key;
                        listView1.Items.Add(it);
                    }
                }
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void buttonSync_Click(object sender, EventArgs e)
        {
            
            HashPath.Clear();
            backgroundWorker1.RunWorkerAsync(textBox1.Text);
            this.Enabled = false;
        }

        private void UpdateGentlemanCache(string folderPath, Dictionary<string, PMetaData> PathMeta)
        {
            if (PathMeta.Count > 0)
            {
                System.IO.FileInfo info;
                if (System.IO.File.Exists(folderPath + @"\.gentleman.ini"))
                {
                    info = new System.IO.FileInfo(folderPath + @"\.gentleman.ini");
                    info.Attributes = System.IO.FileAttributes.Normal;
                }

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(folderPath + @"\.gentleman.ini"))
                {
                    foreach(var item in PathMeta) 
                    {
                        writer.WriteLine(string.Format("[{0}]", System.IO.Path.GetFileName(item.Key)));
                        writer.WriteLine(string.Format("keywords={0}", string.Join(",", item.Value.Keywords.ToArray())));
                        writer.WriteLine(string.Format("hash={0}", item.Value.Hash));
                        writer.WriteLine(string.Format("updated={0}", item.Value.Updated));                            
                    }                        
                }
                info = new System.IO.FileInfo(folderPath + @"\.gentleman.ini");
                info.Attributes = System.IO.FileAttributes.Hidden;
            }
        }

        private void UpdatePicasaCache(string folderPath, Dictionary<string, List<string>> PathTags)
        {
            if (PathTags.Count > 0)
            {
                System.IO.FileInfo info;
                if (System.IO.File.Exists(folderPath + @"\.picasa.ini"))
                {
                    info = new System.IO.FileInfo(folderPath + @"\.picasa.ini");
                    info.Attributes = System.IO.FileAttributes.Normal;
                }

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(folderPath + @"\.picasa.ini"))
                {
                    foreach (var item in PathTags)
                    {
                        writer.WriteLine(string.Format("[{0}]", System.IO.Path.GetFileName(item.Key)));
                        writer.WriteLine(string.Format("keywords={0}", string.Join(",", item.Value.ToArray())));
                    }
                }
                info = new System.IO.FileInfo(folderPath + @"\.picasa.ini");
                info.Attributes = System.IO.FileAttributes.Hidden;
            }
        }               

        private Dictionary<string, PMetaData> LoadGentlemanCache(string folderPath)
        {
            Dictionary<string, PMetaData> CacheResult = new Dictionary<string, PMetaData>();

            if (System.IO.File.Exists(folderPath + @"\.gentleman.ini"))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(folderPath + @"\.gentleman.ini"))
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
                        else if (line.Substring(0, 4) == "hash")
                        {
                            if(line.Substring(5).Trim().Length == 40)
                                CacheResult[filepath].Hash = line.Substring(5);
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
                    reader.Close();
                }
            }
            return CacheResult;
        }

        private Dictionary<string, List<string>> LoadPicasaCache(string folderPath)
        {
            Dictionary<string, List<string>> CacheResult = new Dictionary<string, List<string>>();
            List<string> PathQuene = new List<string>();
            if (System.IO.File.Exists(folderPath + @"\.picasa.ini"))
                PathQuene.Add(folderPath + @"\.picasa.ini");
            if (System.IO.File.Exists(folderPath + @"\picasa.ini"))
                PathQuene.Add(folderPath + @"\picasa.ini");

            foreach (string path in PathQuene)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(path))
                {
                    string line;
                    string filepath = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line[0] == '[')
                        {
                            int index = line.IndexOf(']');
                            filepath = folderPath + @"\" + line.Substring(1, index - 1);
                        }
                        else if (line.Substring(0, 8) == "keywords")
                        {
                            var value=  new List<string>(line.Substring(9).Split(','));                            
                            CacheResult[filepath] = value;
                        }
                    }
                    reader.Close();
                }
            }
            return CacheResult;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Start Scan Subfolder
            var root = e.Argument.ToString();

            if (System.IO.Directory.Exists(root))
            {
                List<string> Quene = new List<string>() { root};

                while (Quene.Count > 0)
                {
                    string folderPath = Quene[0];
                    Quene.RemoveAt(0);

                    backgroundWorker1.ReportProgress(50, folderPath);

                    bool isPicasaCacheUpdated = false;
                    bool isGentlemanCacheUpdated = false;

                    Dictionary<string, List<string>> PathTags = LoadPicasaCache(folderPath);
                    Dictionary<string, PMetaData> PathMeta = LoadGentlemanCache(folderPath);
                    
                    foreach (string filePath in System.IO.Directory.GetFiles(folderPath))
                    {
                        string ext = System.IO.Path.GetExtension(filePath);
                        if (SupportFileExt.Contains(ext.ToLower()))
                        {
                            string hash;
                            DateTime LastUpdate = System.IO.File.GetLastWriteTime(filePath);
                            if (PathMeta.ContainsKey(filePath) && PathMeta[filePath].Hash != null) hash = PathMeta[filePath].Hash;
                            else
                            {
                                try
                                {
                                    hash = ComputeHash(filePath);
                                }
                                catch
                                {
                                    continue;
                                }
                            }

                            List<string> tags = new List<string>();
                            if (PathTags.ContainsKey(filePath)) tags.AddRange(PathTags[filePath]);

                            if (PathMeta.ContainsKey(filePath))
                                tags.AddRange(PathMeta[filePath].Keywords);

                            if (!PathMeta.ContainsKey(filePath) || PathMeta[filePath].Updated != LastUpdate)
                            {
                                if (ext == ".jpg" || ext == ".jpeg")
                                {
                                    try
                                    {
                                        JpegHelper helper = new JpegHelper(filePath);
                                        tags.AddRange(helper.Keywords);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                            tags.Sort();
                            tags = Algorithm.RemoveRepeat<string>(tags);
                            tags = tags.ConvertAll<string>(a => a.Trim());
                            tags.RemoveAll(a => a.Length == 0);

                            if (!HashPath.ContainsKey(hash))
                                HashPath[hash] = new List<string>();
                            HashPath[hash].Add(filePath);
                            if (!HashTags.ContainsKey(hash))
                                HashTags[hash] = new List<string>();
                            HashTags[hash].AddRange(tags);

                            //
                            if (!PathTags.ContainsKey(filePath))
                            {
                                isPicasaCacheUpdated = true;
                                PathTags[filePath] = new List<string>();
                            }

                            if (Algorithm.DiffSet<string>(tags, PathTags[filePath]).Count > 0)
                            {
                                isPicasaCacheUpdated = true;
                                PathTags[filePath] = tags;
                            }

                            if (!PathMeta.ContainsKey(filePath))
                            {
                                isGentlemanCacheUpdated = true;
                                PathMeta[filePath] = new PMetaData();
                            }

                            if (PathMeta[filePath].Hash == null)
                            {
                                isGentlemanCacheUpdated = true;
                                PathMeta[filePath].Hash = hash;
                            }

                            if (PathMeta[filePath].Updated != LastUpdate)
                            {
                                isGentlemanCacheUpdated = true;
                                PathMeta[filePath].Updated = LastUpdate;
                            }

                            if (Algorithm.DiffSet<string>(PathMeta[filePath].Keywords, tags).Count > 0)
                            {
                                isGentlemanCacheUpdated = true;
                                PathMeta[filePath].Keywords = tags;
                            }
                        }
                    }

                    if (isGentlemanCacheUpdated)
                        UpdateGentlemanCache(folderPath, PathMeta);
                    if (isPicasaCacheUpdated)
                        UpdatePicasaCache(folderPath, PathTags);

                    Quene.InsertRange(0, System.IO.Directory.GetDirectories(folderPath));

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
            Page = 1;
            toolStripLabel1.Text = "Page 1";
            Paging(0, HashPath);

            uploader.secret = "24889";
            //uploader.UploadImage(HashTags);
            //var result = uploader.QueryImage(new List<string>(HashTags.Keys));
            //var result1 = uploader.QueryUserImage(new List<string>(HashTags.Keys));
        }        

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Page += 1;
            toolStripLabel1.Text = "Page " + Page;
            Paging(Page, HashPath);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Page -= 1;
            toolStripLabel1.Text = "Page " + Page;
            Paging(Page, HashPath);
        }        
    }
}

